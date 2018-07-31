using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfChannelFactory
    {
        /// <summary>
        /// WCF服务名称
        /// </summary>
        internal string ServiceName { set; get; }

        /// <summary>
        /// 通道类型
        /// </summary>
        internal Type ChanelType { set; get; }

        /// <summary>
        /// 协议
        /// </summary>
        internal string Protocol { set; get; }

        /// <summary>
        /// 服务路径
        /// </summary>
        internal string ServicePath { set; get; }

        /// <summary>
        /// 客户端服务超时设置
        /// </summary>
        internal int ClientTimeout { get; set; }

        /// <summary>
        /// 通道工厂
        /// </summary>
        protected Dictionary<string, ChannelFactory> factories = new Dictionary<string, ChannelFactory>();



        //protected void CreatePool(string[] hosts)
        //{
        //    lock (locker)
        //    {
        //        //结点列表初始化
        //        Hosts = hosts;

        //        //版本增加
        //        Version++;

        //        //读取配置
        //        MaxActive = Config.GetIntValue("MaxActive", 100);
        //        MinIdle = Config.GetIntValue("MinIdle", 2);
        //        MaxIdle = Config.GetIntValue("MaxIdle", 10);
        //        ClientTimeout = Config.GetIntValue("ClientTimeout", 5000);
        //        ServiceName = Config["ServiceName"];
        //        ChanelType = Type.GetType(Config["ChanelType"]);
        //        Protocol = Config.GetStringValue("Protocol", "http://");
        //        ServicePath = Config.GetStringValue("ServicePath", "/" + ServiceName + ".svc");

        //        if (clientsPool == null)
        //        {
        //            clientsPool = new Queue<ISerClient>();
        //        }
        //        else
        //        {
        //            while (idleCount > 0)
        //            {
        //                DestoryClient(DequeueClient());
        //            }

        //            foreach (var factory in factories)
        //            {
        //                if (factory.Value.State != CommunicationState.Closed &&
        //                    factory.Value.State != CommunicationState.Closing)
        //                {
        //                    try
        //                    {
        //                        factory.Value.Close();
        //                    }
        //                    catch { }
        //                }
        //            }
        //            factories.Clear();

        //            clientsPool = new Queue<ISerClient>();
        //        }
        //    }
        //}


        /// <summary>
        /// 获取与服务器地址信息一致的通道工厂
        /// </summary>
        /// <param name="hostInfo">服务器地址信息</param>
        /// <returns>通道工厂</returns>
        protected ChannelFactory GetChannelFactory(string hostInfo)
        {
            if (factories.ContainsKey(hostInfo))
            {
                return factories[hostInfo];
            }
            else
            {
                var endPointAddr = new EndpointAddress(Protocol + hostInfo + ServicePath);
                Binding binding = null;
                switch (Protocol.ToLower())
                {
                    case "http://":
                        binding = new BasicHttpBinding();
                        break;
                    case "net.tcp://":
                        binding = new NetTcpBinding();
                        break;
                    default:
                        binding = new BasicHttpBinding();
                        break;
                }

                binding.SendTimeout = TimeSpan.FromMilliseconds(ClientTimeout);
                binding.ReceiveTimeout = TimeSpan.FromMilliseconds(ClientTimeout);
                var factory = typeof(ChannelFactory<>)
                    .MakeGenericType(ChanelType)
                    .GetConstructor(new Type[] { typeof(Binding), typeof(EndpointAddress) })
                    .Invoke(new object[] { binding, endPointAddr }) as ChannelFactory;
                factories.Add(hostInfo, factory);
                return factory;
            }
        }
    }
}
