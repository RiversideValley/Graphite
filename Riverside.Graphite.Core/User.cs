using System;
using System.Text.Json.Serialization;

namespace Riverside.Graphite.Core;

public class User
{
	private readonly User user;
	public User(User user)
	{
		this.user = user;
	}

	public User() { }

	public Guid Id { get; set; }
	public string Username { get; set; }
	public string Email { get; set; }

	public string WindowsUserName { get; set; }
	public string Password { get; set; }
	public bool IsFirstLaunch { get; set; }

	[JsonPropertyName("UserSettings")]
	public Settings UserSettings { get; set; }

	public static explicit operator User(UserV2 userV2)
	{
		return new User
		{
			Id = Guid.Parse(userV2.SessionId),
			Username = userV2.Username,
			Email = userV2.Email,
			WindowsUserName = userV2.WindowsUserName,
			IsFirstLaunch = userV2.IsFirstLaunch
		};
	}


}
