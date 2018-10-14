using Akka.Actor;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaClient : AbstractClient
    {
        public override URL Url { get; }
        private ActorSystem TheActorSystem { get; set; }
        private int Timeout { get; set; }

        public AkkaClient(ActorSystem actorSystem,URL url,int timeout)
        {
            this.TheActorSystem = actorSystem;
            this.Url = url;
            this.Timeout = timeout;
        }

        public override IInvoker Refer()
        {
            return new AkkaInvoker(TheActorSystem, Url, Timeout);
        }

        public override void Open()
        {

        }

        public override void Close()
        {
            TheActorSystem.Dispose();

        }
        public override void Dispose()
        {
            TheActorSystem.Dispose();
        }
    }
}
