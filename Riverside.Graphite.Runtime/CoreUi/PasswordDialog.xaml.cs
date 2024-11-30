using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Riverside.Graphite.Runtime.CoreUi;

public sealed partial class PasswordDialog : UserControl
{
	public string Website
	{
		get { return WebsiteTextBox.Text; }
		set { WebsiteTextBox.Text = value; }
	}

	public string Username
	{
		get { return UsernameTextBox.Text; }
		set { UsernameTextBox.Text = value; }
	}

	public string Password
	{
		get { return PasswordBox.Password; }
		set { PasswordBox.Password = value; }
	}

	public PasswordDialog()
	{
		this.InitializeComponent();
		ShowPasswordToggle.Toggled += ShowPasswordToggle_Toggled;
	}

	private void ShowPasswordToggle_Toggled(object sender, RoutedEventArgs e)
	{
		if (ShowPasswordToggle.IsOn)
		{
			PasswordBox.PasswordRevealMode = PasswordRevealMode.Visible;
		}
		else
		{
			PasswordBox.PasswordRevealMode = PasswordRevealMode.Hidden;
		}
	}
}
