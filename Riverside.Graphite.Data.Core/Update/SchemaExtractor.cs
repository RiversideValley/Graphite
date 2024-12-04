using JsonDiffPatchDotNet;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Runtime.Helpers.Logging;
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
using Windows.Services.Cortana;

namespace Riverside.Graphite.Data.Core.Update
{
	public class SchemaExtractor
	{
		private readonly string connectionString;
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		private readonly Type classIn;

		public SchemaExtractor(string connectionString, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type classIn)
		{

			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException(nameof(connectionString));

			
			if (classIn is Type type)
				if (type is null)
					throw new ArgumentNullException(nameof(type));	

			this.connectionString = connectionString;
			this.classIn = classIn;
		}

		public async Task HandleExtractionSchemaChanges() {

			
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
					foreach (var prop in properties.Distinct().ToList()) {
						await AddNameColumnsFromProperties(connectionString, classIn.Name, prop); 
					}
				}
			}
			else
			{
				// set something hear when all is indeed the same..
				Console.WriteLine("No schema differences detected.");
			}
		}

		public static async Task AddNameColumnsFromProperties(string connectionString, string strTableName, PropertyInfo property) {

			try
			{
				// capture db, datatype, defaults->go;

				using (var connection = new SqliteConnection($"Data Source={connectionString}"))
				{
					connection.Open();
					using (var action = connection.BeginTransaction())
					{
						try
						{
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
						catch (Exception ex)
						{
							action.Rollback();
							ExceptionLogger.LogException(ex);
							Console.WriteLine($"Update {strTableName} failed at reconstruction.");
						}
					}
				}
			}
			catch (ArgumentNullException ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				ExceptionLogger.LogException(ex);
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				Console.WriteLine($"An error occurred: {ex.Message}");
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

		public string SerializeModelSchema(Type classIn) {
			var json = JsonConvert.SerializeObject(classIn); 
			return json; 
		} 
		
		public string CompareSchemas(string oldSchemaJson, string newSchemaJson) 
		{ 
			var jdp = new JsonDiffPatch(); 
			var patch = jdp.Diff(oldSchemaJson, newSchemaJson);
			if (patch != null)
				return patch.ToString();
			else
				return null; 
		} 
			
	}

}

