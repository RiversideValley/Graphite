namespace Graphite.Helpers;

public enum EnumMessageStatus
{
	Added,
	Collections,
	Informational,
	Login,
	Logout,
	Removed,
	Settings,
	Updated,
	XorError,
}
public record class Message_Settings_Actions(string _payload, EnumMessageStatus _status, object _dataItemPassed = null)
{
	public Message_Settings_Actions(string payload) : this(payload, EnumMessageStatus.Updated, null)
	{
		Payload = payload;
		Status = _status;
	}
	public Message_Settings_Actions(EnumMessageStatus _status, object _dataItemPassed = null) : this(null, _status, _dataItemPassed)
	{

		DataItemPassed = _dataItemPassed;
		Status = _status;
	}
	public Message_Settings_Actions(EnumMessageStatus _status) : this(null, _status, null)
	{
		Payload = _payload;
		Status = _status;
	}

	public Message_Settings_Actions(object dataItemPassed) : this(null, EnumMessageStatus.Updated, dataItemPassed)
	{
		DataItemPassed = dataItemPassed;
	}
	public object DataItemPassed { get; }
	public EnumMessageStatus Status { get; } = _status;
	public string Payload { get; } = _payload;
}

