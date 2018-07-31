
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

namespace Zooyard.Rpc.RemotingImpl
{
    public class RemotingClient : AbstractClient
    {
        public override URL Url { get; }
        private object RemotingObject { get; set; }
        private Type RemotingObjectType { get; set; }
        private IChannelReceiver Channel { get; set; }

        public RemotingClient(IChannelReceiver channel,Type remotingObjectType, URL url)
        {
            this.Url = url;
            this.RemotingObjectType = remotingObjectType;
            this.Channel = channel;
        }

        public override IInvoker Refer()
        {
            Open();

            //grpc client service
            //R channel = ChannelFactory.CreateChannel();
            return new RemotingInvoker(RemotingObject);
        }

        public override void Open()
        {
            Channel.StopListening(null);
            Channel.StartListening(null);

            if (RemotingObject == null)
            {
                RemotingObject = RemotingServices.Connect(RemotingObjectType, $"{Url.Protocol}://{Url.Host}:{Url.Port}/{Url.Path}");
                //RemotingObject = Activator.GetObject(RemotingObjectType, $"{Url.Protocol}://{Url.Host}:{Url.Port}/{Url.Path}");
            }
        }

        public override void Close()
        {
            Channel.StopListening(null);
            RemotingObject = null;
        }

        public override void Dispose()
        {
            this.Close();
        }


    }
}
