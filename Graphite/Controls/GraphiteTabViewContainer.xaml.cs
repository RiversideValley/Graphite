using CommunityToolkit.Mvvm.ComponentModel;
using Graphite.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Graphite.Controls;

public sealed partial class GraphiteTabViewContainer : TabView
{
	public GraphiteTabViewContainer()
	{
		InitializeComponent();
		ViewModel = new GraphiteTabViewViewModel()
		{
			Style = (Style)Application.Current.Resources["DefaultTabViewStyle"]
		};
	}

	public GraphiteTabViewViewModel ViewModel { get; set; }

	public partial class GraphiteTabViewViewModel : ObservableObject
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
		typeof(GraphiteTabViewContainer),
		new PropertyMetadata(Settings.UILayout.Modern));

}
