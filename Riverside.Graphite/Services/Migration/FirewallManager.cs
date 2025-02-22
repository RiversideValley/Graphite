using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security.Principal;
using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Services.Migration
{
	public class FirewallManager
	{
		private const string AppName = "GraphiteMigrationTool";
		private const int Port = 8080;

		private static async Task<bool> RulesExistAsync()
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c netsh advfirewall firewall show rule name=\"{AppName} INbound\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
					string output = await process.StandardOutput.ReadToEndAsync();
					await process.WaitForExitAsync();
					return output.Contains("Rule Name:"); // Returns true if rule exists
				}
			}
			catch
			{
				return false;
			}
		}

		public static async Task<bool> EnsureFirewallRulesExistAsync()
		{
			// First check if rules already exist
			if (await RulesExistAsync())
			{
				return true; // Rules exist, no need for elevation
			}

			// Rules don't exist, need elevation
			if (!IsElevated())
			{
				var result = await RequestElevationAsync();
				if (!result)
				{
					return false;
				}
			}

			return await CreateFirewallRulesAsync();
		}

		private static bool IsElevated()
		{
			using (var identity = WindowsIdentity.GetCurrent())
			{
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}

		public static async Task<bool> CreateFirewallRulesAsync()
		{
			try
			{
				await CreateRuleAsync("in");
				await CreateRuleAsync("out");
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to create firewall rules: {ex.Message}");
				return false;
			}
		}

		private static async Task CreateRuleAsync(string direction)
		{
			string args = $"advfirewall firewall add rule name=\"{AppName} {direction.ToUpperInvariant()}bound\" dir={direction} action=allow protocol=TCP localport={Port} enable=yes profile=public,private,domain";
			await ExecuteCommandAsync("netsh", args);
		}

		private static async Task ExecuteCommandAsync(string command, string arguments)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c {command} {arguments}",
				Verb = "runas",
				UseShellExecute = true,
				CreateNoWindow = false,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			using (var process = new Process { StartInfo = startInfo })
			{
				try
				{
					process.Start();
					await process.WaitForExitAsync();

					if (process.ExitCode != 0)
					{
						throw new Exception($"Command failed with exit code {process.ExitCode}");
					}
				}
				catch (System.ComponentModel.Win32Exception ex)
				{
					if (ex.NativeErrorCode == 1223) // The operation was canceled by the user.
					{
						throw new OperationCanceledException("The user canceled the UAC prompt.", ex);
					}
					throw;
				}
			}
		}

		private static async Task<bool> RequestElevationAsync()
		{
			var dialog = new ContentDialog
			{
				Title = "Administrator Permission Required",
				Content = "Firewall rules need to be created to allow the application to communicate. A UAC prompt will appear. Please click 'Yes' to allow the changes.",
				PrimaryButtonText = "Continue",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary
			};

			var result = await dialog.ShowAsync();

			return result == ContentDialogResult.Primary;
		}
	}
}

