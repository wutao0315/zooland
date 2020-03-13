using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IClient : IDisposable
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
