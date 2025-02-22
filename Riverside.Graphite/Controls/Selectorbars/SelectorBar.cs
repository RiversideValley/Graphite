using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Riverside.Graphite.Controls.Selectorbars
{
	public class SelectorBar : ItemsControl
	{
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(SelectorBarItem), typeof(SelectorBar), new PropertyMetadata(null, OnSelectedItemChanged));

		public SelectorBarItem SelectedItem
		{
			get => (SelectorBarItem)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public event SelectionChangedEventHandler SelectionChanged;

		public SelectorBar()
		{
			this.DefaultStyleKey = typeof(SelectorBar);
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (Items.Count > 0 && SelectedItem == null)
			{
				SelectedItem = Items[0] as SelectorBarItem;
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is SelectorBarItem selectorBarItem)
			{
				selectorBarItem.Tapped += SelectorBarItem_Tapped;
			}
		}

		private void SelectorBarItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			if (sender is SelectorBarItem tappedItem)
			{
				SelectedItem = tappedItem;
			}
		}

		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selectorBar = (SelectorBar)d;
			var oldItem = e.OldValue as SelectorBarItem;
			var newItem = e.NewValue as SelectorBarItem;

			if (oldItem != null)
			{
				oldItem.IsSelected = false;
			}

			if (newItem != null)
			{
				newItem.IsSelected = true;
			}

			selectorBar.SelectionChanged?.Invoke(selectorBar, new SelectionChangedEventArgs(
				(IList<object>)new List<object> { oldItem }.Where(i => i != null),
				(IList<object>)new List<object> { newItem }.Where(i => i != null)));
		}
	}
}
