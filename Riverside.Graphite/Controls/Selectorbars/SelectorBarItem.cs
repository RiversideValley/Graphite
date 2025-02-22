using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Controls.Selectorbars;
	public class SelectorBarItem : ContentControl
	{
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(string), typeof(SelectorBarItem), new PropertyMetadata(default(string)));

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SelectorBarItem), new PropertyMetadata(false, OnIsSelectedChanged));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public SelectorBarItem()
		{
			this.DefaultStyleKey = typeof(SelectorBarItem);
		}

		private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var item = (SelectorBarItem)d;
			item.OnIsSelectedChanged();
		}

		protected virtual void OnIsSelectedChanged()
		{
			VisualStateManager.GoToState(this, IsSelected ? "Selected" : "Normal", true);
		}
	}

