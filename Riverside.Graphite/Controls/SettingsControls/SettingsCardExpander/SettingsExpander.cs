using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;

namespace Graphite.Controls.SettingsControls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class SettingsExpander : Control
	{
		private ToggleButton _expandCollapseButton;

		public SettingsExpander()
		{
			this.DefaultStyleKey = typeof(SettingsExpander);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (_expandCollapseButton != null)
			{
				_expandCollapseButton.Click -= ExpandCollapseButton_Click;
			}

			_expandCollapseButton = GetTemplateChild("ExpandCollapseButton") as ToggleButton;
			ItemsControl = GetTemplateChild("ContentItemsControl") as ItemsControl;

			if (_expandCollapseButton != null)
			{
				_expandCollapseButton.Click += ExpandCollapseButton_Click;
			}

			UpdateVisualState(false);
		}

		private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
		{
			IsExpanded = !IsExpanded;
		}

		private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (SettingsExpander)d;
			control.OnIsExpandedChanged();
		}

		private void OnIsExpandedChanged()
		{
			if (IsExpanded)
			{
				OnExpanding(new RoutedEventArgs());
				UpdateVisualState(true);
				OnExpanded(new RoutedEventArgs());
			}
			else
			{
				OnCollapsing(new RoutedEventArgs());
				UpdateVisualState(true);
				OnCollapsed(new RoutedEventArgs());
			}
		}

		private void UpdateVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", useTransitions);
		}
	}
}

