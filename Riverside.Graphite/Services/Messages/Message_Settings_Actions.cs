namespace Riverside.Graphite.Services.Messages;
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
public record class Message_Settings_Actions(string _payload, EnumMessageStatus _status)
{
	public Message_Settings_Actions(string payload) : this(payload, EnumMessageStatus.Updated)
	{
		Payload = payload;
		Status = _status;
	}

	public Message_Settings_Actions(EnumMessageStatus _status) : this(null, _status)
	{
		Payload = _payload;
		Status = _status;
	}

	public EnumMessageStatus Status { get; } = _status;
	public string Payload { get; } = _payload;
}