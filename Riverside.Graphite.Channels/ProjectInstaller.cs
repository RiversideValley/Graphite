using System.ServiceProcess;

public class ProjectInstaller : System.Configuration.Install.Installer
{
	private ServiceProcessInstaller processInstaller;
	private ServiceInstaller serviceInstaller;

	public ProjectInstaller()
	{
		processInstaller = new ServiceProcessInstaller
		{
			Account = ServiceAccount.LocalSystem
		};

		serviceInstaller = new ServiceInstaller
		{
			ServiceName = "GraphiteChannel",
			Description = "A Windows Service hosting Azure SignalR",
			StartType = ServiceStartMode.Automatic
		};

		Installers.Add(processInstaller);
		Installers.Add(serviceInstaller);
	}
}
