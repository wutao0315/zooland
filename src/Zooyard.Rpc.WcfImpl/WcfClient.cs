using System.ServiceModel;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfClient : AbstractClient
    {
        public override URL Url { get; }
        public ICommunicationObject Channel { get; private set; }
        public WcfClient(ICommunicationObject channel, URL url)
        {
            this.Channel = channel;
            this.Url = url;
        }

        public override IInvoker Refer()
        {
            Open();

            return new WcfInvoker(Channel);
        }

        public override void Open()
        {

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
