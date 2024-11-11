using Fire.Browser.Core;
using Fire.Core.Exceptions;
using Fire.Core.Helpers;
using Fire.Core.ImagesBing;
using Fire.Data.Core.Actions;
using Fire.Data.Favorites;
using FireBrowserDatabase;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services;
using FireBrowserWinUi3.Services.Models;
using FireBrowserWinUi3.ViewModels;
using Microsoft.Bing.WebSearch.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using static FireBrowserWinUi3.MainWindow;
using Settings = Fire.Core.Models.Settings;

namespace FireBrowserWinUi3.Pages;


public sealed partial class NewTab : Page
{


	public List<TrendingItem> trendings = new();
	public HomeViewModel ViewModel { get; set; }
	private HistoryActions HistoryActions { get; } = new HistoryActions(AuthService.CurrentUser.Username);
	private Fire.Browser.Core.Settings userSettings { get; set; }
	private SettingsService SettingsService { get; }

	private Passer param;
	private readonly bool isAuto = default;

	public NewTab()
	{
		ViewModel = App.GetService<HomeViewModel>();

		// init to load controls from settings, and start clock . 
		_ = ViewModel.Intialize().GetAwaiter();
		// assign to ViewModel, and or new instance.  
		ViewModel.SettingsService.Initialize();
		userSettings = ViewModel.SettingsService.CoreSettings;

		InitializeComponent();

		if (userSettings.IsTrendingVisible)
		{
			_ = UpdateTrending().ConfigureAwait(false);
		}
	}
	public class TrendingItem
	{
		public string webSearchUrl { get; set; }
		public string name { get; set; }
		public string url { get; set; }
		public string text { get; set; }
		public TrendingItem() { }
		public TrendingItem(string _webSearchUrl, string _name, string _url, string _text)
		{
			webSearchUrl = _webSearchUrl;
			name = _name;
			url = _url;
			text = _text;
		}

	}
	private async Task UpdateTrending()
	{
		BingSearchApi bing = new();
		string topics = bing.TrendingListTask("calico cats").GetAwaiter().GetResult();
		// fixed treding errors. 
		if (topics is not null)
		{
			List<Newtonsoft.Json.Linq.JToken> list = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(topics).ToList();

			trendings.Clear(); ;

			foreach (Newtonsoft.Json.Linq.JToken item in list)
			{

				trendings.Add(new TrendingItem(item["webSearchUrl"].ToString(), item["name"].ToString(), item["image"]["url"].ToString(), item["query"]["text"].ToString()));
			}
		}

		await Task.Delay(1000);


	}
	public record TrendingListItem(string webSearchUrl, string name, string url, string text);

	private async void NewTabSearchBox_QuerySubmittedAsync(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		if (string.IsNullOrEmpty(args.QueryText))
		{
			return;
		}

		if (isAuto && Application.Current is App app && app.m_window is MainWindow window)
		{
			_ = window.DispatcherQueue.TryEnqueue(() =>
			{
				window.FocusUrlBox(args.QueryText);
			});

			await Task.Delay(200);

			_ = sender.DispatcherQueue.TryEnqueue(() =>
			{
				_ = sender.Focus(FocusState.Programmatic);
			});

			await Task.Delay(200);

			_ = sender.DispatcherQueue.TryEnqueue(() =>
			{
				sender.Text = string.Empty;
			});

			await Task.Delay(200);
		}
	}
	private async void QueryThis_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{


		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
		{

			if (!string.IsNullOrEmpty(sender.Text))
			{
				List<HistoryItem> suggestions = await SearchControls(sender.Text);

				if (suggestions.Count > 0)
				{
					sender.ItemsSource = suggestions;
				}
				else
				{
					List<HistoryItem> l = new();
					HistoryItem h = new();
					h.Title = string.Format("{0} Searching...\r\nTopic:\t {1}", SearchProviders.ProvidersList.Where((x) => x.ProviderName == userSettings.EngineFriendlyName).FirstOrDefault().ProviderName.ToUpperInvariant(), sender.Text);
					BitmapImage bip = SearchProviders.ProvidersList.Where((x) => x.ProviderName == userSettings.EngineFriendlyName).FirstOrDefault().Image;
					h.ImageSource = bip;
					l.Add(h);
					sender.ItemsSource = l;
				}

			}
			else
			{
				List<HistoryItem> l = new();
				HistoryItem h = new();
				h.Title = "Please type to search!";
				BitmapImage bip = SearchProviders.ProvidersList.Where((x) => x.ProviderName == userSettings.EngineFriendlyName).FirstOrDefault().Image;
				h.ImageSource = bip;
				l.Add(h);
				sender.ItemsSource = l;

			}

			//pause execution and make thread safer... 
			await Task.Delay(200);

		};

	}

