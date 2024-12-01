using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Riverside.Graphite.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services
{
	public class PermissionManager
	{
		private static readonly ConcurrentDictionary<string, Dictionary<string, PermissionItem>> _userPermissions = new();
		private static readonly object _fileLock = new();
		private const string PERMISSIONS_FOLDER = "Permissions";
		private const string SETTINGS_FOLDER = "Settings";

		public static event EventHandler<string> PermissionsChanged;

		public class PermissionItem
		{
			public CoreWebView2PermissionState State { get; set; }
			public CoreWebView2PermissionKind Kind { get; set; }
			public bool Changed { get; set; }
			public DateTime LastUpdated { get; set; }
		}

		private static string GetUserFolderPath(string username)
		{
			return Path.Combine(UserDataManager.CoreFolderPath, "Users", username);
		}

		private static string GetPermissionsFolderPath(string username)
		{
			string userFolderPath = GetUserFolderPath(username);
			return Path.Combine(userFolderPath, PERMISSIONS_FOLDER);
		}

		private static string GetSettingsFolderPath(string username)
		{
			string userFolderPath = GetUserFolderPath(username);
			return Path.Combine(userFolderPath, SETTINGS_FOLDER);
		}

		private static string GetPermissionsFilePath(string username)
		{
			string permissionsFolderPath = GetPermissionsFolderPath(username);
			return Path.Combine(permissionsFolderPath, "sitepermissions.json");
		}

		public static void CreateUserFolders(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				Debug.WriteLine("Username is null or empty. Cannot create user folders.");
				return;
			}

			try
			{
				string userFolderPath = GetUserFolderPath(username);
				string permissionsFolderPath = GetPermissionsFolderPath(username);
				string settingsFolderPath = GetSettingsFolderPath(username);

				Directory.CreateDirectory(userFolderPath);
				Directory.CreateDirectory(permissionsFolderPath);
				Directory.CreateDirectory(settingsFolderPath);

				Debug.WriteLine($"Created folders for user {username}:");
				Debug.WriteLine($"User folder: {userFolderPath}");
				Debug.WriteLine($"Permissions folder: {permissionsFolderPath}");
				Debug.WriteLine($"Settings folder: {settingsFolderPath}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error creating folders for user {username}: {ex}");
			}
		}

		public static async Task LoadPermissionsAsync(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				Debug.WriteLine("Username is null or empty");
				return;
			}

			if (_userPermissions.ContainsKey(username))
			{
				return;
			}

			try
			{
				CreateUserFolders(username);
				string filePath = GetPermissionsFilePath(username);
				if (!File.Exists(filePath))
				{
					_userPermissions.TryAdd(username, new Dictionary<string, PermissionItem>());
					return;
				}

				string json = await File.ReadAllTextAsync(filePath);
				var permissions = JsonConvert.DeserializeObject<Dictionary<string, PermissionItem>>(json)
								?? new Dictionary<string, PermissionItem>();

				_userPermissions.TryUpdate(username, permissions, _userPermissions.GetValueOrDefault(username));
				NotifyPermissionsChanged(username);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading permissions for user {username}: {ex}");
				_userPermissions.TryAdd(username, new Dictionary<string, PermissionItem>());
			}
		}

		private static string GetPermissionKey(string url, CoreWebView2PermissionKind kind)
		{
			try
			{
				var uri = new Uri(url);
				string domain = uri.GetLeftPart(UriPartial.Authority);
				return $"{domain}:{kind}";
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error creating permission key for URL {url}: {ex}");
				return $"{url}:{kind}";
			}
		}

		public static (CoreWebView2PermissionState? State, bool Changed) GetStoredPermission(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(url))
			{
				return (null, false);
			}

			try
			{
				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					string key = GetPermissionKey(url, kind);
					if (permissions.TryGetValue(key, out var permission))
					{
						return (permission.State, permission.Changed);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error getting stored permission: {ex}");
			}

			return (null, false);
		}

		public static async Task UpdatePermission(
			string username,
			string url,
			CoreWebView2PermissionKind kind,
			bool allowed)
		{
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(url))
			{
				return;
			}

			try
			{
				await LoadPermissionsAsync(username);

				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					string key = GetPermissionKey(url, kind);
					var permissionItem = new PermissionItem
					{
						State = allowed ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny,
						Kind = kind,
						Changed = false,
						LastUpdated = DateTime.UtcNow
					};

					if (permissions.ContainsKey(key))
					{
						var existingPermission = permissions[key];
						permissionItem.Changed = existingPermission.State != permissionItem.State;
					}

					permissions[key] = permissionItem;
					await SavePermissionsAsync(username);
					Debug.WriteLine($"Updated permission for {key} to {(allowed ? "Allow" : "Deny")}");
					NotifyPermissionsChanged(username);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error updating permission: {ex}");
			}
		}

		private static async Task SavePermissionsAsync(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				return;
			}

			try
			{
				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					string filePath = GetPermissionsFilePath(username);
					string json = JsonConvert.SerializeObject(permissions, Formatting.Indented);

					lock (_fileLock)
					{
						File.WriteAllText(filePath, json);
					}

					Debug.WriteLine($"Saved permissions for user {username}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error saving permissions: {ex}");
			}
		}

		public static async Task ClearPermissionsAsync(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				return;
			}

			try
			{
				_userPermissions.TryRemove(username, out _);
				string filePath = GetPermissionsFilePath(username);

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				await LoadPermissionsAsync(username);
				NotifyPermissionsChanged(username);
				Debug.WriteLine($"Cleared permissions for user {username}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error clearing permissions: {ex}");
			}
		}

		public static async Task MarkPermissionChanged(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(url))
			{
				return;
			}

			try
			{
				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					string key = GetPermissionKey(url, kind);
					if (permissions.TryGetValue(key, out var permission))
					{
						permission.Changed = true;
						await SavePermissionsAsync(username);
						Debug.WriteLine($"Marked permission as changed for {key}");
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error marking permission as changed: {ex}");
			}
		}

		public static async Task<Dictionary<string, PermissionItem>> GetAllPermissionsAsync(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				Debug.WriteLine("GetAllPermissionsAsync: Username is null or empty");
				return new Dictionary<string, PermissionItem>();
			}

			try
			{
				await LoadPermissionsAsync(username);

				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					Debug.WriteLine($"GetAllPermissionsAsync: Retrieved {permissions.Count} permissions for user {username}");
					return new Dictionary<string, PermissionItem>(permissions);
				}
				else
				{
					Debug.WriteLine($"GetAllPermissionsAsync: No permissions found for user {username}");
					return new Dictionary<string, PermissionItem>();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"GetAllPermissionsAsync: Error retrieving permissions for user {username}: {ex}");
				return new Dictionary<string, PermissionItem>();
			}
		}

		private static void NotifyPermissionsChanged(string username)
		{
			PermissionsChanged?.Invoke(null, username);
		}
	}
}

