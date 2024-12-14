using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.CoreUi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	public class PermissionManager
	{
		private static readonly ConcurrentDictionary<string, Dictionary<string, PermissionItem>> _userPermissions = new();
		private static readonly object _fileLock = new();
		private const string PERMISSIONS_FOLDER = "Permissions";

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

		private static string GetPermissionsFilePath(string username)
		{
			string permissionsFolderPath = GetPermissionsFolderPath(username);
			return Path.Combine(permissionsFolderPath, "sitepermissions.json");
		}

		private static void EnsureDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		public static async Task LoadPermissionsAsync(string username)
		{
			string path = Path.Combine(UserDataManager.CoreFolderPath, "Users", username.ToString(), "Permissions");
			EnsureDirectoryExists(path);

			if (string.IsNullOrEmpty(username))
			{
				return;
			}

			try
			{
				string filePath = GetPermissionsFilePath(username);

				if (!File.Exists(filePath))
				{
					Debug.WriteLine($"Permissions file does not exist for user: {username}");
					_userPermissions.TryAdd(username, new Dictionary<string, PermissionItem>());
					return;
				}

				string json = await File.ReadAllTextAsync(filePath);

				var permissions = JsonConvert.DeserializeObject<Dictionary<string, PermissionItem>>(json)
								?? new Dictionary<string, PermissionItem>();


				_userPermissions.AddOrUpdate(username, permissions, (_, __) => permissions);
				NotifyPermissionsChanged(username);
			}
			catch (Exception ex)
			{
				_userPermissions.TryAdd(username, new Dictionary<string, PermissionItem>());
			}
		}

		private static string GetPermissionKey(string url, CoreWebView2PermissionKind kind)
		{
			try
			{
				var uri = new Uri(url);
				string domain = uri.Host.Replace("www.", "");

				string key = $"{domain}:{kind}";
				return key;
			}
			catch (Exception ex)
			{
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
				else
				{
				}
			}
			catch (Exception ex)
			{
			}

			return (null, false);
		}

		public static async Task<CoreWebView2PermissionState> HandlePermissionRequest(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(url))
			{
				return CoreWebView2PermissionState.Deny;
			}

			try
			{
				await LoadPermissionsAsync(username);
				var (storedPermission, _) = GetStoredPermission(username, url, kind);

				if (storedPermission.HasValue)
				{
					return storedPermission.Value;
				}

				string permissionKind = kind.ToString();
				string formattedPermission = FormatPermissionKind(permissionKind);

				CoreWebView2PermissionState permissionState = await ShowAndHandlePermissionDialogAsync(formattedPermission, storedPermission, url);

				if (permissionState != CoreWebView2PermissionState.Default)
				{
					bool allowed = permissionState == CoreWebView2PermissionState.Allow;
					await UpdatePermission(username, url, kind, allowed);
					await SavePermissionsAsync(username);
				}

				return permissionState;
			}
			catch (Exception ex)
			{
				return CoreWebView2PermissionState.Deny;
			}
		}

		public static async Task<CoreWebView2PermissionState> GetEffectivePermissionState(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{

			await LoadPermissionsAsync(username);
			var (storedPermission, _) = GetStoredPermission(username, url, kind);

			if (storedPermission.HasValue)
			{
				return storedPermission.Value;
			}

			return CoreWebView2PermissionState.Default;
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
					NotifyPermissionsChanged(username);
				}
			}
			catch (Exception ex)
			{
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

				}
			}
			catch (Exception ex)
			{
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
					}
				}
			}
			catch (Exception ex)
			{
			}
		}

		public static Dictionary<string, PermissionItem> GetUserPermissions(string username)
		{
			if (_userPermissions.TryGetValue(username, out var permissions))
			{
				return permissions;
			}
			return new Dictionary<string, PermissionItem>();
		}

		public static async Task DeletePermission(string username, string url, CoreWebView2PermissionKind kind)
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
					if (permissions.Remove(key))
					{
						await SavePermissionsAsync(username);
						NotifyPermissionsChanged(username);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error deleting permission: {ex.Message}");
			}
		}

		private static string FormatPermissionKind(string permissionKind)
		{
			return permissionKind.Replace("CoreWebView2PermissionKind", "").ToLower();
		}

		private static async Task<CoreWebView2PermissionState> ShowAndHandlePermissionDialogAsync(
			string formattedPermission,
			CoreWebView2PermissionState? storedPermission,
			string url)
		{
			try
			{
				string dialogTitle = $"Allow {formattedPermission}?";
				string manageText = $"The website at {url} wants to use {formattedPermission}.";

				var dialog = new PermissionDialog(dialogTitle, manageText);

				var tcs = new TaskCompletionSource<CoreWebView2PermissionState>();

				dialog.AllowClicked += (sender, args) =>
				{
					tcs.SetResult(CoreWebView2PermissionState.Allow);
				};

				dialog.DenyClicked += (sender, args) =>
				{
					tcs.SetResult(CoreWebView2PermissionState.Deny);
				};

				dialog.CancelClicked += (sender, args) =>
				{
					tcs.SetResult(CoreWebView2PermissionState.Default);
				};

				dialog.XamlRoot = (Application.Current as App)?.m_window.Content.XamlRoot;

				_ = dialog.ShowAsync();

				var result = await tcs.Task;

				return result;
			}
			catch (Exception ex)
			{
				return CoreWebView2PermissionState.Deny;
			}
		}

		private static void NotifyPermissionsChanged(string username)
		{
			PermissionsChanged?.Invoke(null, username);
		}
	}
}

