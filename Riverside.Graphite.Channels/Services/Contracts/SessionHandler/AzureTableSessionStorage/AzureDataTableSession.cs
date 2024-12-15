using Azure.Data.Tables;


namespace FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage
{
	public interface IAzureDataTableSessionStorage
	{
		Task<Session> GetOrCreateSessionAsync(string userName, string partnerName);
		Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName);
		Task<List<SQL_SESSION_ENTITY>> GetSessionBySessionId(string sessionId);
		Task<bool> DeleteUserSession(string userName, string partnerName);
	}

	//https://github.dev/microsoft/TeamCloud/blob/c9f7bd085798468a3edd8fb3dcf089c8a5ab5759/src/TeamCloud.API/Services/OneTimeTokenService.cs#L45#L81
	public class AzureDataTableSession : IAzureDataTableSessionStorage
	{
		private TableServiceClient? _tableServiceClient { get; set; }
		private TableClient? _tableClient { get; set; }
		private Azure.Data.Tables.TableEntity? _tableEntity { get; set; }
		private readonly IConfiguration? _configuration;
		private readonly SignalRService _signalRService;
		private readonly IHubCommander? _commander;
		public AzureDataTableSession(IConfiguration configuration, SignalRService signalR, IHubCommander commander)
		{
			_configuration = configuration;
			_signalRService = signalR;
			_commander = commander;
			_ = InitializeTables().ConfigureAwait(false);
		}
		public async Task<Task> InitializeTables()
		{
			_tableServiceClient = new(_configuration?.GetRequiredSection("AzureStorage").Value!);
			_tableClient = _tableServiceClient?.GetTableClient("SessionTable");
			await _tableClient!.CreateIfNotExistsAsync().ConfigureAwait(false);
			return Task.CompletedTask;
		}


		internal async Task<Session?> SetPrivateSession(string userName, string partnerName)
		{
			var result = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.PartitionKey == userName && x.RowKey == partnerName).AsPages().ToBlockingEnumerable();

			if (result!.Count() > 0)
			{
				return await Task.FromResult<Session?>(new Session(result?.First().Values!.Select(x => x.SessionId).ToString()!));
			}
			else { return null; }
		}

		public async Task<bool> IsUserActive(string roomNameUserName)
		{
			var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.PartitionKey == roomNameUserName).AsPages().ToBlockingEnumerable();

			if (answer!.Count() > 0)
			{
				return await Task.FromResult(true);
			}

			return await Task.FromResult(false);
		}
		public async Task<bool> DeleteUserSession(string userName, string partnerName)
		{
			var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.PartitionKey == userName && x.RowKey == partnerName);

			var batchIt = new List<TableTransactionAction>();

			await foreach (var item in answer!)
			{
				batchIt.Add(new TableTransactionAction(TableTransactionActionType.Delete, item));
			}

			var response = await _tableClient!.SubmitTransactionAsync(batchIt).ConfigureAwait(false);

			if ((response.GetRawResponse() as Azure.Response).Status == 202)
			{
				return true;
			}

			return false;
		}
		public async Task<Session> GetOrCreateSessionAsync(string userName, string partnerName)
		{
			userName = userName.ToLower();
			partnerName = partnerName.ToLower();
			var isJointRoom = await SetPrivateSession(userName, partnerName);

			if (isJointRoom is null)
			{
				var isPartnerJoint = await SetPrivateSession(partnerName, userName);

				if (isPartnerJoint is not null)
				{
					await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName.ToLower(), isPartnerJoint));
					return isPartnerJoint;
				}
				else
				{
					var userNameisAuth = _commander?.MsalCurrentUsers!.Where(x => x == userName).FirstOrDefault();
					var partnernameisAuth = _commander?.MsalCurrentUsers!.Where(y => y == partnerName).FirstOrDefault();
					if (userNameisAuth is not null && partnernameisAuth is not null)
					{
						var session = new Session(Guid.NewGuid().ToString());
						await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, session));
						await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(partnerName, userName, session));
						return session;
					}
				}
			}


			// check to see if session exist already nor matter whom created it . 
			var sql = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.RowKey == partnerName);

			if (sql?.ToBlockingEnumerable().Count() > 0)
			{
				var sessionExists = _tableClient?.GetEntityIfExists<SQL_SESSION_ENTITY>(userName, partnerName);

				if (sessionExists!.HasValue)
				{
					return sessionExists.Value!.ToSession();
				}
				else
				{
					var existing = sql.ToBlockingEnumerable().FirstOrDefault();
					if (existing is not null)
					{
						await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, sql.ToBlockingEnumerable().FirstOrDefault()!.ToSession()));
					}
					return existing!.ToSession();
				}
			}
			else
			{
				var newSession = new Session(Guid.NewGuid().ToString());
				await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, newSession));
				return newSession;
			}
		}

		public async Task<List<SQL_SESSION_ENTITY>> GetSessionBySessionId(string sessionId)
		{
			var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.SessionId == sessionId).AsPages();

			var sessions = new List<SQL_SESSION_ENTITY>();

			await foreach (var entity in answer!)
			{
				sessions.Add(entity.Values.FirstOrDefault()!);
			}

			return sessions;
		}
		public async Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName)
		{
			var result = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(s => s.PartitionKey == userName).ConfigureAwait(false);

			var sessions = new SortedDictionary<string, Session>();

			await foreach (var entity in result!)
			{
				sessions.Add(entity.RowKey, entity.ToSession());
			}

			return sessions.ToArray();
		}
	}
}

