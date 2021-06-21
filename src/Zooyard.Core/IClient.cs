using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IClient : IDisposable,IAsyncDisposable
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
