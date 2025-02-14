using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Favorites;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class FavoritesTimeLine : Page
{
	FavoritesViewModel ViewModel { get; set; }
	public FavoritesTimeLine()
	{
		ViewModel = App.GetService<FavoritesViewModel>();

		InitializeComponent();
		Loaded += (s,e) => ViewModel.LoadFavorites().ConfigureAwait(false);

		ViewModel.FavoritesContextMenu = FavoritesContextMenu; 
	}

	
}