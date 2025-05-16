using System.Collections.Generic;

namespace Riverside.Graphite.Core;

public class UserV2
{
	public string Username { get; set; }
	public string Email { get; set; }
	public string WindowsUserName { get; set; }
	public bool IsFirstLaunch { get; set; }
	public string ProfileImagePath { get; set; }
	public bool HasPassword { get; set; }
	public List<string> Profiles { get; set; }
	public string ActiveProfile { get; set; }
	public string SessionId { get; set; }
	public override string ToString()
	{
		return $"Username: {Username}, Email: {Email}, WindowsUserName: {WindowsUserName}, IsFirstLaunch: {IsFirstLaunch}";
	}
	public static explicit operator UserV2(Riverside.Graphite.Core.User source)
	{
		return new UserV2
		{
			Username = source.Username,
			Email = source.Email,
			WindowsUserName = source.WindowsUserName,
			IsFirstLaunch = source.IsFirstLaunch,
			HasPassword = source.Password != null,
			SessionId = source.Id.ToString(),
		};
	}

}
