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
public sealed partial class AlphaFilter : Window
{
	public Dictionary<char, string> Letters { get; set; }
	public KeyValuePair<char, string> SelectedLetter { get; set; }

	public AlphaFilter()
	{
		this.InitializeComponent();

		Letters = new  Dictionary<char, string>(Enumerable.Range('A', 26).ToDictionary(i => (char)i, i => $"\\uE8{(i - 'A'):00}"));

	}

	private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		this.Close();
	}
	public class IconConverter
	{
		public static ObservableCollection<FontIcon> Icons = new ObservableCollection<FontIcon>
		(Enumerable.Range('A', 26).Select(i => new FontIcon
		{
			Glyph = char.ConvertFromUtf32(i),
			FontFamily = new FontFamily("Segoe UI Symbol"),
			FontSize = 32
		}));

		
		
	}

	public static implicit operator UIElement(AlphaFilter v)
	{
		throw new NotImplementedException();
	}
}

