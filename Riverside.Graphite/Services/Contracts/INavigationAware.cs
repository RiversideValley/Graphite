namespace Riverside.Graphite.Services.Contracts
{
	public interface INavigationAware
	{
		void OnNavigatedTo(object parameter);

		void OnNavigatedFrom();
	}
}
