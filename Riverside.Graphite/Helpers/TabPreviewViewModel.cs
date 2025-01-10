using System.ComponentModel;

namespace Riverside.Graphite.Helpers;

public class TabPreviewViewModel : INotifyPropertyChanged
{
	private string _previewtabTitle;
	private string _previewFooterText;

	public string PreviewTabTitle
	{
		get => _previewtabTitle;
		set
		{
			if (_previewtabTitle != value)
			{
				_previewtabTitle = value;
				OnPropertyChanged(nameof(PreviewTabTitle));
			}
		}
	}

	public string PreviewFooterText
	{
		get => _previewFooterText;
		set
		{
			if (_previewFooterText != value)
			{
				_previewFooterText = value;
				OnPropertyChanged(nameof(PreviewFooterText));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
