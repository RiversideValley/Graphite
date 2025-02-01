using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace Riverside.Graphite;
public sealed partial class SetupWebView : Page
{
	public SetupWebView()
	{
		InitializeComponent();
	}

	private Riverside.Graphite.Core.User GetUser()
	{
		// Check if the user is authenticated.
		if (AuthService.IsUserAuthenticated)
		{
			// Return the authenticated user.
			return AuthService.CurrentUser;
		}

		// If no user is authenticated, return null or handle as needed.
		return null;
	}
	private void StatusTog_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			// Assuming 'url' and 'selection' have been defined earlier
			bool autoSettingValue = toggleSwitch.IsOn;
			AppService.AppSettings.StatusBar = autoSettingValue;
		}
	}

	private void BrowserKeys_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			// Assuming 'url' and 'selection' have been defined earlier
			bool autoSettingValue = toggleSwitch.IsOn;
			AppService.AppSettings.BrowserKeys = autoSettingValue;
		}
	}

	private void BrowserScripts_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			// Assuming 'url' and 'selection' have been defined earlier
			bool autoSettingValue = toggleSwitch.IsOn;
			AppService.AppSettings.BrowserScripts = autoSettingValue;
		}
	}

	private void userag_TextChanged(object sender, TextChangedEventArgs e)
	{
		string blob = userag.Text.ToString();
		if (!string.IsNullOrEmpty(blob))
		{
			AppService.AppSettings.Useragent = blob;
		}
	}

	

	private void SetupWebViewBtn_Click(object sender, RoutedEventArgs e)
	{
		// allow user settings to be created from current setup user.
		// synchronous call because I want to make sure database is created first before your time out on the setupfinish page...
	
		_ = Frame.Navigate(typeof(SetupFinish));
	}
}
