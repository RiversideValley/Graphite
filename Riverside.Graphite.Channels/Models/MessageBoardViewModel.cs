using CommunityToolkit.Mvvm.ComponentModel;
using FireCore.Services;
using FireCore.Services.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FireCore.Models
{
	
    public class MessageBoardViewModel : ObservableRecipient
    {

        private IHubContext<AzureChat>? _hubContext;
        private readonly IHubCommander? _commander;
        private readonly HttpClient? _httpClient;
        ILogger<MessageBoardViewModel> _logger;
        public MessageBoardViewModel(IHubContext<AzureChat> hubContext, IHubCommander? commander, ILogger<MessageBoardViewModel> logger, HttpClient? httpClient)
        {
            _hubContext = hubContext;
            _commander = commander;
            _logger = logger;
            _httpClient = httpClient;
			Initialize().ConfigureAwait(false);	

		}

        public Task Initialize()
        {
            try
            {
				var users = AzureChat.ConnectedIds.Select(t => t.Value).ToList(); /// _graphServiceClient!.Users.Request().GetAsync();
				_commander!.MsalCurrentUsers = users.ToList();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.GetHashCode(), e.Message!.ToString(), e);
				return Task.FromException(e);	
            }
			
			return Task.CompletedTask;	

        }

    }
}
