using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.ViewModels;
using System.ComponentModel;

namespace Riverside.Graphite.Controls
{
	public sealed partial class FireBrowserTabViewItem : TabViewItem, INotifyPropertyChanged
	{
		public FireBrowserTabViewItem()
		{
			InitializeComponent();
		}

		private TabViewItemViewModel _viewModel = new TabViewItemViewModel { IsTooltipEnabled = false };

		public TabViewItemViewModel ViewModel
		{
			get => _viewModel;
			set
			{
				if (_viewModel != value)
				{
					_viewModel = value;
					OnPropertyChanged(nameof(ViewModel));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void ShowPreview()
		{
			if (TabPreviewPopup != null)
			{
				UpdatePopupContent();
				TabPreviewPopup.IsOpen = true;
			}
		}

		public void HidePreview()
		{
			if (TabPreviewPopup != null)
			{
				TabPreviewPopup.IsOpen = false;
			}
		}

		private void UpdatePopupContent()
		{
			if (Content != null)
			{
				PreviewTitle.Text = Header?.ToString() ?? "No Title";
				PreviewContent.Content = Content; // Display the tab's content dynamically in the popup
				PreviewFooter.Text = "Status: Ready";
			}
		}
	}
}
