using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Server;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftServer : AbstractServer
    {
        public TBaseServer TheServer { get; set; }
        
        public override void DoExport()
        {
            //DemoServiceImpl handler = new DemoServiceImpl();
            //TProcessor processor = new DemoService.Processor(handler);
            //TServerTransport serverTransport = new TServerSocket(9090);
            //TServer server = new TSimpleServer(processor, serverTransport);

            // Use this for a multithreaded server
            // server = new TThreadPoolServer(processor, serverTransport);
            

            Console.WriteLine($"Starting the thrift server ...");

            //开启服务
            Task.Run(()=> 
            {
                TheServer.ServeAsync(CancellationToken.None).GetAwaiter().GetResult();
            });
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            TheServer.Stop();
        }
    }
}
