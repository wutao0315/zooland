using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IClient : IAsyncDisposable
    {
        URL Url { get; }
        Task<IInvoker> Refer();
        string Version { get; }
        DateTime ActiveTime { get; set; }
        Task Open();
        Task Close();
        void Reset();
        
    }
}
