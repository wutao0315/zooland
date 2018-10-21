
using System;
using Zooyard.Rpc.Support;
#if NET461
//using Microsoft.Owin.Hosting;
#endif
namespace Zooyard.Rpc.HttpImpl
{
    public class HttpServer<T> : AbstractServer
    {
        public string WebApiUrl { get; set; } = "http://localhost:10010/";
        private static IDisposable webApiApp;

        public override void DoExport()
        {

            //var host = new WebHostBuilder()

            //    .UseKestrel()

            //    .UseContentRoot(Directory.GetCurrentDirectory())

            //    .UseIISIntegration()

            //    .UseStartup<Startup>()

            //    .Build();

            try
            {
#if NET461
                //webApiApp = WebApp.Start<T>(url: WebApiUrl);
#endif
                Console.Out.WriteLine("Server listening...");
                Console.Out.WriteLine("Press any key to stop the server...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex Message:{ex.Message}");
                Console.WriteLine($"Ex Source:{ex.Source}");
                Console.WriteLine($"Ex StackTrace:{ex.StackTrace}");
                Console.Out.WriteLine("--- Press <return> to quit ---");
                //_logger.Error(ex);
            }

            //host.Run();
            Console.WriteLine($"Starting the grpc server ...");
            //向注册中心发送服务注册信息
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            //if (TheServer != null)
            //{
            //    TheServer.ShutdownAsync().Wait();
            //}
            webApiApp.Dispose();
        }
    }
}
