using Azure;
using Azure.Data.Tables;
using System;

namespace Riverside.Graphite.Services.Models
{
	public class UserEntity : ITableEntity
	{
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public string Email { get; set; }
		public string BlobUrl { get; set; }

		public string WindowUserName { get; set; }
		public string BlobName { get; set; }

		public ETag ETag { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
	}


}
