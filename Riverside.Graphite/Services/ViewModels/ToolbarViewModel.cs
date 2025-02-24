using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace Graphite.ViewModels;

public partial class ToolbarViewModel : ObservableObject
{
	[ObservableProperty]
	private bool canGoBack;

	[ObservableProperty]
	private bool canGoForward;

	[ObservableProperty]
	private bool canRefresh;

	[ObservableProperty]
	private string currentAddress = string.Empty;

	[ObservableProperty]
	private string securityIcon = string.Empty;

	[ObservableProperty]
	private string securityIconText = string.Empty;

	[ObservableProperty]
	private string securityText = string.Empty;

	[ObservableProperty]
	private string securityType = string.Empty;

	[ObservableProperty]
	private Visibility homeButtonVisibility = Visibility.Visible;

	public bool CanNavigate => CanGoBack || CanGoForward;

	public void UpdateNavigationState(bool canGoBack, bool canGoForward)
	{
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;
		CanRefresh = true;
	}

	public void UpdateSecurityInfo(string securityType, string securityText, string securityIcon, string securityIconText)
	{
		SecurityType = securityType;
		SecurityText = securityText;
		SecurityIcon = securityIcon;
		SecurityIconText = securityIconText;
	}

	public void UpdateAddress(string address)
	{
		CurrentAddress = address;
	}

	public void SetHomeButtonVisibility(bool isVisible)
	{
		HomeButtonVisibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
	}


}

