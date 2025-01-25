using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riverside.Graphite.Data.Core.Models
{
    public class Collection
    {
        [Key]
        public int Id { get; set; }

        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime CreatedDate { get; set; }

        [ForeignKey("HistoryItemId")]
        public int HistoryItemId { get; set; }  // Foreign Key for HistoryItem

        [ForeignKey("CollectionNameId")]
        public int CollectionNameId { get; set; } // Navigation Property for CollectionName

        [NotMapped]
        public IEnumerable<CollectionName> ParentCollection { get; set; }
        [NotMapped]
        public IEnumerable<DbHistoryItem> ItemsHistory { get; set; }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), DateFormat, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }
}
