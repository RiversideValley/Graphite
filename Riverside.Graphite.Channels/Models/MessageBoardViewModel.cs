using FireCore.Services;
using FireCore.Services.Hubs;

namespace FireCore.Models
{

	using Microsoft.AspNetCore.SignalR;
	using Microsoft.Extensions.Logging;
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Net.Http;
	using System.Threading.Tasks;

	public class MessageBoardViewModel : INotifyPropertyChanged
	{
		private IHubContext<AzureChat>? _hubContext;
		private readonly IHubCommander? _commander;
		private readonly HttpClient? _httpClient;
		private readonly ILogger<MessageBoardViewModel> _logger;

		public MessageBoardViewModel(IHubContext<AzureChat> hubContext, IHubCommander? commander, ILogger<MessageBoardViewModel> logger, HttpClient? httpClient)
		{
			_hubContext = hubContext;
			_commander = commander;
			_logger = logger;
			_httpClient = httpClient;
			Initialize().ConfigureAwait(false);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private string? _requestId;
		public string? RequestId
		{
			get => _requestId;
			set
			{
				if (_requestId != value)
				{
					_requestId = value;
					OnPropertyChanged(nameof(RequestId));
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
					OnPropertyChanged(nameof(ShowRequestId));
				}
			}
		}

		public Task Initialize()
		{
			try
			{
				if (AzureChat.ConnectedIds is null) return Task.CompletedTask;

				var users = AzureChat.ConnectedIds?.Select(t => t.Value).ToList();
				_commander!.MsalCurrentUsers = users!.ToList();

			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "Error initializing MessageBoardViewModel");
				return Task.FromException(e);
			}
			return Task.CompletedTask;
		}
	}

}
