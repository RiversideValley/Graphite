using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Riverside.Graphite.Runtime.Helpers.JsonReflection
{
	public static class JsonGetterPropNames
	{
		public static List<string> GetJsonPropertyNames<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
		{
			List<string> jsonPropertyNames = new();

			PropertyInfo[] properties = typeof(T).GetProperties();
			foreach (PropertyInfo property in properties)
			{
				JsonPropertyNameAttribute jsonPropertyNameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
				if (jsonPropertyNameAttribute != null)
				{
					jsonPropertyNames.Add(jsonPropertyNameAttribute.Name);
				}
				else
				{
					// If no JsonPropertyNameAttribute is present, add the property name as is
					// This mimics the default behavior of System.Text.Json
					jsonPropertyNames.Add(property.Name);
				}
			}

			return jsonPropertyNames;
		}
	}
}