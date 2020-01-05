using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IClient : IDisposable
    {
        URL Url { get; }
        IInvoker Refer();
        string Version { get; }
        DateTime ActiveTime { get; set; }
        void Open();
        Task OpenAsync();
        void Close();
        Task CloseAsync();
        void Reset();
        
    }
}