	private async Task<List<HistoryItem>> SearchBingApi(string text)
	{
		SdkBingWebSearch sdkBingWebSearch = new();

		SearchResponse result = await sdkBingWebSearch.WebSearchResultTypesLookup(text);

		if (result is null)
		{
			return new List<HistoryItem>();
		}

		List<HistoryItem> items = new();

		foreach (WebPage webPage in result.WebPages?.Value)
		{
			BitmapImage setBitmap = new();

			try
			{
				Uri convertUrl = new(webPage.Url);
				setBitmap.UriSource = new Uri(string.Format("https://www.google.com/s2/favicons?domain_url={0}", convertUrl, convertUrl.Host));

			}
			catch (Exception)
			{

				Console.WriteLine("Failed to set the uri from the web result");

			}

			if (setBitmap is null)
			{
				setBitmap.UriSource = SearchProviders.ProvidersList.Where(x => x.ProviderName.ToLowerInvariant() == "bing").FirstOrDefault().Image.UriSource;
			}


			items.Add(new HistoryItem
			{
				Title = webPage.Name!,
				ImageSource = setBitmap,
				Url = webPage.Url!,
				LastVisitTime = "Bing's safe searching api.."
			});
		}

		return items.DistinctBy(x => x.Title).OrderByDescending(z => z.LastVisitTime).ToList();

	}

