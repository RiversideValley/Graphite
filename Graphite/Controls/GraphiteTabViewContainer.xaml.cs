using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using Graphite.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Graphite.UserSys;

namespace Graphite.Controls;

public sealed partial class GraphiteTabViewContainer : TabView
{
	public TabManager TabManager { get; private set; }

	public GraphiteTabViewContainer()
	{
		this.InitializeComponent();
		ViewModel = new GraphiteTabViewViewModel()
		{
			Style = (Style)Application.Current.Resources["DefaultTabViewStyle"]
		};
		string username = UserManager.GetCurrentUsername();
		TabManager = new TabManager(this, username);
		this.SelectionChanged += GraphiteTabViewContainer_SelectionChanged;
	}

	public ObservableCollection<GraphiteTabViewItem> Tabs { get; } = new ObservableCollection<GraphiteTabViewItem>();

	public GraphiteTabViewViewModel ViewModel { get; set; }

	public partial class GraphiteTabViewViewModel : ObservableObject
	{
		[ObservableProperty]
		private Style _style = (Style)Application.Current.Resources["DefaultTabViewStyle"];

		[ObservableProperty]
		private ObservableCollection<GraphiteTabViewItem> _tabs = new ObservableCollection<GraphiteTabViewItem>();

		[ObservableProperty]
		private GraphiteTabViewItem _activeTab;

		[ObservableProperty]
		private object _activeTabContent;
	}

	public bool IsSplitViewActive
	{
		get { return (bool)GetValue(IsSplitViewActiveProperty); }
		set { SetValue(IsSplitViewActiveProperty, value); }
	}

	public static readonly DependencyProperty IsSplitViewActiveProperty =
		DependencyProperty.Register("IsSplitViewActive", typeof(bool), typeof(GraphiteTabViewContainer), new PropertyMetadata(false));

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


	private void GraphiteTabViewContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (SelectedItem is GraphiteTabViewItem selectedTab)
		{
			ViewModel.ActiveTab = selectedTab;
			UpdateActiveTabContent(selectedTab);
			TabManager.UpdateTabActivity(selectedTab);
		}
	}

	private void UpdateActiveTabContent(GraphiteTabViewItem selectedTab)
	{
		ViewModel.ActiveTabContent = selectedTab.Content;
	}

	internal event EventHandler<GraphiteTabViewItem> TabPutToSleep;

	public void AddTab(GraphiteTabViewItem newTab)
	{
		ViewModel.Tabs.Add(newTab);
		this.TabItems.Add(newTab);
	}
}

public class SplitViewContainer : Grid
{
	public static readonly DependencyProperty PrimaryContentProperty =
		DependencyProperty.Register(nameof(PrimaryContent), typeof(object), typeof(SplitViewContainer), new PropertyMetadata(null));

	public object PrimaryContent
	{
		get => GetValue(PrimaryContentProperty);
		set => SetValue(PrimaryContentProperty, value);
	}

	public static readonly DependencyProperty SecondaryContentProperty =
		DependencyProperty.Register(nameof(SecondaryContent), typeof(object), typeof(SplitViewContainer), new PropertyMetadata(null));

	public object SecondaryContent
	{
		get => GetValue(SecondaryContentProperty);
		set => SetValue(SecondaryContentProperty, value);
	}

	public static readonly DependencyProperty IsSplitViewActiveProperty =
		DependencyProperty.Register(nameof(IsSplitViewActive), typeof(bool), typeof(SplitViewContainer), new PropertyMetadata(false, OnIsSplitViewActiveChanged));

	public bool IsSplitViewActive
	{
		get => (bool)GetValue(IsSplitViewActiveProperty);
		set => SetValue(IsSplitViewActiveProperty, value);
	}

	private static void OnIsSplitViewActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var container = (SplitViewContainer)d;
		container.UpdateLayout();
	}

	public SplitViewContainer()
	{
		this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		this.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var primaryContent = new ContentPresenter
		{
			Margin = new Thickness(3),
			CornerRadius = new CornerRadius(5)
		};
		primaryContent.SetBinding(ContentPresenter.ContentProperty, new Binding { Path = new PropertyPath("PrimaryContent"), Source = this });
		this.Children.Add(primaryContent);
		Grid.SetColumn(primaryContent, 0);

		var splitter = new ContentControl
		{
			Width = 5,
			Background = Application.Current.Resources["SystemControlBackgroundBaseLowBrush"] as SolidColorBrush,
		};
		this.Children.Add(splitter);
		Grid.SetColumn(splitter, 1);

		var secondaryContent = new ContentPresenter
		{
			Margin = new Thickness(3),
			CornerRadius = new CornerRadius(5)
		};
		secondaryContent.SetBinding(ContentPresenter.ContentProperty, new Binding { Path = new PropertyPath("SecondaryContent"), Source = this });
		this.Children.Add(secondaryContent);
		Grid.SetColumn(secondaryContent, 2);

		UpdateLayout();
	}

	private void UpdateLayout()
	{
		if (IsSplitViewActive)
		{
			this.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
			this.ColumnDefinitions[1].Width = GridLength.Auto;
			this.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
		}
		else
		{
			this.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
			this.ColumnDefinitions[1].Width = new GridLength(0);
			this.ColumnDefinitions[2].Width = new GridLength(0);
		}
	}
}

