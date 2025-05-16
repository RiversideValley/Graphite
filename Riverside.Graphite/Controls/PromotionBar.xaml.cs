using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace Riverside.Graphite.Controls
{
	public sealed partial class PromotionBar : UserControl
	{
		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(nameof(Content), typeof(string), typeof(PromotionBar), new PropertyMetadata(string.Empty));

		public static readonly DependencyProperty ButtonTextProperty =
			DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(PromotionBar), new PropertyMetadata("Go to Promotion"));

		public static readonly DependencyProperty PromotionUrlProperty =
			DependencyProperty.Register(nameof(PromotionUrl), typeof(string), typeof(PromotionBar), new PropertyMetadata(string.Empty));

		public string Content
		{
			get { return (string)GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}

		public string ButtonText
		{
			get { return (string)GetValue(ButtonTextProperty); }
			set { SetValue(ButtonTextProperty, value); }
		}

		public string PromotionUrl
		{
			get { return (string)GetValue(PromotionUrlProperty); }
			set { SetValue(PromotionUrlProperty, value); }
		}

		public PromotionBar()
		{
			this.InitializeComponent();
		}

		private async void OnPromotionButtonClick(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(PromotionUrl))
			{
				await Launcher.LaunchUriAsync(new Uri(PromotionUrl));
			}
		}
	}
}