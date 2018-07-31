using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpServer : AbstractServer
    {
        //public IList<ServerServiceDefinition> Services { get; set; }
        //public IList<ServerPort> Ports { get; set; }
        //public Server TheServer { get; set; }
        
        
        public override void DoExport()
        {
            
            //var host = new WebHostBuilder()

            //    .UseKestrel()

            //    .UseContentRoot(Directory.GetCurrentDirectory())

            //    .UseIISIntegration()

            //    .UseStartup<Startup>()

            //    .Build();



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

        }
    }
}
