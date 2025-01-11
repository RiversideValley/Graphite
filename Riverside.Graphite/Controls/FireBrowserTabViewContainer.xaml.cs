using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Runtime.Models;

namespace Riverside.Graphite.Controls
{
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

		public FireBrowserTabViewViewModel ViewModel { get; set; }

		public partial class FireBrowserTabViewViewModel : ObservableObject
		{
			[ObservableProperty]
			private Style style;
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

		public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
			nameof(Mode),
			typeof(Settings.UILayout),
			typeof(FireBrowserTabViewContainer),
			new PropertyMetadata(Settings.UILayout.Modern));

		public static readonly DependencyProperty IsPreviewEnabledProperty =
			DependencyProperty.Register(nameof(IsPreviewEnabled), typeof(bool), typeof(FireBrowserTabViewContainer),
				new PropertyMetadata(true));

		public bool IsPreviewEnabled
		{
			get => (bool)GetValue(IsPreviewEnabledProperty);
			set => SetValue(IsPreviewEnabledProperty, value);
		}

		//private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	// Optional: Handle selection changes between tabs.
		//}

		//public void AddTab(FireBrowserTabViewItem tabViewItem)
		//{
		//	if (tabViewItem != null)
		//	{
		//		tabViewItem.PointerEntered += TabItem_PointerEntered;
		//		tabViewItem.PointerExited += TabItem_PointerExited;
		//	}
		//}

		//private void TabItem_PointerEntered(object sender, RoutedEventArgs e)
		//{
		//	if (IsPreviewEnabled && sender is FireBrowserTabViewItem tabViewItem)
		//	{
		//		tabViewItem.ShowPreview();
		//	}
		//}

		//private void TabItem_PointerExited(object sender, RoutedEventArgs e)
		//{
		//	if (sender is FireBrowserTabViewItem tabViewItem)
		//	{
		//		tabViewItem.HidePreview();
		//	}
		//}
	}
}
