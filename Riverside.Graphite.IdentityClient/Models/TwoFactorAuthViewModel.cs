﻿using Microsoft.UI.Xaml;
using Riverside.Graphite.IdentityClient.Enums;
using Riverside.Graphite.IdentityClient.Services;
using Riverside.Graphite.IdentityClient.Utils;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Riverside.Graphite.IdentityClient.Models
{
	public class TwoFactorAuthViewModel
	{
		private readonly TwoFactorAuthService _authService;
		private string _code;
		private int _progressValue;
		private string _name;
		private Totp _totp;
		private int _remainingSeconds;
		private DispatcherTimer _timer;

		public TwoFactorAuthItem Item { get; }

		public string Code
		{
			get => _code;
			set
			{
				if (_code != value)
				{
					_code = value;
					OnPropertyChanged(nameof(Code));
				}
			}
		}

		public int ProgressValue
		{
			get => _progressValue;
			set
			{
				if (_progressValue != value)
				{
					_progressValue = value;
					OnPropertyChanged(nameof(ProgressValue));
				}
			}
		}

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}

		public ICommand CopyCodeCommand { get; }
		public ICommand RemoveItemCommand { get; }

		public TwoFactorAuthViewModel(TwoFactorAuthItem item, TwoFactorAuthService authService)
		{
			Item = item;
			_authService = authService;
			Name = item.Name;
			CopyCodeCommand = new RelayCommand(CopyCode);
			RemoveItemCommand = new RelayCommand(RemoveItem);

			_totp = new Totp(Item.Secret, 30, (OtpHashMode)Item.OtpHashMode, Item.Size);

			// Calculate initial remaining seconds
			long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			_remainingSeconds = 30 - (int)(currentUnixTime % 30);

			// Generate initial code
			GenerateNewCode();
			UpdateProgressValue();
		}

		public void UpdateCodeAndProgress()
		{
			long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			_remainingSeconds = 30 - (int)(currentUnixTime % 30);

			if (_remainingSeconds == 30 || _remainingSeconds == 0)
			{
				GenerateNewCode();
			}

			UpdateProgressValue();
		}

		private void GenerateNewCode()
		{
			Code = _totp.ComputeTotp();
		}

		private void UpdateProgressValue()
		{
			// Calculate progress as a percentage (0-100)
			ProgressValue = (int)((_remainingSeconds / 30.0) * 100);
		}

		private async void CopyCode()
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(Code);
			Clipboard.SetContent(dataPackage);

			await Task.Delay(2000);
		}

		private async void RemoveItem()
		{
			await _authService.RemoveItemAsync(this);
		}

		public event EventHandler<PropertyChangedEventArgs> PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PropertyChangedEventArgs : EventArgs
	{
		public string PropertyName { get; }

		public PropertyChangedEventArgs(string propertyName)
		{
			PropertyName = propertyName;
		}
	}

	public class RelayCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Action execute, Func<bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

		public void Execute(object parameter) => _execute();

		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}

