using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Fire.Core.Models;

public class Shortcut
{
    public async Task CreateShortcut(string username)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktopPath, $"{username}.lnk");

        await Task.Run(() =>
        {
            var shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            // Set the target application and command-line argument
            shortcut.TargetPath = $"firebrowseruser://{username}"; // Include -user in the protocol

            // Set the description to include the desired properties
            shortcut.Description = $"-fireuser {username}"; // Set the description as needed

            // Get the path to the directory where the DLL is located
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = Path.GetDirectoryName(assemblyPath);

            // Construct the path to the Logo.ico file within the DLL's directory
            string iconPath = Path.Combine(directoryPath, "Logo.ico");

            // Set the icon for the shortcut to the path of the ICO file
            shortcut.IconLocation = iconPath;

            // Save the shortcut file
            shortcut.Save();
        });
    }
}