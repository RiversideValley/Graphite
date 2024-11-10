using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;

namespace FireBrowserWinUi3.Controls;

public sealed partial class PopUpView : UserControl
{
    private bool isDragging;

    //private bool isDragging;
    private Point clickPosition;
    private double originalWidth;
    private double originalHeight;
    private DispatcherTimer dragTimer;

    public PopUpView()
    {
        this.InitializeComponent();
        this.SizeChanged += PopUpView_SizeChanged;

        // Initialize the timer
        dragTimer = new DispatcherTimer();
        dragTimer.Interval = TimeSpan.FromMilliseconds(10); // Adjust the interval as needed
        dragTimer.Tick += DragTimer_Tick;
    }

    private void PopUpView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Capture the size of the control after rendering
        originalWidth = e.NewSize.Width;
        originalHeight = e.NewSize.Height;
    }


    public void SetSource(Uri uri)
    {
        webView.Source = uri;
    }

    public void Show()
    {
        this.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Visibility = Visibility.Collapsed;
        if (this.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(this);
            dragTimer.Stop();
            webView.CoreWebView2.Stop();
            webView.Close();
        }
    }

    private void PopOut_Click(object sender, RoutedEventArgs e)
    {
        // Create a new window
        var newWindow = new Microsoft.UI.Xaml.Window();

        // Create an instance of the user control
        var popUpView = new FireBrowserWinUi3.Controls.PopUpView();

        // Set the content of the new window to the PopUpView
        newWindow.Content = popUpView;

        // Show the new window
        newWindow.Activate();
    }


    private void DragTimer_Tick(object sender, object e)
    {
        var mainWindow = (Application.Current as App)?.m_window as MainWindow;
        var transform = this.TransformToVisual(mainWindow.Content);
        Point relativeCoords = transform.TransformPoint(new Point(0, 0));

        double newLeft = relativeCoords.X + clickPosition.X;
        double newTop = relativeCoords.Y + clickPosition.Y;

        // Get the dimensions of the window
        double windowWidth = mainWindow.Bounds.Width;
        double windowHeight = mainWindow.Bounds.Height;

        // Ensure the PopUpView stays within the bounds of the window
        double maxWidth = windowWidth - originalWidth;
        double maxHeight = windowHeight - originalHeight;

        newLeft = Math.Max(0, Math.Min(newLeft, maxWidth));
        newTop = Math.Max(0, Math.Min(newTop, maxHeight));

        // Set the new position of the PopUpView
        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
    }

    private void Header_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        isDragging = true;
        clickPosition = e.GetCurrentPoint(this).Position;
        this.CapturePointer(e.Pointer);

        // Start the timer
        dragTimer.Start();
    }

    private void Header_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        isDragging = false;
        this.ReleasePointerCaptures();

        // Stop the timer
        dragTimer.Stop();
    }

}
