using FireBrowserWinUi3Auth.Controls;
using FireBrowserWinUi3Auth.ViewModels;
using FireBrowserWinUi3AuthCore;
using FireBrowserWinUi3Core.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3Auth
{
    public static class TwoFactorsAuthentification
    {
        private static readonly DispatcherTimer loginTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
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
            var items = Task.Run(() => FireBrowserWinUi3Core.Helpers.TwoFactorsAuthentification.Load()).Result;

            foreach (var item in items)
            {
                var twoFactAuth = new TwoFactAuth(item);
                twoFactAuth.Start();
                Items.Add(twoFactAuth);
            }
        }

        public static void ShowFlyout(FrameworkElement element)
        {
            HandleUserAuthentication();
            ResetTimerAndShowFlyout(element);
        }

        private static void HandleUserAuthentication() => userAuthenticated = !userAuthenticated;

        private static void ResetTimerAndShowFlyout(FrameworkElement element)
        {
            loginTimer.Stop();
            loginTimer.Start();

            var flyout = new Two2FAFlyout();
            flyout.ShowAt(element);
        }

        public static void Add(string name, string secret)
        {
            var item = new TwoFactorAuthItem
            {
                Name = name,
                Secret = Base32Encoding.ToBytes(secret)
            };

            var twoFactAuth = new TwoFactAuth(item);
            twoFactAuth.Start();

            Items.Add(twoFactAuth);
            FireBrowserWinUi3Core.Helpers.TwoFactorsAuthentification.Items.Add(item);
        }
    }
}