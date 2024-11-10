using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplitTabPage : Page
    {
        public SplitTabPage()
        {
            this.InitializeComponent();
            LeftFrame.Navigate(typeof(NewTab));
            RightFrame.Navigate(typeof(NewTab));
        }
        public void CloseWebViews()
        {
            (LeftFrame.Content as WebContent).WebViewElement.Close();
            (RightFrame.Content as WebContent).WebViewElement.Close();
        }
    }
}
