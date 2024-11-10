using FireBrowserWinUi3MultiCore;
using FireBrowserWinUi3Services.PluginCore;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Pluginss : Page
    {
        private PluginManager manager = null;
        public ObservableCollection<PluginEntry> MenuItems { get; set; } = new ObservableCollection<PluginEntry>();


        public string userFolderPath = System.IO.Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Modules");
        public Pluginss()
        {
            this.InitializeComponent();

            manager = new PluginManager(userFolderPath);
            foreach (var ele in manager.CurrentPlugins)
            {
                PluginEntry ent = new PluginEntry(ele);
                MenuItems.Add(ent);
            }

            ListPath.ItemsSource = MenuItems;
        }
    }
}
