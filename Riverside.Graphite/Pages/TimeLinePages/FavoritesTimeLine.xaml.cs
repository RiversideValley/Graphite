using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services.ViewModels;

namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class FavoritesTimeLine : Page
{
	FavoritesViewModel ViewModel { get; set; }
	public FavoritesTimeLine()
	{
		ViewModel = App.GetService<FavoritesViewModel>();

		InitializeComponent();
		Loaded += (s, e) => ViewModel.LoadFavorites().ConfigureAwait(false);

		ViewModel.FavoritesContextMenu = FavoritesContextMenu;
	}


}