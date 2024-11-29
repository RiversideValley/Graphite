using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Riverside.Graphite.Runtime.Helpers
{
	public class SitePermission
	{
		public string Url { get; set; }
		public Dictionary<string, bool?> Permissions { get; set; } = new Dictionary<string, bool?>();
	}

	public static class PermissionManager
	{
		private static readonly Dictionary<string, List<SitePermission>> _userPermissions = new();

		public static async Task LoadPermissionsAsync(string username)
		{
			string filePath = GetPermissionFilePath(username);
			if (File.Exists(filePath))
			{
				string json = await File.ReadAllTextAsync(filePath);
				_userPermissions[username] = JsonSerializer.Deserialize<List<SitePermission>>(json);
			}
			else
			{
				_userPermissions[username] = new List<SitePermission>();
			}
		}

		public static async Task SavePermissionsAsync(string username)
		{
			string filePath = GetPermissionFilePath(username);
			string json = JsonSerializer.Serialize(_userPermissions[username], new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(filePath, json);
		}

		public static void StorePermission(string username, string url, CoreWebView2PermissionKind kind, bool? allowed)
		{
			if (!_userPermissions.ContainsKey(username))
			{
				_userPermissions[username] = new List<SitePermission>();
			}

			string rootUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
			SitePermission sitePermission = _userPermissions[username].Find(sp => sp.Url == rootUrl);
			if (sitePermission == null)
			{
				sitePermission = new SitePermission { Url = rootUrl };
				_userPermissions[username].Add(sitePermission);
			}

			sitePermission.Permissions[kind.ToString()] = allowed;
		}

		public static CoreWebView2PermissionState? GetStoredPermission(string username, string url, CoreWebView2PermissionKind kind)
		{
			if (!_userPermissions.ContainsKey(username))
			{
				return null;
			}

			string rootUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
			SitePermission sitePermission = _userPermissions[username].Find(sp => sp.Url == rootUrl);
			if (sitePermission != null && sitePermission.Permissions.TryGetValue(kind.ToString(), out bool? allowed))
			{
				if (allowed.HasValue)
				{
					return allowed.Value ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
				}
			}

			return null;
		}

		private static string GetPermissionFilePath(string username)
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", "Users", username, "Database", "sitepermissions.json");
		}
	}
}