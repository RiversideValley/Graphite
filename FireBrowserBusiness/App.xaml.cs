﻿using FireBrowserMultiCore;
using FireBrowserWinUi3;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserBusiness
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        /// 
        public App()
        {
            this.InitializeComponent();

            string coreFolderPath = UserDataManager.CoreFolderPath;
            string username = GetUsernameFromCoreFolderPath(coreFolderPath);

            if (username != null)
                AuthService.Authenticate(username);
        }

        public static string GetUsernameFromCoreFolderPath(string coreFolderPath)
        {
            try
            {
                string usrCoreFilePath = Path.Combine(coreFolderPath, "UsrCore.json");

                // Check if UsrCore.json exists
                if (File.Exists(usrCoreFilePath))
                {
                    // Read the JSON content from UsrCore.json
                    string jsonContent = File.ReadAllText(usrCoreFilePath);

                    // Deserialize the JSON content into a list of user objects
                    var users = JsonSerializer.Deserialize<List<User>>(jsonContent);

                    if (users != null && users.Count > 0 && !string.IsNullOrWhiteSpace(users[0].Username))
                    {
                        return users[0].Username; // Assuming you want the first user's username
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during file reading or deserialization
                Console.WriteLine("Error reading UsrCore.json: " + ex.Message);
            }

            // Return null or an empty string if the username couldn't be retrieved
            return null;
        }


        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (!Directory.Exists(UserDataManager.CoreFolderPath))
            {
                // The "FireBrowserUserCore" folder does not exist, so proceed with  application's setup behavior.
                m_window = new SetupWindow();
                m_window.Activate();
            }
            else
            {
                // The "FireBrowserUserCore" folder exists, so proceed with your application's normal behavior.
                m_window = new MainWindow();
                m_window.Activate();
            }
        }


        private Window m_window;
    }
}
