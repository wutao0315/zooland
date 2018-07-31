using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RpcProviderAutofac
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = $"{Program.HostName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                StartType = ServiceStartMode.Automatic
            };

            // Automatically start after install
            AfterInstall += (sender, args) =>
            {
                using (var serviceController = new ServiceController(serviceInstaller.ServiceName))
                    serviceController.Start();
            };

            Installers.AddRange(new Installer[]
            {
                serviceProcessInstaller,
                serviceInstaller
            });
        }
    }
}
