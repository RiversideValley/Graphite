using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System.Diagnostics;
using Riverside.Graphite.Core;
using System.Collections.Concurrent;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Runtime.CoreUi;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

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

		public static async Task LoadPermissionsAsync(string username)
		{
			if (string.IsNullOrEmpty(username))
			{
				Debug.WriteLine("LoadPermissionsAsync: Username is null or empty");
				return;
			}

			try
			{
				string filePath = GetPermissionsFilePath(username);
				Debug.WriteLine($"Attempting to load permissions from file: {filePath}");

				if (!File.Exists(filePath))
				{
					Debug.WriteLine($"Permissions file does not exist for user: {username}");
					_userPermissions.TryAdd(username, new Dictionary<string, PermissionItem>());
					return;
				}

				string json = await File.ReadAllTextAsync(filePath);
				Debug.WriteLine($"Loaded JSON content: {json}");

				var permissions = JsonConvert.DeserializeObject<Dictionary<string, PermissionItem>>(json)
								?? new Dictionary<string, PermissionItem>();

				Debug.WriteLine($"Deserialized {permissions.Count} permissions for user: {username}");

				_userPermissions.AddOrUpdate(username, permissions, (_, __) => permissions);
				NotifyPermissionsChanged(username);

				foreach (var kvp in permissions)
				{
					Debug.WriteLine($"Loaded permission: Key={kvp.Key}, State={kvp.Value.State}, Kind={kvp.Value.Kind}");
				}
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
				string domain = uri.Host.Replace("www.", "");
				Debug.WriteLine($"Creating permission key for URL: {url}");
				Debug.WriteLine($"Extracted domain: {domain}");
				Debug.WriteLine($"Permission kind: {kind}");
				string key = $"{domain}:{kind}";
				Debug.WriteLine($"Generated permission key: {key}");
				return key;
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
				Debug.WriteLine("GetStoredPermission: Username or URL is null or empty");
				return (null, false);
			}

			try
			{
				Debug.WriteLine($"Looking up permission for user: {username}");
				if (_userPermissions.TryGetValue(username, out var permissions))
				{
					string key = GetPermissionKey(url, kind);
					Debug.WriteLine($"Checking for permission with key: {key}");
					Debug.WriteLine($"Available permission keys: {string.Join(", ", permissions.Keys)}");

					if (permissions.TryGetValue(key, out var permission))
					{
						Debug.WriteLine($"Found stored permission: State={permission.State}, Changed={permission.Changed}");
						return (permission.State, permission.Changed);
					}
					Debug.WriteLine("No stored permission found for key");
				}
				else
				{
					Debug.WriteLine("No permissions found for user");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error getting stored permission: {ex}");
			}

			return (null, false);
		}

		public static async Task<CoreWebView2PermissionState> HandlePermissionRequest(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{
			Debug.WriteLine($"HandlePermissionRequest called for user: {username}, URL: {url}, Kind: {kind}");

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(url))
			{
				Debug.WriteLine("HandlePermissionRequest: Username or URL is null or empty");
				return CoreWebView2PermissionState.Deny;
			}

			try
			{
				await LoadPermissionsAsync(username);
				var (storedPermission, _) = GetStoredPermission(username, url, kind);

				if (storedPermission.HasValue)
				{
					Debug.WriteLine($"Using stored permission for {kind} on {url}: {storedPermission.Value}");
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

				Debug.WriteLine($"Permission {kind} for {url} set to {permissionState} after showing dialog");
				return permissionState;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in HandlePermissionRequest: {ex}");
				return CoreWebView2PermissionState.Deny;
			}
		}

		public static async Task<CoreWebView2PermissionState> GetEffectivePermissionState(
			string username,
			string url,
			CoreWebView2PermissionKind kind)
		{
			Debug.WriteLine($"GetEffectivePermissionState called for user: {username}, URL: {url}, Kind: {kind}");

			await LoadPermissionsAsync(username);
			var (storedPermission, _) = GetStoredPermission(username, url, kind);

			if (storedPermission.HasValue)
			{
				Debug.WriteLine($"Using stored permission for {kind} on {url}: {storedPermission.Value}");
				return storedPermission.Value;
			}

			Debug.WriteLine($"No stored permission found for {kind} on {url}. Returning Default.");
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
					Debug.WriteLine($"User allowed {formattedPermission} for {url}");
					tcs.SetResult(CoreWebView2PermissionState.Allow);
				};

				dialog.DenyClicked += (sender, args) =>
				{
					Debug.WriteLine($"User denied {formattedPermission} for {url}");
					tcs.SetResult(CoreWebView2PermissionState.Deny);
				};

				dialog.CancelClicked += (sender, args) =>
				{
					Debug.WriteLine($"User cancelled permission request for {formattedPermission} on {url}");
					tcs.SetResult(CoreWebView2PermissionState.Default);
				};

				dialog.XamlRoot = (Application.Current as App)?.m_window.Content.XamlRoot;

				_ = dialog.ShowAsync();

				var result = await tcs.Task;

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error showing permission dialog: {ex}");
				return CoreWebView2PermissionState.Deny;
			}
		}

		private static void NotifyPermissionsChanged(string username)
		{
			PermissionsChanged?.Invoke(null, username);
		}
	}
}

