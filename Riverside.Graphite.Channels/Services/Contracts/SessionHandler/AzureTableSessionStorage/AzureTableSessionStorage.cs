// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;


namespace FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage
{
    public class AzureTableSessionStorage : ISessionHandler
    {
        private readonly CloudStorageAccount _storageAccount;

        private readonly CloudTableClient _cloudTableClient;

        private readonly CloudTable _sessionTable;

        private readonly IConfiguration _configuration;

        private readonly SignalRService _signalRService;
        private readonly IHubCommander? _commander;

        public AzureTableSessionStorage(IConfiguration configuration, SignalRService signalRService, IHubCommander commander)
        {
            _configuration = configuration;
            _storageAccount = CloudStorageAccount.Parse(_configuration["AzureStorage"]);
            _cloudTableClient = _storageAccount.CreateCloudTableClient();
            _commander = commander;
            _sessionTable = _cloudTableClient.GetTableReference("SessionTable");
            _sessionTable.CreateIfNotExistsAsync();
            _signalRService = signalRService;

        }


        internal async Task<Session?> SetPrivateSession(string userName, string partnerName)
        {

            var query = new TableQuery<SessionEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, partnerName))
            );

            var result = await _sessionTable.ExecuteQuerySegmentedAsync(query, null);

            if (result.Count<SessionEntity>() > 0)
            {
                return result?.FirstOrDefault()!.ToSession();
            }
            else { return null; }


        }

        public async Task<bool> IsUserActive(string roomNameUserName)
        {

            var sessionRow = new TableQuery<SessionEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, roomNameUserName)
            );

            var answer = await _sessionTable.ExecuteQuerySegmentedAsync(sessionRow, null);

            if (answer.Count() > 0)
            {
                return true;
            }

            return false;

        }
        public async Task<bool> DeleteUserSession(string userName, string partnerName)
        {

            var sessionRow = new TableQuery<SessionEntity>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, partnerName)
                )
            );

            var answer = await _sessionTable.ExecuteQuerySegmentedAsync(sessionRow, null);

            if (answer.Count() > 0)
            {
                var removable = await _sessionTable.ExecuteAsync(TableOperation.Delete(answer.FirstOrDefault()));
                var result = removable.Result as SessionEntity;
                if (result is not null)
                {
                    return true;
                }
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
                    await _sessionTable.ExecuteAsync(TableOperation.Insert(new SessionEntity(userName, partnerName.ToLower(), isPartnerJoint)));
                    return isPartnerJoint;
                }
                else
                {
                    var userNameisAuth = _commander?.MsalCurrentUsers!.Where(x => x.UserPrincipalName == userName).FirstOrDefault();
                    var partnernameisAuth = _commander?.MsalCurrentUsers!.Where(y => y.UserPrincipalName == partnerName).FirstOrDefault();
                    if (userNameisAuth is not null && partnernameisAuth is not null)
                    {
                        var session = new Session(Guid.NewGuid().ToString());
                        await _sessionTable.ExecuteAsync(TableOperation.Insert(new SessionEntity(userName, partnerName, session)));
                        await _sessionTable.ExecuteAsync(TableOperation.Insert(new SessionEntity(partnerName, userName, session)));
                        return session;
                    }

                }
            }


            // check to see if session exist already nor matter whom created it . 
            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, partnerName));

            var result = await _sessionTable.ExecuteQuerySegmentedAsync(query, null);
            // if session exists
            if (result.Count<SessionEntity>() > 0)
            {
                // session exist see if current use is subscribed or not 
                var isUserandGroupExist = TableOperation.Retrieve<SessionEntity>(userName, partnerName);
                var retrievedResult = await _sessionTable.ExecuteAsync(isUserandGroupExist);
                var sessionEntity = retrievedResult.Result as SessionEntity;

                if (sessionEntity != null)
                {
                    return sessionEntity.ToSession();
                }
                else
                {

                    var s = result.FirstOrDefault();
                    await _sessionTable.ExecuteAsync(TableOperation.Insert(new SessionEntity(userName, partnerName, s.ToSession())));
                    return s.ToSession();
                }
            }
            else
            {
                var newSession = new Session(Guid.NewGuid().ToString());
                await _sessionTable.ExecuteAsync(TableOperation.Insert(new SessionEntity(userName, partnerName, newSession)));
                return newSession;
            }

        }

        public async Task<List<SessionEntity>> GetSessionBySessionId(string sessionId)
        {

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("SessionId", QueryComparisons.Equal, sessionId));

            var result = await _sessionTable.ExecuteQuerySegmentedAsync(query, null);

            var sessions = new List<SessionEntity>();

            foreach (var entity in result)
            {
                sessions.Add(entity);
            }

            return sessions;

        }
        public async Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName)
        {

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName));

            var result = await _sessionTable.ExecuteQuerySegmentedAsync(query, null);

            var sessions = new SortedDictionary<string, Session>();
            foreach (var entity in result)
            {
                sessions.Add(entity.RowKey, entity.ToSession());
            }

            return sessions.ToArray();
        }
    }
}
