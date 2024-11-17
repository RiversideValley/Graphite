using System;

namespace Riverside.Graphite.Services.BarcodeHost.Extensions;
public class StringValueAttribute : Attribute
{
	#region Properties

	public string StringValue { get; protected set; }

	#endregion

	/// <param name="value"></param>
	public StringValueAttribute(string value)
	{
		StringValue = value;
	}
}

public static class CustomExtensions
{
	/// <param name="value"></param>
	public static string GetStringValue(this Enum value)
	{
#if NETSTANDARD1_3
        var fieldInfo = value.GetType().GetRuntimeField(value.ToString());
#else
		System.Reflection.FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
#endif
		StringValueAttribute[] attr = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
		return attr.Length > 0 ? attr[0].StringValue : null;
	}
}
