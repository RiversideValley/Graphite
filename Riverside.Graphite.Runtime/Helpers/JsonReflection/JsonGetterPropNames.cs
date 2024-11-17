using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
				JsonPropertyAttribute jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
				if (jsonPropertyAttribute != null)
				{
					jsonPropertyNames.Add(jsonPropertyAttribute.PropertyName);
				}
			}

			return jsonPropertyNames;
		}
	}
}

