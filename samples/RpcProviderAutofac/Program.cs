using Common.Logging;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RpcProviderAutofac
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));
        public const string HostName = "RpcProvider";

        static void Main(string[] args)
        {

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            if (!Environment.UserInteractive) // Running as service
            {
                using (var service = new Service())
                {
                    ServiceBase.Run(service);
                }
            }
            else // Running as console app
            {
                var parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[]
                        {"/LogFile=", Assembly.GetExecutingAssembly().Location});
                        return;

                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[]
                        {"/LogFile=", "/u", Assembly.GetExecutingAssembly().Location});
                        return;
                }

                try
                {
                    Console.Title = $"{HostName} {AppDomain.CurrentDomain.BaseDirectory}";

                    var hostDomain = AppDomain.CreateDomain($"{HostName}.Host", null, new AppDomainSetup
                    {
                        AppDomainInitializer = Startup,
                        AppDomainInitializerArguments = args,
                    });

                    Console.ReadLine();

                    hostDomain.DoCallBack(Shutdown);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    Console.Out.WriteLine(e);

                    Console.ReadLine();
                }
            }
        }

        static void Startup(string[] args)
        {
            Logger.Info($"{HostName} - Starting ...");
            Logger.Title = $"{HostName} - Starting ...";

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var buildTime = Utils.RetrieveLinkerTimestamp(Assembly.GetExecutingAssembly().Location);
            var programName = $"{HostName} {version.Major}.{version.Minor}.{version.Build}";
            Logger.Info($"{programName} (build at {buildTime.ToString(CultureInfo.CurrentCulture)})\n");
            Logger.Info("Starting...");

            //var options = ReadOptions();
            //var rules = ReadRules();
            //var listenEndpoints = options.ListenOn.Split(',');
            //var startedEvent = new CountdownEvent(listenEndpoints.Length);

            try
            {
                // Using Spring's IoC container
                // Force Spring to load configuration

                //lock (ContextRegistry.GetContext())
                //{

                //}

                Console.Out.WriteLine("Server listening...");
                Console.Out.WriteLine("Press any key to stop the server...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex Message:{ex.Message}");
                Console.WriteLine($"Ex Source:{ex.Source}");
                Console.WriteLine($"Ex StackTrace:{ex.StackTrace}");
                Console.Out.WriteLine("--- Press <return> to quit ---");
                _logger.Error(ex);
            }
        }

        static void Shutdown()
        {
            //lock (ContextRegistry.GetContext())
            //{
            //    ContextRegistry.GetContext().Dispose();
            //}
            Logger.Info($"{HostName} has been stopped.");
        }

        #region Nested class to support running as service

        private class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = $"{HostName}.Host_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            }

            private AppDomain hostDomain;

            protected override void OnStart(string[] args)
            {
                hostDomain = AppDomain.CreateDomain($"{HostName}", null, new AppDomainSetup
                {
                    AppDomainInitializer = Startup,
                    AppDomainInitializerArguments = args,
                });
                base.OnStart(args);
            }

            protected override void OnStop()
            {
                hostDomain.DoCallBack(Shutdown);
                base.OnStop();
            }
        }

        #endregion
    }
}
