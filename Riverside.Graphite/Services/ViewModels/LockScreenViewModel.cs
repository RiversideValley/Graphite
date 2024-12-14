using Riverside.Graphite.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.ViewModels
{
	public class LockScreenViewModel : INotifyPropertyChanged
	{
		private string _username;
		private string _profileImagePath;

		public string Username
		{
			get => _username;
			set
			{
				if (_username != value)
				{
					_username = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(WelcomeMessage));
				}
			}
		}

		public string ProfileImagePath
		{
			get => _profileImagePath;
			set
			{
				if (_profileImagePath != value)
				{
					_profileImagePath = value;
					OnPropertyChanged();
				}
			}
		}

		public string WelcomeMessage => $"Welcome back, {Username}";

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void LoadUserData()
		{
			Username = AuthService.CurrentUser.Username;
			ProfileImagePath = $"{UserDataManager.CoreFolderPath}\\{UserDataManager.UsersFolderPath}\\{Username}\\profile_image.jpg";
		}
	}
}
