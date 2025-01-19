using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Services; // Ensure you import your SettingsService namespace
using System;

namespace Riverside.Graphite.Services
{
	public class MicaBackdropController
	{
		private MicaController micaController;
		private SystemBackdropConfiguration backdropConfiguration;
		private Window mainWindow;
		public SettingsService SettingsService { get; set; }


		// Constructor to accept the Window instance
		public MicaBackdropController(Window window)
		{
			SettingsService = App.GetService<SettingsService>();
			mainWindow = window;
		}

		// Method to set and apply the backdrop based on settings
		public void SetBackdrop(Window window)
		{
			// Null check for SettingsService and CoreSettings
			if (SettingsService.CoreSettings == null)
			{
				// Handle the case where CoreSettings is not initialized
				throw new InvalidOperationException("SettingsService.CoreSettings is not initialized.");
			}

			// Get the backdrop kind from settings
			string backdropSetting = SettingsService.CoreSettings.BackDrop;

			// Apply the appropriate backdrop based on the setting
			ApplyBackdrop(backdropSetting, window);
		}

		private void ApplyBackdrop(string backdropSetting, Window window)
		{
			if (!MicaController.IsSupported())
			{
				return; // Mica not supported, exit.
			}

			// Dispose the existing MicaController if any
			micaController?.Dispose();
			micaController = new MicaController();

			// Apply the correct Mica Kind based on the settings value
			micaController.Kind = backdropSetting switch
			{
				"Mica" => MicaKind.Base,        // Example: Mica
				"MicaAlt" => MicaKind.BaseAlt,  // Example: MicaAlt
				_ => MicaKind.Base,             // Default to Base if not found
			};

			// Create the backdrop configuration
			backdropConfiguration = new SystemBackdropConfiguration
			{
				IsInputActive = true,
				Theme = GetAppTheme()
			};

			// Handle the theme change dynamically
			((FrameworkElement)window.Content).ActualThemeChanged += (sender, args) =>
			{
				backdropConfiguration.Theme = GetAppTheme();
			};

			// Ensure that your window implements ICompositionSupportsSystemBackdrop
			if (window is ICompositionSupportsSystemBackdrop compositionWindow)
			{
				micaController.AddSystemBackdropTarget(compositionWindow);
				micaController.SetSystemBackdropConfiguration(backdropConfiguration);
			}
			else
			{
				// Handle the case where the window does not support ICompositionSupportsSystemBackdrop
				// For example, show an error or fallback to a different backdrop
			}
		}

		private SystemBackdropTheme GetAppTheme()
		{
			return ((FrameworkElement)mainWindow.Content).ActualTheme switch
			{
				ElementTheme.Dark => SystemBackdropTheme.Dark,
				ElementTheme.Light => SystemBackdropTheme.Light,
				_ => SystemBackdropTheme.Default,
			};
		}
	}
}
