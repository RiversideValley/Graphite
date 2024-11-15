using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Pages.Patch;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services;
using Riverside.Graphite.Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace Riverside.Graphite
{
	public partial class UC_Viewmodel : ObservableRecipient
	{
		public UC_Viewmodel()
		{
			Users = new List<UserExtend>();
			//LoginToMicrosoft().ConfigureAwait(false);
		}
		public List<UserExtend> Users { get; set; }
		public UserExtend User { get; set; }
		public Window ParentWindow { get; set; }
		public UIElement ParentGrid { get; set; }
		public bool IsCoreFolder => _IsCoreFolder();

		[ObservableProperty]
		[NotifyPropertyChangedRecipients]
		private bool _IsMsLogin;
		public BitmapImage MsProfilePicture { get; set; }

		//public Visibility _IsMsLoginVisible  => IsMsLogin ? Visibility.Visible : Visibility.Collapsed;
		public Visibility IsMsLoginVisibility { get { return IsMsLogin ? Visibility.Visible : Visibility.Collapsed; ; } }

		private readonly Func<bool> _IsCoreFolder = () =>
		{
			// Your condition here
			string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string backupDirectory = Path.Combine(documentsFolder);

			if (Directory.Exists(backupDirectory))
			{
				string[] backupFiles = Directory.GetFiles(backupDirectory, "*.firebackup");
				return backupFiles.Length > 0;
			}
			return false;
		};



		[RelayCommand(CanExecute = nameof(IsMsLogin))]
		private async Task MsLogOut()
		{

			if (IsMsLogin)
			{
				await AppService.MsalService?.SignOutAsync();
				IsMsLogin = AppService.MsalService.IsSignedIn;
				RaisePropertyChanges(nameof(IsMsLoginVisibility));

			}

		}

		[RelayCommand]
		private async Task LoginToMicrosoft()
		{

			Microsoft.Identity.Client.AuthenticationResult answer = await AppService.MsalService?.SignInAsync();
			RaisePropertyChanges(nameof(IsMsLogin));
			IsMsLogin = answer is not null && answer.AccessToken is not null;

			if (IsMsLogin)
			{

				if (AppService.GraphService.ProfileMicrosoft is null)
				{


					using Stream stream = await AppService.MsalService.GraphClient?.Me.Photo.Content.GetAsync();
					if (stream == null)
					{
						MsProfilePicture = new BitmapImage(new Uri("ms-appx:///Assets/Microsoft.png"));
						RaisePropertyChanges(nameof(MsProfilePicture));
						return;
					}

					MemoryStream memoryStream = new();
					await stream.CopyToAsync(memoryStream);
					memoryStream.Position = 0;

					BitmapImage bitmapImage = new();
					await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
					MsProfilePicture = bitmapImage;
					RaisePropertyChanges(nameof(MsProfilePicture));
				}
				else
				{
					MsProfilePicture = AppService.GraphService.ProfileMicrosoft;
				}


			}
			RaisePropertyChanges(nameof(IsMsLoginVisibility));
		}

		[RelayCommand]
		private void ExitWindow()
		{
			AppService.IsAppGoingToClose = true;
			ParentWindow?.Close();
		}


		[RelayCommand]
		private async Task AdminCenter()
		{

			UpLoadBackup win = new();
			win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay);
			Windows.Graphics.SizeInt32? desktop = await Windowing.SizeWindow();
			win.AppWindow.MoveAndResize(new(ParentWindow.AppWindow.Position.X, 0, desktop.Value.Width / 2, desktop.Value.Height / 2));
			win.ExtendsContentIntoTitleBar = true;
			_ = Windowing.ShowWindow(WindowNative.GetWindowHandle(win), Windowing.WindowShowStyle.SW_SHOWDEFAULT);
			_ = Windowing.AnimateWindow(WindowNative.GetWindowHandle(win), 2000, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_ACTIVATE);
			win.AppWindow?.ShowOnceWithRequestedStartupState();

		}

		[RelayCommand]
		private void OpenWindowsWeather()
		{
			Windows.System.LauncherOptions options = new();
			options.DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseMinimum;

			// Launch the URI
			_ = Windows.System.Launcher.LaunchUriAsync(new("msnweather://forecast"), options).GetAwaiter().GetResult();
		}

		[RelayCommand]
		private async Task BackUpCore()
		{

			BackUpDialog dlg = new();
			dlg.XamlRoot = ParentGrid?.XamlRoot;
			_ = await dlg.ShowAsync();

		}

		[RelayCommand(CanExecute = nameof(IsCoreFolder))]
		private async Task RestoreCore()
		{
			//usercentral is big enough now. 
			RestoreBackupDialog dlg = new();
			dlg.XamlRoot = ParentGrid?.XamlRoot;
			_ = await dlg.ShowAsync();
		}

		public void RaisePropertyChanges([CallerMemberName] string propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
	}
}