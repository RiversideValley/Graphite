using Fire.Authentication.Private;
using Fire.Core.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fire.Authentication.ViewModels
{
	public class TwoFactAuth : INotifyPropertyChanged
	{
		private string code = "000000";
		private int progressValue = 100;
		private int remainingSeconds;

		public string Name { get; set; }
		public TwoFactorAuthItem Data { get; set; }

		public string Code
		{
			get => code;
			set => SetAndNotify(ref code, value);
		}

		public int ProgressValue
		{
			get => progressValue;
			set => SetAndNotify(ref progressValue, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public TwoFactAuth(TwoFactorAuthItem data)
		{
			Data = data;
			Name = data.Name;
		}

		public void Start()
		{
			totp = new Totp(Data.Secret, Data.Step, (OtpHashMode)Data.OtpHashMode, Data.Size);
			Refresh();
		}

		private Totp totp;

		private async void Refresh()
		{
			await Task.Delay(10); // Initial delay before starting the loop

			while (true)
			{
				int remainingSec = totp.RemainingSeconds();

				if (remainingSec != remainingSeconds)
				{
					Code = totp.ComputeTotp();
					remainingSeconds = remainingSec;
					ProgressValue = 100 * remainingSeconds / 30;
				}

				if (remainingSeconds == 30)
				{
					Code = totp.ComputeTotp();
				}

				await Task.Delay(1000);
			}
		}

		private void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				NotifyPropertyChanged(propertyName);
			}
		}
	}
}