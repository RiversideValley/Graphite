// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Data.Tables;

namespace FireCore.Services.Contracts.SessionHandler.AzureTableSessionStorage
{

	public class SQL_SESSION_ENTITY : ITableEntity
	{
		public string RowKey { get; set; } = default!;

		public string PartitionKey { get; set; } = default!;

		public string? SessionId { get; set; }

		public ETag ETag { get; set; } = default!;

		public DateTimeOffset? Timestamp { get; set; } = default!;
		public SQL_SESSION_ENTITY() { }

		public SQL_SESSION_ENTITY(string pkey, string rkey)
		{
			PartitionKey = pkey;
			RowKey = rkey;
		}

		public SQL_SESSION_ENTITY(string pkey, string rkey, Session session)
		{
			PartitionKey = pkey;
			RowKey = rkey;
			SessionId = session.SessionId;
		}

		public Session ToSession()
		{
			return new Session(SessionId!);
		}
	}
	public class SessionEntity : SQL_SESSION_ENTITY
	{
		public string? SessionId { get; set; }

		public SessionEntity() { }

		public SessionEntity(string pkey, string rkey)
		{
			PartitionKey = pkey;
			RowKey = rkey;
		}

		public SessionEntity(string pkey, string rkey, Session session)
		{
			PartitionKey = pkey;
			RowKey = rkey;
			SessionId = session.SessionId;
		}

		public Session ToSession()
		{
			return new Session(SessionId!);
		}
	}
}
