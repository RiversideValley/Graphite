using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

