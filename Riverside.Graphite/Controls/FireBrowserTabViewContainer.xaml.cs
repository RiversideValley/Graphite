using CommunityToolkit.Mvvm.ComponentModel;
using Riverside.Graphite.Runtime.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FireBrowserWinUi3.Controls;

public sealed partial class FireBrowserTabViewContainer : TabView
{
	public FireBrowserTabViewContainer()
	{
		InitializeComponent();
		ViewModel = new FireBrowserTabViewViewModel()
		{
			Style = (Style)Application.Current.Resources["DefaultTabViewStyle"]
		};
	}

	public FireBrowserTabViewViewModel ViewModel { get; set; } = new FireBrowserTabViewViewModel();

	public partial class FireBrowserTabViewViewModel : ObservableObject
	{
		[ObservableProperty] private Style style;
	}

	public Settings.UILayout Mode
	{
		get => (Settings.UILayout)GetValue(ModeProperty);
		set
		{
			ViewModel.Style = value switch
			{
				Settings.UILayout.Modern => (Style)Application.Current.Resources["DefaultTabViewStyle"],
				Settings.UILayout.Vertical => (Style)Application.Current.Resources["VerticalTabViewStyle"],
				_ => (Style)Application.Current.Resources["DefaultTabViewStyle"]
			};

			SetValue(ModeProperty, value);
		}
	}


	public static DependencyProperty ModeProperty = DependencyProperty.Register(
	nameof(Mode),
	typeof(Settings.UILayout),
	typeof(FireBrowserTabViewContainer),
	null);
}