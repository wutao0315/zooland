using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcServer : AbstractServer
    {
        public IList<ServerServiceDefinition> Services { get; set; }
        public IList<ServerPort> Ports { get; set; }
        public Server TheServer { get; set; }
        
        
        public override void DoExport()
        {
            //DemoServiceImpl handler = new DemoServiceImpl();
            //TProcessor processor = new DemoService.Processor(handler);
            //TServerTransport serverTransport = new TServerSocket(9090);
            //TServer server = new TSimpleServer(processor, serverTransport);

            // Use this for a multithreaded server
            // server = new TThreadPoolServer(processor, serverTransport);


            //Server.ServiceDefinitionCollection cc = new Server.ServiceDefinitionCollection { new ServerServiceDefinition() { } };
            foreach (var item in Services)
            {
                TheServer.Services.Add(item);
            }
            foreach (var item in Ports)
            {
                TheServer.Ports.Add(item);
            }
            //开启服务
            TheServer.Start();
            Console.WriteLine($"Starting the grpc server ...");
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            if (TheServer != null)
            {
                TheServer.ShutdownAsync().GetAwaiter().GetResult();
                TheServer = null;
            }

        }
    }
}
