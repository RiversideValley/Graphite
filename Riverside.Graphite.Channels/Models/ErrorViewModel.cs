
using CommunityToolkit.Mvvm.ComponentModel;

namespace FireCore.Models
{
	using System.ComponentModel;
	using System.Runtime.CompilerServices;

	public class ErrorViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		private string? _requestId;
		public string? RequestId
		{
			get => _requestId;
			set
			{
				if (_requestId != value)
				{
					_requestId = value;
					OnPropertyChanged();
					// Call the method to update ShowRequestId
					ShowRequestId = !string.IsNullOrEmpty(_requestId);
				}
			}
		}

		private bool _showRequestId;
		public bool ShowRequestId
		{
			get => _showRequestId;
			set
			{
				if (_showRequestId != value)
				{
					_showRequestId = value;
					OnPropertyChanged();
				}
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

}
