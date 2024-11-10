using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fire.Core.Helpers.JsonReflection
{
    public static class JsonGetterPropNames
    {
        public static List<string> GetJsonPropertyNames<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            var jsonPropertyNames = new List<string>();

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAttribute != null)
                {
                    jsonPropertyNames.Add(jsonPropertyAttribute.PropertyName);
                }
            }

            return jsonPropertyNames;
        }
    }

}

