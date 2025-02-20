using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;

namespace Graphite.Controls
{
	public sealed partial class GraphiteTabViewItem : TabViewItem
	{
		public GraphiteTabViewItem()
		{
			InitializeComponent();
			InitializeContextMenu();
		}

		public BitmapImage BitViewWebContent { get; set; }

		public string Value
		{
			get => (string)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value),
			typeof(string),
			typeof(GraphiteTabViewItem),
			null);

		public bool IsPinned
		{
			get => (bool)GetValue(IsPinnedProperty);
			set => SetValue(IsPinnedProperty, value);
		}

		public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
			nameof(IsPinned),
			typeof(bool),
			typeof(GraphiteTabViewItem),
			new PropertyMetadata(false, OnIsPinnedChanged));

		private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is GraphiteTabViewItem tabItem)
			{
				tabItem.UpdatePinnedAppearance();
			}
		}

		private void UpdatePinnedAppearance()
		{
			if (IsPinned)
			{
				this.Style = (Style)Application.Current.Resources["PinnedTabStyle"];
			}
			else
			{
				this.Style = (Style)Application.Current.Resources["DefaultTabStyle"];
			}
			UpdatePinMenuItemText();
		}

		private void InitializeContextMenu()
		{
			var contextMenu = new MenuFlyout();

			var pinMenuItem = new MenuFlyoutItem
			{
				Text = "Pin",
				Icon = new SymbolIcon(Symbol.Pin)
			};
			pinMenuItem.Click += PinMenuItem_Click;

			contextMenu.Items.Add(pinMenuItem);

			this.ContextFlyout = contextMenu;
		}

		private void SleepMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// Implement sleep logic here or call a method in TabManager
		}

		private void PinMenuItem_Click(object sender, RoutedEventArgs e)
		{
			UpdatePinMenuItemText();
		}

		private void UpdatePinMenuItemText()
		{
			if (ContextFlyout is MenuFlyout contextMenu)
			{
				var pinMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item => item.Text.StartsWith("Pin") || item.Text.StartsWith("Unpin"));
				if (pinMenuItem != null)
				{
					pinMenuItem.Text = IsPinned ? "Unpin" : "Pin";
				}
			}
		}

		public void UpdatePinStatus(bool isPinned)
		{
			IsPinned = isPinned;
			UpdatePinnedAppearance();
		}

		
	}
}

