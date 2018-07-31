
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfClient : AbstractClient
    {
        public override URL Url { get; }
        public ICommunicationObject Channel { get; private set; }
        //private ChannelFactory ChannelFactory { get; set; }
        //public Binding Binding { get; private set; }
        //public EndpointAddress EndpointAddress { get; private set; }
        public WcfClient(ICommunicationObject channel, URL url)
        {
            this.Channel = channel;
            this.Url = url;
        }

        public override IInvoker Refer()
        {
            Open();

            //grpc client service
            //R channel = ChannelFactory.CreateChannel();
            return new WcfInvoker(Channel);
        }

        public override void Open()
        {
            //if (Channel == null || Channel.State == CommunicationState.Closing || Channel.State == CommunicationState.Closed)
            //{
            //    Channel = ChannelFactory.GetType().GetMethod("CreateChannel", new Type[] { }).Invoke(ChannelFactory, null) as ICommunicationObject;
            //}

            if (Channel.State != CommunicationState.Opened &&
               Channel.State != CommunicationState.Opening)
            {
                Channel.Open();
            }
        }

        public override void Close()
        {
            if (Channel.State != CommunicationState.Closed &&
                 Channel.State != CommunicationState.Closing)
            {
                try
                {
                    Channel.Close();
                }
                catch { }
            }
        }

        public override void Dispose()
        {
            if (Channel.State != CommunicationState.Closed &&
                 Channel.State != CommunicationState.Closing)
            {
                try
                {
                    Channel.Close();
                    Channel.Abort();
                }
                catch { }
            }
        }


    }
}
