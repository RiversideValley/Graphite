// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Azure;
using Azure.Data.Tables;
using FireCore.Services.Contracts.MessageHandler;
using FireCore.Services.Contracts.MessageHandler.AzureTableMessageStorage;

public class AzureDataTableMessages : IMessageHandler
{
	private readonly TableServiceClient _tableServiceClient;
	private readonly TableClient _messageTable;
	private readonly IConfiguration? _configuration;

	public AzureDataTableMessages(IConfiguration configuration)
	{
		_configuration = configuration;
		_tableServiceClient = new(_configuration?.GetRequiredSection("AzureStorage").Value!);
		_messageTable = _tableServiceClient.GetTableClient("MessageTable");
		_messageTable.CreateIfNotExists();
	}

	public async Task<string> AddNewMessageAsync(string sessionId, Message message)
	{
		var messageTime = DateTime.Now.Ticks.ToString();
		var messageEntity = new MessageEntity(sessionId, messageTime, message);

		await _messageTable.AddEntityAsync(messageEntity);

		return messageTime;
	}

	public async Task<Message?> UpdateMessageAsync(string sessionId, string sequenceId, string messageStatus)
	{
		var retry = 0;
		const int MAX_RETRY = 10;
		while (retry < MAX_RETRY)
		{
			try
			{
				var response = await _messageTable.GetEntityAsync<MessageEntity>(sessionId, sequenceId);
				var updateEntity = response.Value;

				if (updateEntity != null)
				{
					updateEntity.UpdateMessageStatus(messageStatus);
					await _messageTable.UpdateEntityAsync(updateEntity, ETag.All, TableUpdateMode.Replace);
					return updateEntity.ToMessage();
				}
			}
			catch (RequestFailedException ex) when (ex.Status == 412) // Precondition Failed
			{
				if (++retry == MAX_RETRY)
				{
					throw;
				}
				await Task.Delay(new Random().Next(10, 100));
			}
		}
		return null;
	}

	public async Task<List<Message>> LoadHistoryMessageAsync(string sessionId)
	{
		var messages = new List<Message>();
		await foreach (var entity in _messageTable.QueryAsync<MessageEntity>(filter: $"PartitionKey eq '{sessionId}'"))
		{
			var message = entity.ToMessage();
			message.SequenceId = entity.RowKey;
			messages.Add(message);
		}
		return messages;
	}
}
