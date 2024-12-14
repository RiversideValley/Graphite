using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Runtime.CoreUi;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;

namespace Riverside.Graphite.Pages
{
	public sealed partial class SecureVault : Page, INotifyPropertyChanged
	{
		private SecureVaultViewModel _viewModel;
		public SecureVaultViewModel ViewModel
		{
			get => _viewModel;
			set
			{
				_viewModel = value;
				OnPropertyChanged();
			}
		}

		private const string VAULT_KEY_NAME = "SecureVaultKey";

		public SecureVault()
		{
			this.InitializeComponent();
			ViewModel = new SecureVaultViewModel();
			this.Loaded += SecureVault_Loaded;
		}

		private void SecureVault_Loaded(object sender, RoutedEventArgs e)
		{
			AuthenticateButton.Click += AuthenticateButton_Click;
		}

		private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
		{
			if (await AuthenticateAsync())
			{
				AuthOverlay.Visibility = Visibility.Collapsed;
				await LoadVaultDataAsync();
			}
			else
			{
				await ShowErrorDialogAsync("Authentication Failed", "Unable to verify your identity. Please try again.");
			}
		}

		private async Task<bool> AuthenticateAsync()
		{
			try
			{
				if (await KeyCredentialManager.IsSupportedAsync())
				{
					var result = await KeyCredentialManager.RequestCreateAsync(VAULT_KEY_NAME, KeyCredentialCreationOption.ReplaceExisting);
					if (result.Status == KeyCredentialStatus.Success)
					{
						return await VerifyKeyCredentialAsync(result.Credential);
					}
				}

				// Fallback to PIN if Windows Hello is not available or failed
				return await FallbackAuthenticationMethodAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
				return false;
			}
		}

		private async Task<bool> VerifyKeyCredentialAsync(KeyCredential credential)
		{
			var challenge = CryptographicBuffer.GenerateRandom(32);
			var signResult = await credential.RequestSignAsync(challenge);

			if (signResult.Status == KeyCredentialStatus.Success)
			{
				// In a real-world scenario, you would verify the signature here
				return true;
			}

			return false;
		}

		private async Task<bool> FallbackAuthenticationMethodAsync()
		{
			var pinDialog = new ContentDialog
			{
				Title = "Enter PIN",
				Content = new PasswordBox(),
				PrimaryButtonText = "OK",
				CloseButtonText = "Cancel",
				XamlRoot = this.XamlRoot
			};

			if (await pinDialog.ShowAsync() == ContentDialogResult.Primary)
			{
				string enteredPin = (pinDialog.Content as PasswordBox).Password;
				// In a real application, you should use a secure method to verify the PIN.
				return enteredPin == "1234";
			}

			return false;
		}

		private async Task LoadVaultDataAsync()
		{
			// In a real application, you would load the data from a secure storage
			// For this example, we'll just add some sample data
			ViewModel.Passwords.Add(new PasswordItem { Website = "example.com", Username = "user@example.com", Password = "password123" });
			ViewModel.CreditCards.Add(new CreditCardItem { CardName = "My Credit Card", CardNumber = "1234 5678 9012 3456", Expiration = "12/25", CVV = "123" });
		}

		private async Task ShowErrorDialogAsync(string title, string content)
		{
			var errorDialog = new ContentDialog
			{
				Title = title,
				Content = content,
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot
			};

			await errorDialog.ShowAsync();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class SecureVaultViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<PasswordItem> Passwords { get; } = new ObservableCollection<PasswordItem>();
		public ObservableCollection<CreditCardItem> CreditCards { get; } = new ObservableCollection<CreditCardItem>();

		public ICommand AddPasswordCommand => new RelayCommand<XamlRoot>(AddPassword);
		public ICommand AddCreditCardCommand => new RelayCommand<XamlRoot>(AddCreditCard);

		private async void AddPassword(XamlRoot root)
		{
			var dialog = new ContentDialog
			{
				Title = "Add Password",
				PrimaryButtonText = "Add",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				Content = new PasswordDialog(),
				XamlRoot = root
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				var passwordDialog = dialog.Content as PasswordDialog;
				Passwords.Add(new PasswordItem
				{
					Website = passwordDialog.Website,
					Username = passwordDialog.Username,
					Password = passwordDialog.Password
				});
			}
		}

		private async void AddCreditCard(XamlRoot root)
		{
			var dialog = new ContentDialog
			{
				Title = "Add Credit Card",
				PrimaryButtonText = "Add",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				Content = new CreditCardDialog(),
				XamlRoot = root
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				var creditCardDialog = dialog.Content as CreditCardDialog;
				CreditCards.Add(new CreditCardItem
				{
					CardName = creditCardDialog.CardName,
					CardNumber = creditCardDialog.CardNumber,
					Expiration = creditCardDialog.Expiration,
					CVV = creditCardDialog.CVV
				});
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class PasswordItem
	{
		public string Website { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
	}

	public class CreditCardItem
	{
		public string CardName { get; set; }
		public string CardNumber { get; set; }
		public string Expiration { get; set; }
		public string CVV { get; set; }
	}

	public class RelayCommand<T> : ICommand
	{
		private readonly Action<T> _execute;
		private readonly Func<T, bool> _canExecute;

		public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

		public void Execute(object parameter) => _execute((T)parameter);

		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
