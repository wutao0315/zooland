using Grpc.Core;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcClient : AbstractClient
    {
        public override URL Url { get; }
        public GrpcClient(Channel channel,object grpcClient, URL url, ChannelCredentials channelCredentials, int clientTimeout)
        {
            this.Url = url;
            this.TheChannel = channel;
            this.ChannelCredentials = channelCredentials;
            this.TheGrpcClient = grpcClient;
            this.ClientTimeout = clientTimeout;
        }
        /// <summary>
        /// 传输层
        /// </summary>
        protected Channel TheChannel { get; private set; }
        protected ChannelCredentials ChannelCredentials { get; private set; }
        protected int ClientTimeout { get; private set; }
        protected object TheGrpcClient { get; private set; }

        

        public override IInvoker Refer()
        {
            if (TheChannel!=null)
            {
                if (TheChannel.State==ChannelState.Shutdown)
                {
                    TheChannel = new Channel(TheChannel.Target, ChannelCredentials);
                }
            }

            Open();
            //grpc client service

            return new GrpcInvoker(TheGrpcClient, ClientTimeout);
        }

        public override void Open()
        {
            if (TheChannel.State != ChannelState.Ready)
            {
                TheChannel.ConnectAsync().Wait(ClientTimeout / 2);
            }
            if (TheChannel.State != ChannelState.Ready)
            {
                throw new Grpc.Core.RpcException(Status.DefaultCancelled, "connect failed");
            }
        }

        public override void Close()
        {
            if (TheChannel != null)
            {
                TheChannel.ShutdownAsync().Wait();
            }
        }

        public override void Dispose()
        {
            if (TheChannel!=null)
            {
                TheChannel.ShutdownAsync().Wait();
            }
        }
    }
}
