using System;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractClient: IClient
    {
        public virtual string Version { get { return Url.GetParameter(URL.VERSION_KEY); } }
        public DateTime ActiveTime { get; set; } = DateTime.Now;
        public abstract URL Url { get; }
        public abstract Task<IInvoker> Refer();
        public abstract Task Open();
        public abstract Task Close();
        public abstract ValueTask DisposeAsync();
        public virtual void Reset() { }
    }
}
