using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Riverside.Graphite.Services.WindowsHandler.Contracts;

namespace Riverside.Graphite.Services.WindowsHandler;

public class TitleBar
{
	private IWindowHandler _windowHandler;
	private Grid _titleBarGrid;
	private Button _minimizeButton;
	private Button _maximizeRestoreButton;
	private Button _closeButton;

	public TitleBar(IWindowHandler windowHandler)
	{
		_windowHandler = windowHandler;
		CreateTitleBar();
	}

	private void CreateTitleBar()
	{
		_titleBarGrid = new Grid
		{
			Height = 32,
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
		};

		_titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_titleBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		CreateDragRegion();
		CreateTitleBarButtons();

		_titleBarGrid.PointerPressed += TitleBar_PointerPressed;
	}

	private void CreateDragRegion()
	{
		var dragRegion = new Grid();
		dragRegion.SetValue(Grid.ColumnProperty, 0);
		_titleBarGrid.Children.Add(dragRegion);
	}

	private void CreateTitleBarButtons()
	{
		_minimizeButton = CreateTitleBarButton("\uE921", 1);
		_maximizeRestoreButton = CreateTitleBarButton("\uE922", 2);
		_closeButton = CreateTitleBarButton("\uE8BB", 3);

		_minimizeButton.Click += (s, e) => NativeMethods.ShowWindow(_windowHandler.Hwnd, NativeMethods.SW_MINIMIZE);
		_maximizeRestoreButton.Click += MaximizeRestoreButton_Click;
		_closeButton.Click += (s, e) => _windowHandler.MainWindow.Close();
	}

	private Button CreateTitleBarButton(string glyph, int column)
	{
		var button = new Button
		{
			Content = new FontIcon { Glyph = glyph, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") },
			Width = 46,
			Height = 32,
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
		};
		button.SetValue(Grid.ColumnProperty, column);
		_titleBarGrid.Children.Add(button);
		return button;
	}

	private void TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (e.GetCurrentPoint(_titleBarGrid).Properties.IsLeftButtonPressed)
		{
			NativeMethods.ReleaseCapture();
			NativeMethods.SendMessage(_windowHandler.Hwnd, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, nint.Zero);
		}
	}

	private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
	{
		var windowPlacement = new NativeMethods.WINDOWPLACEMENT();
		NativeMethods.GetWindowPlacement(_windowHandler.Hwnd, ref windowPlacement);

		if (windowPlacement.showCmd == NativeMethods.SW_MAXIMIZE)
		{
			NativeMethods.ShowWindow(_windowHandler.Hwnd, NativeMethods.SW_NORMAL);
			_maximizeRestoreButton.Content = new FontIcon { Glyph = "\uE922", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") };
		}
		else
		{
			NativeMethods.ShowWindow(_windowHandler.Hwnd, NativeMethods.SW_MAXIMIZE);
			_maximizeRestoreButton.Content = new FontIcon { Glyph = "\uE923", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons") };
		}
	}

	public UIElement GetTitleBarElement()
	{
		return _titleBarGrid;
	}
}