	private Task<List<HistoryItem>> SearchControls(string query)
	{
		List<HistoryItem> suggestions = new();


		foreach (HistoryItem item in ViewModel.HistoryItems)
		{
			if (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				suggestions.Add(item);
			}

		}
		foreach (FavItem item in ViewModel.FavoriteItems!)
		{
			HistoryItem converter = new();
			converter.Url = item.Url;
			converter.Title = item.Title;
			converter.ImageSource = new BitmapImage(new(item.IconUrlPath!));
			if (!string.IsNullOrEmpty(converter.Title) && converter.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				suggestions.Add(converter);
			}
		}

		suggestions = suggestions.DistinctBy(t => t.Url).ToList();
		return Task.FromResult(suggestions.OrderByDescending(i => i.Title!.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)).ThenBy(i => i.LastVisitTime).DistinctBy(t => t.Url).DistinctBy(z => z.Title).ToList());
	}

	private async void QueryThis_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if (args.SelectedItem is HistoryItem control)
		{
			sender.Text = control.Url;
			sender.IsSuggestionListOpen = false;
			await Task.Delay(200);
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				window.NavigateToUrl(control.Url);
			}
		}
	}
	private async void NewTab_Loaded(object sender, RoutedEventArgs e)
	{
		// round-robin if one or more newTab's are open apply settings. 
		await ViewModel.Intialize();
		userSettings = ViewModel.SettingsService.CoreSettings;

		//NO need to load because property is attached to viewModel, and also if you select the tab it will call the load event may we can refresh the page... 
		ViewModel.HistoryItems = await HistoryActions.GetAllHistoryItems();
		ViewModel.RaisePropertyChanges(nameof(ViewModel.HistoryItems));

		ViewModel.FavoriteItems = ViewModel.LoadFavorites();
		ViewModel.RaisePropertyChanges(nameof(ViewModel.FavoriteItems));

		SearchengineSelection.SelectedItem = userSettings.EngineFriendlyName;
		NewTabSearchBox.Text = string.Empty;
		_ = NewTabSearchBox.Focus(FocusState.Programmatic);

		if (userSettings.IsTrendingVisible)
		{
			await UpdateTrending();
		}

		HomeSync();
	}


	private async void HomeSync()
	{
		Type.IsOn = userSettings.Auto is true;
		Mode.IsOn = userSettings.LightMode is true;

		ViewModel.BackgroundType = GetBackgroundType(userSettings.Background);
		ViewModel.RaisePropertyChanges(nameof(ViewModel.BackgroundType));

		//var color = (Windows.UI.Color)XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), userSettings.NtpTextColor);
		NewColor.IsEnabled = userSettings.Background is 2;
		// If userSettings.ColorBackground is null, default to a specific color (e.g., Windows.UI.Colors.White)
		// Use Microsoft.UI.Colors instead of Windows.UI.Colors
		NewColorPicker.Color = (Color)XamlBindingHelper.ConvertValue(
	   typeof(Color),
	   string.IsNullOrWhiteSpace(userSettings.ColorBackground)
		   ? Microsoft.UI.Colors.White // Default color
		   : userSettings.ColorBackground
		 );

		// Convert the NtpTextColor setting from a string (if not null) or use a default color
		NtpColorPicker.Color = (Color)XamlBindingHelper.ConvertValue(
			typeof(Color),
			string.IsNullOrWhiteSpace(userSettings.NtpTextColor)
				? Microsoft.UI.Colors.Black // Default color
				: userSettings.NtpTextColor
		);

		//NtpTime.Foreground = NtpDate.Foreground = new SolidColorBrush(color);
		GridSelect.SelectedIndex = userSettings.Background;
		SetVisibilityBasedOnLightMode(userSettings.LightMode is true);
		await Task.CompletedTask;
	}

	private Settings.NewTabBackground GetBackgroundType(int setting)
	{
		return setting switch
		{
			2 => Settings.NewTabBackground.Costum,
			1 => Settings.NewTabBackground.Featured,
			_ => Settings.NewTabBackground.None
		};
	}

	private void SetVisibilityBasedOnLightMode(bool isLightMode)
	{
		Visibility visibility = isLightMode ? Visibility.Collapsed : Visibility.Visible;

		NtpGrid.Visibility = Edit.Visibility = SetTab.Visibility = BigGrid.Visibility = visibility;
	}

	private void GridSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		GridViewItem selection = (sender as GridView)?.SelectedItem as GridViewItem;

		if (selection != null && selection.Tag is string tag)
		{
			SetAndSaveBackgroundSettings(
				tag switch
				{
					"None" => (0, Settings.NewTabBackground.None, false, Visibility.Collapsed),
					"Featured" => (1, Settings.NewTabBackground.Featured, false, Visibility.Visible),
					"Custom" => (2, Settings.NewTabBackground.Costum, true, Visibility.Collapsed),
					_ => throw new ArgumentException("Invalid selection.")
				});
		}
	}
	private async void SetAndSaveBackgroundSettings((int, Settings.NewTabBackground, bool, Visibility) settings)
	{
		(int background, Settings.NewTabBackground backgroundType, bool isNewColorEnabled, Visibility downloadVisibility) = settings;
		userSettings.Background = background;
		ViewModel.BackgroundType = backgroundType;
		NewColor.IsEnabled = isNewColorEnabled;
		Download.Visibility = downloadVisibility;
		await ViewModel.SettingsService?.SaveChangesToSettings(AuthService.CurrentUser, userSettings);
	}
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		param = e.Parameter as Passer;
	}

	public static Brush GetGridBackgroundAsync(Settings.NewTabBackground backgroundType, Fire.Browser.Core.Settings userSettings)
	{
		switch (backgroundType)
		{
			case Settings.NewTabBackground.None:
				return new SolidColorBrush(Colors.Transparent);

			case Settings.NewTabBackground.Costum:
				string colorString = userSettings.ColorBackground?.ToString() ?? "#000000";

				Color color = colorString == "#000000" ?
				 Colors.Transparent :
				 (Windows.UI.Color)XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), colorString);
				return new SolidColorBrush(color);


			case Settings.NewTabBackground.Featured:
				SocketsHttpHandler handler = new()
				{
					EnableMultipleHttp2Connections = true, // Optional but recommended
					SslOptions = new SslClientAuthenticationOptions
					{
						ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
					}
				};

				HttpClient client = new(handler);

				// Static field to cache the ImageBrush so it's fetched only once
				ImageBrush cachedImageBrush = null;

				// Check if the image has already been fetched and cached
				if (cachedImageBrush != null)
				{
					return cachedImageBrush;
				}

				try
				{
					string request = client.GetStringAsync(new Uri("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1")).Result;

					try
					{
						// Deserialize JSON response into ImageRoot object
						ImageRoot images = System.Text.Json.JsonSerializer.Deserialize<ImageRoot>(request);

						// Construct the image URL using data from the API
						Uri imageUrl = new("https://bing.com" + images.images[0].url);

						// Create BitmapImage from the URL
						BitmapImage btpImg = new(imageUrl);

						// Create and return an ImageBrush for WPF
						cachedImageBrush = new ImageBrush()
						{
							ImageSource = btpImg,
							Stretch = Stretch.UniformToFill
						};

						return cachedImageBrush;
					}
					catch (System.Text.Json.JsonException jsonEx)
					{
						// Handle JSON parsing errors
						Console.WriteLine($"Error parsing JSON: {jsonEx.Message}");
					}
					catch (Exception ex)
					{
						// Handle other exceptions
						Console.WriteLine($"Error: {ex.Message}");
					}
				}
				catch (HttpRequestException httpEx)
				{
					// Handle HTTP request exceptions
					Console.WriteLine($"HTTP request error: {httpEx.Message}");
				}
				catch (Exception ex)
				{
					// Handle other exceptions
					Console.WriteLine($"Error: {ex.Message}");
				}
				break;


		}

		return new SolidColorBrush();
	}

	private class ImageRoot
	{
		public Image[] images { get; set; }
	}
	private class Image
	{
		public string url { get; set; }
		public string copyright { get; set; }
		public string copyrightlink { get; set; }
		public string title { get; set; }
	}

	private async Task DownloadImage()
	{
		try
		{
			Fire.Browser.Core.User user = AuthService.CurrentUser;
			string username = user.Username;
			string databasePath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database");
			string imagesFolderPath = Path.Combine(databasePath, "CacheImages");
			string storedDbPath = Path.Combine(databasePath, "StoredDb.json");

			if (!Directory.Exists(imagesFolderPath))
			{
				_ = Directory.CreateDirectory(imagesFolderPath);
			}

			if (!File.Exists(storedDbPath))
			{
				File.WriteAllText(storedDbPath, "[]");

			}

			Guid gd = Guid.NewGuid();
			string imageName = $"{gd}.png";
			string savedImagePath = await new ImageDownloader().SaveGridAsImageAsync(GridImage, imageName, imagesFolderPath);

			StoredImages newImageData = new()
			{
				Name = imageName,
				Location = imagesFolderPath,
				Extension = ".png",
				Primary = false // Adjust this according to your logic
			};

			await new ImagesHelper().AppendToJsonAsync(storedDbPath, newImageData);
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}
	private void UpdateUserSettings(Action<Fire.Browser.Core.Settings> updateAction)
	{
		if (AuthService.CurrentUser != null)
		{
			updateAction.Invoke(userSettings);
			ViewModel.SettingsService.CoreSettings = userSettings;
			UpdateNtpClock();
		}
	}

	private void UpdateNtpClock()
	{
		try
		{
			ViewModel.NtpTimeEnabled = userSettings.NtpDateTime;
			_ = ViewModel.Intialize();
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}

	private void Type_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.Auto = Type.IsOn);
	}

	private void Mode_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.LightMode = Mode.IsOn);
	}

	private void NewColor_TextChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		string newColor = userSettings.ColorBackground = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), NewColorPicker.Color).ToString();
		UpdateUserSettings(userSettings => userSettings.ColorBackground = newColor);
		// raise a change to backgroundtype so that the x:Bind on GridMain will show new backgroundColor {x:Bind local:NewTab.GetGridBackgroundAsync(ViewModel.BackgroundType, userSettings), Mode=OneWay}
		ViewModel.RaisePropertyChanges(nameof(ViewModel.BackgroundType));
	}
	private void DateTimeToggle_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.NtpDateTime = DateTimeToggle.IsOn);
	}

	private void FavoritesToggle_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsFavoritesToggled = FavoritesTimeToggle.IsOn);
	}

	private void HistoryToggle_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsHistoryToggled = HistoryToggle.IsOn);
	}

	private void SearchVisible_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsSearchVisible = SearchVisible.IsOn);
	}

	private void FavsVisible_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsFavoritesVisible = FavsVisible.IsOn);
	}

	private void HistoryVisible_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsHistoryVisible = HistoryVisible.IsOn);
	}

	private void NtpColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		string newColor = userSettings.NtpTextColor = XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), NtpColorPicker.Color).ToString();
		UpdateUserSettings(userSettings => userSettings.NtpTextColor = newColor);
		ViewModel.BrushNtp = new SolidColorBrush(NtpColorPicker.Color);
	}
	private void Download_Click(object sender, RoutedEventArgs e)
	{
		_ = DownloadImage().ConfigureAwait(false);
	}

	private void NewTabSearchBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (!isAuto && e.Key is Windows.System.VirtualKey.Enter && Application.Current is App app && app.m_window is MainWindow window)
		{
			window.FocusUrlBox(NewTabSearchBox.Text);
		}
	}

	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (Application.Current is App app && app.m_window is MainWindow window)
		{
			if (e.AddedItems.Count > 0)
			{
				window.NavigateToUrl((e.AddedItems.FirstOrDefault() as HistoryItem).Url);
			}
		}
	}
	private async void SearchengineSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (e.AddedItems.Count == 0)
			{
				return;
			}

			if (e.AddedItems[0] is SearchProviders selection)
			{
				userSettings.EngineFriendlyName = selection.ProviderName;
				userSettings.SearchUrl = selection.ProviderUrl;
				ViewModel.SearchProvider = selection;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.SearchProvider));
				await ViewModel.SettingsService?.SaveChangesToSettings(AuthService.CurrentUser, userSettings);
				userSettings = ViewModel.SettingsService.CoreSettings;
				//SearchengineSelection.SelectedItem = selection.ProviderName; 

			}
			_ = NewTabSearchBox.Focus(FocusState.Programmatic);
		}
		catch (Exception ex)
		{
			Console.WriteLine("An error occurred: " + ex.Message);
		}
	}


	private void TrendingSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (Application.Current is App app && app.m_window is MainWindow window)
		{
			if (e.AddedItems.Count > 0)
			{
				ViewModel.TrendingItem = e.AddedItems.FirstOrDefault() as TrendingItem;
				window.NavigateToUrl(ViewModel.TrendingItem.webSearchUrl);

			}
		}
	}
	private void TrendingVisible_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsTrendingVisible = TrendingVisible.IsOn);
	}

	private void FloatingBox_Toggled(object sender, RoutedEventArgs e)
	{
		UpdateUserSettings(userSettings => userSettings.IsLogoVisible = FloatingBox.IsOn);
	}

	private void ScrollToSelectedSuggestion(HistoryItem selectedItem)
	{
		// Use Dispatcher to ensure the UI thread is available
		_ = DispatcherQueue.TryEnqueue(() =>
		{
			ListBoxItem container = NewTabSearchBox.ContainerFromItem(selectedItem) as ListBoxItem;
			container?.StartBringIntoView();
		});
	}

	private void FavoritesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is not ListView listView || listView.ItemsSource == null)
		{
			return;
		}

		if (listView.SelectedItem is FavItem item)
		{
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (e.AddedItems.Count > 0)
				{
					window.NavigateToUrl(item.Url);
				}
			}
		}
	}
}