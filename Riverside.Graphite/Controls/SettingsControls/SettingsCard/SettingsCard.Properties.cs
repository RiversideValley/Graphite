using Microsoft.UI.Xaml;

namespace Graphite.Controls.SettingsControls
{
	public partial class SettingsCard
	{
		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(default(string)));

		public static readonly DependencyProperty DescriptionProperty =
			DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(default(string)));

		public static readonly DependencyProperty HeaderIconProperty =
			DependencyProperty.Register(nameof(HeaderIcon), typeof(object), typeof(SettingsCard), new PropertyMetadata(default));

		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(nameof(Content), typeof(object), typeof(SettingsCard), new PropertyMetadata(default));

		public static readonly DependencyProperty ContentTemplateProperty =
			DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(SettingsCard), new PropertyMetadata(default));

		public string Header
		{
			get => (string)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public object HeaderIcon
		{
			get => GetValue(HeaderIconProperty);
			set => SetValue(HeaderIconProperty, value);
		}

		public object Content
		{
			get => GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public DataTemplate ContentTemplate
		{
			get => (DataTemplate)GetValue(ContentTemplateProperty);
			set => SetValue(ContentTemplateProperty, value);
		}
	}
}

