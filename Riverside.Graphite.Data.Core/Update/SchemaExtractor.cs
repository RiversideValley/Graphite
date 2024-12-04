using JsonDiffPatchDotNet;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Riverside.Graphite.Data.Core.Update
{
	public class SchemaExtractor
	{
		private readonly string connectionString;
		private readonly DbContext dbContext;
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		private readonly Type classIn;

		public SchemaExtractor(string connectionString, DbContext dbContext, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type classIn)
		{
			this.connectionString = connectionString;
			this.dbContext = dbContext;
			this.classIn = classIn;
		}

		public async Task CompareAndExtractSchema() {

			
			var oldSchemaJson = GetDatabaseSchemaAsJson(connectionString, classIn.Name);

			var newSchemaJson = GetPropertyNamesOnlyJson(classIn);

			var diff = CompareSchemas(oldSchemaJson, newSchemaJson);

			if (!string.IsNullOrEmpty(diff))
			{
				List<PropertyInfo> properties = new List<PropertyInfo>();

				JObject Jobj = JsonConvert.DeserializeObject<JObject>(diff);
				foreach (var item in Jobj.Properties()) {

					var prop = GetPropertyNamesIncludePropertyName(classIn, item.Value.ToString()); 
					if (prop is not null)
						properties.Add(prop);
				}

				if (properties.Count > 0)
				{
					foreach (var prop in properties) {
						await AddNameColumnsFromProperties(connectionString, classIn.Name, prop); 
					}
				}
			}
			else
			{
				Console.WriteLine("No schema differences detected.");
			}
		}

		public static async Task AddNameColumnsFromProperties(string connectionString, string strTableName, PropertyInfo property) {

			using (var connection = new SqliteConnection($"Data Source={connectionString}"))
			{
				connection.Open();
				var action = connection.BeginTransaction(); 

				var type = property.PropertyType switch
				{
					Type t when t == typeof(int) => "INTEGER",
					Type t when t == typeof(string) => "TEXT",
					Type t when t == typeof(DateTime) => "DATETIME",
					Type t when t == typeof(bool) => "INTEGER",
					Type t when t == typeof(double) => "REAL", // Add more type mappings as needed _ => "UNKNOWN" // Default case if type is not handled };
					_ => "TEXT"
				};
				object value = type switch
				{
					string t when t == "INTEGER" => 0,
					string t when t == "TEXT" => "",
					string t when t == "DATETIME" => DateTime.Now,
					string t when t == "REAL" => 0.0,
					_ => throw new NotImplementedException(),
				};

				var command = new SqliteCommand($"ALTER TABLE {strTableName} ADD {property.Name} {type} NOT NULL DEFAULT {value};", connection);
				command.Transaction = action;

				var result = command.ExecuteNonQuery();
				if (result >= 0)
					action.Commit(); 
					
				
			}

			await Task.Delay(100); 
			

		
		}
		public static string GetDatabaseSchemaAsJson(string connectionString, string strTableName)
		{
			using (var connection = new SqliteConnection($"Data Source={connectionString}"))
			{
				connection.Open();

				
				var command = new SqliteCommand($"PRAGMA table_info('{strTableName}')", connection);
				using (var reader = command.ExecuteReader())
				{
					var tableInfo = new List<Dictionary<string, object>>();

					while (reader.Read())
					{
						var columnInfo = new Dictionary<string, object>();

						for (int i = 0; i < reader.FieldCount; i++)
						{
							columnInfo[reader.GetName(i)] = reader.GetValue(i);
						}

						tableInfo.Add(columnInfo);
					}
					
					
					var name = tableInfo.Where(x=> x.ContainsKey("name")).Select(x => x["name"]).ToList();
					return JsonConvert.SerializeObject(name, Formatting.None);
				}
			}
		}
		public static bool IsJsonArray(string jsonString) {
			
			try { 
				JToken token = JToken.Parse(jsonString.Replace('\'', '\"')); 
				return token.Type == JTokenType.Array; 
			}
			catch 
			{
				return false; 
			} 
		}
		public static PropertyInfo GetPropertyNamesIncludePropertyName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string name)
		{
			if (name is null || !IsJsonArray(name))
				return null; 

			var p_name = JArray.Parse(name)[0].Value<string>();
			
			if (p_name is null)
				return null;

			var list = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(o => o.Name == p_name).FirstOrDefault();
			return list; 

		}
		public static string GetPropertyNamesOnlyJson([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) 
		{ 
			var list = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(o=> o.Name != "Self").Select(p => p.Name).ToList(); 
			return JsonConvert.SerializeObject(list, Formatting.None);

		}

	public class YourClass
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DateOfBirth { get; set; }
	}


	public string SerializeModelSchema(Type classIn) {
			var json = JsonConvert.SerializeObject(classIn); 
			return json; 
		} 
		
		public string CompareSchemas(string oldSchemaJson, string newSchemaJson) 
		{ 
			var jdp = new JsonDiffPatch(); 
			var patch = jdp.Diff(oldSchemaJson, newSchemaJson); 
			return patch.ToString();	
		} 
			
	}

}

