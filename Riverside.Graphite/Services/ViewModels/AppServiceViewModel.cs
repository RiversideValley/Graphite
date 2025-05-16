using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Core;
using Riverside.Graphite.Services.Contracts;


namespace Riverside.Graphite.Services.ViewModels
{
	public partial class AppServiceViewModel : ObservableRecipient, INavigationAware
	{
		public Window ControllerWindow { get; set; }
		public Settings AppSettings { get; set; }
		public Graphite.Core.User MainUser { get; set; }

		public bool IsUserAuthenticated { get; set; }

		public AppServiceViewModel()
		{
			ControllerWindow = AppService.ActiveWindow;
			AppSettings = AppService.AppSettings;
			MainUser = AuthService.CurrentUser;
			IsUserAuthenticated = MainUser != null;

		}

		private AppServiceViewModel(IMessenger messenger) : base(messenger)
		{
		}

		public void OnNavigatedFrom()
		{
			;
		}

		public void OnNavigatedTo(object parameter)
		{
			;
		}
		public static AppServiceViewModel CreateWithMessenger(IMessenger messenger)
		{
			return new AppServiceViewModel(messenger);
		}
	}
}
