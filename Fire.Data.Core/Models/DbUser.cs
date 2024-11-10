using Fire.Browser.Core;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fire.Data.Core.Models;

public class DbUser
{
	[Key]
	public Guid Id { get; set; }
	public string Username { get; set; }
	public bool IsFirstLaunch { get; set; }

	[JsonPropertyName("UserSettings")]
	public Settings UserSettings { get; set; }

}
