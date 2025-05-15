using System.ComponentModel;
using System.ServiceProcess;

namespace FileWatcherService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            // Initialize the service process installer
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Configure the service to run under the LocalService account
            processInstaller.Account = ServiceAccount.LocalService;
            processInstaller.Username = null;
            processInstaller.Password = null;

            // Configure the service properties
            serviceInstaller.ServiceName = "FileWatcherService";
            serviceInstaller.DisplayName = "File Watcher Service";
            serviceInstaller.Description = "Monitors C:\\Folder1 for new files and moves them to C:\\Folder2";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add the installers to the collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
