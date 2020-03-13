using Akka.Actor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaClient : AbstractClient
    {
        public override URL Url { get; }
        private readonly ActorSystem _actorSystem;
        private readonly int _timeout;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public AkkaClient(ActorSystem actorSystem, URL url, int timeout, ILoggerFactory loggerFactory)
        {
            _actorSystem = actorSystem;
            this.Url = url;
            _timeout = timeout;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AkkaClient>();
        }

        public override async Task<IInvoker> Refer()
        {
            await Task.CompletedTask;
            return new AkkaInvoker(_actorSystem, Url, _timeout, _loggerFactory);
        }

        public override async Task Open()
        {
            await Task.CompletedTask;
        }

        public override async Task Close()
        {
            await DisposeAsync();
        }

        public override async Task DisposeAsync()
        {
            _actorSystem.Dispose();
            await Task.CompletedTask;
        }
    }
}
