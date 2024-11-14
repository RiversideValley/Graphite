using Riverside.Graphite.IdentityClient.Controls;
using Riverside.Graphite.IdentityClient.Private;
using Riverside.Graphite.Runtime.Models;
using Riverside.Graphite.Runtime.Helpers;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.IdentityClient.ViewModels
{
	public static class TwoFactorsAuthentification
	{
		private static readonly DispatcherTimer loginTimer = new() { Interval = TimeSpan.FromMinutes(5) };
		private static bool userAuthenticated = false;
		internal static ObservableCollection<TwoFactAuth> Items { get; } = new ObservableCollection<TwoFactAuth>();

		public static XamlRoot XamlRoot { get; set; }

		static TwoFactorsAuthentification()
		{
			loginTimer.Tick += (_, _) => userAuthenticated = false;
			InitializeData();
		}

		public static void Init()
		{
			loginTimer.Start();
		}

		private static void InitializeData()
		{
			ObservableCollection<TwoFactorAuthItem> items = Task.Run(Riverside.Graphite.Runtime.Helpers.TwoFactorsAuthentification.Load).Result;

			foreach (TwoFactorAuthItem item in items)
			{
				TwoFactAuth twoFactAuth = new(item);
				twoFactAuth.Start();
				Items.Add(twoFactAuth);
			}
		}

		public static void ShowFlyout(FrameworkElement element)
		{
			HandleUserAuthentication();
			ResetTimerAndShowFlyout(element);
		}

		private static void HandleUserAuthentication()
		{
			userAuthenticated = !userAuthenticated;
		}

		private static void ResetTimerAndShowFlyout(FrameworkElement element)
		{
			loginTimer.Stop();
			loginTimer.Start();

			Two2FAFlyout flyout = new();
			flyout.ShowAt(element);
		}

		public static void Add(string name, string secret)
		{
			TwoFactorAuthItem item = new()
			{
				Name = name,
				Secret = Base32Encoding.ToBytes(secret)
			};

			TwoFactAuth twoFactAuth = new(item);
			twoFactAuth.Start();

			Items.Add(twoFactAuth);
			Items.Add(item);
		}
	}
}