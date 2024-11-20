using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite.Runtime.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Riverside.Graphite.Controls;
public sealed partial class AlphaFilter : Flyout
{
	public ObservableCollection<string> Letters { get; set; }
	public string SelectedLetter { get; set; }

	public AlphaFilter()
	{
		this.InitializeComponent();

		Letters =  new ObservableCollection<string>(Enumerable.Range('A', 26).Select(i => ((char)i).ToString()).ToList());
		GridMain.ItemsSource = Letters;
	}

	private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
		if (currentWindow != null && e.AddedItems.Count>0) {
			currentWindow.FilterBrowserHistory(e.AddedItems.FirstOrDefault().ToString());
		}
		this.Hide(); 
	}
	
}

