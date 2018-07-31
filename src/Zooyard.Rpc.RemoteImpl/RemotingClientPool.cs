using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;
using Zooyard.Rpc.RemotingImpl;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace Zooyard.Rpc.RemotingImpl
{
    public class RemotingClientPool : AbstractClientPool
    {
        public const string PROXY_KEY = "proxy";

        public IDictionary<string, Type> TheRemotingTypes { get; set; }
        //public IDictionary<string, Type> TheProtocolTypes { get; set; }
        public IDictionary<string, IChannelReceiver> TheProtocolChannels { get; set; }

        public const string ENSURESECURITY_KEY = "EnsureSecurity";

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化

            //if (!TheProtocolTypes.ContainsKey(url.Protocol))
            //{
            //    throw new RpcException("not find the protocol types");
            //}
            //var channelType = TheProtocolTypes[url.Protocol];

            //var channel = channelType.GetConstructor(new Type[] { }).Invoke(new object[] { }) as IChannel;

            if (!TheProtocolChannels.ContainsKey(url.Protocol))
            {
                throw new RpcException("not find the protocol channel");
            }
            var channel = TheProtocolChannels[url.Protocol];
            
            var ensureSecurity = url.GetParameter<bool>(ENSURESECURITY_KEY, false);
            
            if (!ChannelServices.RegisteredChannels.Contains(channel))
            {
                ChannelServices.RegisterChannel(channel, ensureSecurity);
            }


            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !TheRemotingTypes.ContainsKey(proxyKey))
            {
                throw new RpcException("not find the proxy remoting client");
            }

            var remotingType = TheRemotingTypes[proxyKey];
            
            return new RemotingClient(channel, remotingType, url);
        }
    }
}
