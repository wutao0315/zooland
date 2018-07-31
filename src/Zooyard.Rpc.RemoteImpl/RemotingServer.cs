using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.RemotingImpl
{
    public class RemotingServer : AbstractServer
    {
        public IList<RemotingService> Services { get; set; } = new List<RemotingService>();
        public override void DoExport()
        {
            //开启服务
            foreach (var item in Services)
            {
                item.Open();
                //向注册中心发送服务注册信息
            }


            // Step 3 of the hosting procedure: Add a service endpoint.

            Console.WriteLine($"Starting the remoting server ...");
           
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            foreach (var item in Services)
            {
                item.Dispose();
            }
        }
    }
}
