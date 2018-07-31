using System;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractClient: Core.IClient
    {
        public virtual string Version { get { return Url.GetParameter(URL.VERSION_KEY); } }
        public DateTime ActiveTime { get; set; } = DateTime.Now;
        public abstract URL Url { get; }
        public abstract IInvoker Refer();
        public abstract void Open();
        public abstract void Close();
        public abstract void Dispose();
        public virtual void Reset() { }
    }
}
