using System;

namespace Zooyard.Core
{
    public interface IClient : IDisposable
    {
        URL Url { get; }
        IInvoker Refer();
        string Version { get; }
        DateTime ActiveTime { get; set; }
        void Open();
        void Close();
        void Reset();
        
    }
}
