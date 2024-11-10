using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceProcess;

namespace FireBrowserWinUi3.Pages
{
    public sealed partial class ApiDash : Page
    {
        public class ApiItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public Brush StatusColor { get; set; }
        }

        public class ApiDashViewModel
        {
            public ObservableCollection<ApiItem> ApiItems { get; set; }

            public ApiDashViewModel()
            {
                ApiItems = new ObservableCollection<ApiItem>
                {
                    new ApiItem { Name = "Vault Api", Description = "Connection To Vault", Status = "Inactive", StatusColor = new SolidColorBrush(Colors.Red) },
                    //new ApiItem { Name = "Services Api", Description = "Connection To Services", Status = "Inactive", StatusColor = new SolidColorBrush(Colors.Red) },
                    //new ApiItem { Name = "Data Api", Description = "Connection To Data", Status = "Inactive", StatusColor = new SolidColorBrush(Colors.Red) },
                    //new ApiItem { Name = "Account Api", Description = "Connection To Account", Status = "Inactive", StatusColor = new SolidColorBrush(Colors.Red) },
                    // Add more items here
                };

                UpdateServiceStatus();
            }

            public void UpdateServiceStatus()
            {
                string serviceName = "SecureVaultService";

                try
                {
                    using (ServiceController sc = new ServiceController(serviceName))
                    {
                        bool isRunning = sc.Status == ServiceControllerStatus.Running;

                        foreach (var item in ApiItems)
                        {
                            item.Status = isRunning ? "Active" : "Inactive";
                            item.StatusColor = new SolidColorBrush(isRunning ? Colors.Green : Colors.Red);
                        }
                    }
                }
                catch
                {
                    // Handle errors (e.g., service not found)
                    foreach (var item in ApiItems)
                    {
                        item.Status = "Unknown";
                        item.StatusColor = new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }

        public ApiDash()
        {
            this.InitializeComponent();
            this.DataContext = new ApiDashViewModel();
        }

        private void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController("SecureVaultService"))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        ((ApiDashViewModel)DataContext).UpdateServiceStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., service not found, access denied)
                System.Diagnostics.Debug.WriteLine($"Error starting service: {ex.Message}");
            }
        }

        private void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController("SecureVaultService"))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        ((ApiDashViewModel)DataContext).UpdateServiceStatus();
                    }
                }
            }
            catch
            {

            }
        }

        private const string ServiceName = "SecureVaultService";

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
