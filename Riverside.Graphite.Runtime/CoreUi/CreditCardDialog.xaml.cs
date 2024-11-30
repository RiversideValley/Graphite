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
public sealed partial class CreditCardDialog : UserControl
{
	public string CardName
	{
		get { return CardNameTextBox.Text; }
		set { CardNameTextBox.Text = value; }
	}

	public string CardNumber
	{
		get { return CardNumberTextBox.Text; }
		set { CardNumberTextBox.Text = value; }
	}

	public string Expiration
	{
		get { return ExpirationTextBox.Text; }
		set { ExpirationTextBox.Text = value; }
	}

	public string CVV
	{
		get { return CVVBox.Password; }
		set { CVVBox.Password = value; }
	}

	public CreditCardDialog()
	{
		this.InitializeComponent();
		ShowCVVToggle.Toggled += ShowCVVToggle_Toggled;
		CardNumberTextBox.TextChanging += CardNumberTextBox_TextChanging;
	}

	private void ShowCVVToggle_Toggled(object sender, RoutedEventArgs e)
	{
		if (ShowCVVToggle.IsOn)
		{
			CVVBox.PasswordRevealMode = PasswordRevealMode.Visible;
		}
		else
		{
			CVVBox.PasswordRevealMode = PasswordRevealMode.Hidden;
		}
	}

	private void CardNumberTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
	{
		string text = CardNumberTextBox.Text.Replace(" ", "");
		string formattedText = "";

		for (int i = 0; i < text.Length; i++)
		{
			if (i > 0 && i % 4 == 0)
			{
				formattedText += " ";
			}
			formattedText += text[i];
		}

		if (formattedText != CardNumberTextBox.Text)
		{
			CardNumberTextBox.Text = formattedText;
			CardNumberTextBox.SelectionStart = formattedText.Length;
		}
	}
}
