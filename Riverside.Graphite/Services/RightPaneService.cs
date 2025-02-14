using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using System.Threading;
using System.Threading.Tasks;


namespace WebDive.Services;

	public class RightPaneService : IRightPaneService
	{
		private readonly IPageService _pageService;

		private SplitView _splitView;
		private Frame _frame;
		private object _lastParamUsed;

		public RightPaneService(IPageService pageService)
		{
			_pageService = pageService;
		}

		public void Initialize(Frame rightPaneFrame, SplitView splitView)
		{
			_splitView = splitView;
			_frame = rightPaneFrame;
			_frame.Navigated += OnNavigated;
		}

	/*
	 *  1. PageService Registers the ViewModels, and Pages. -> static ListofAllPages. 
	 *  2. Viewmodels must inherit INavigationAware. -> tracks side frame navigation, and allows for event handlers to be either (init/disposed)
	 *  3. RightPane never opens a type that is already in the frame -> content of pageType. 
	 *  4. Could add nav buttons to pan if needed. 
	 *  5. Cross Reference previos ViewModels to kill processes or event handlers with 'vmBeforeNavigatioin';
	 *  2025-02-10 jd. 
	 */
	
	public async void OpenInRightPane(string pageKey, object parameter = null)
	{
		SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
		

		using (_semaphoreSlim.WaitAsync())
			{
			// Don't open the same page multiple times
			try
			{
					if (_frame.GetPageViewModel() == null || _frame.GetPageViewModel().GetType().FullName != pageKey || (parameter != null && !parameter.Equals(_lastParamUsed)))
					{
						var pageType = _pageService.GetPageType(pageKey);
						var vmBeforeNavigation = _frame.GetPageViewModel();
						var navigationResult = _frame.Navigate(pageType, parameter);
						if (navigationResult)
						{
							_lastParamUsed = parameter;
							if (vmBeforeNavigation is INavigationAware navigationAware)
							{
								navigationAware.OnNavigatedFrom();
							}
						}
					}
					_splitView.OpenPaneLength = (App.Current.m_window.Bounds.Width * .33) >= 400 ? (App.Current.m_window.Bounds.Width * .33) : 400;
					_splitView.IsPaneOpen = true;
				await Task.Delay(100); 
				}
				catch (System.Exception ex)
				{
					ExceptionLogger.LogException(ex);	
					
				}
				finally
				{
					_semaphoreSlim.Release();
				}	
			}
		
		}

		public void CleanUp()
		{
			_frame.Navigated -= OnNavigated;
		}

		private void OnNavigated(object sender, NavigationEventArgs e)
		{
			if (sender is Frame frame)
			{
				frame.BackStack.Clear();
				if (frame.GetPageViewModel() is INavigationAware navigationAware)
				{
					navigationAware.OnNavigatedTo(e.Parameter);
				}
			}
		}
	}

