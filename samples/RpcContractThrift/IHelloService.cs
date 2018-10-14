
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcContractThrift
{
    public interface IHelloService
    {
        Task<string> CallNameVoidAsync(CancellationToken cancellationToken);
        Task CallNameAsync(string name, CancellationToken cancellationToken);
        Task CallVoidAsync(CancellationToken cancellationToken);
        Task<string> HelloAsync(string name, CancellationToken cancellationToken);
        Task<HelloData> SayHelloAsync(string name, CancellationToken cancellationToken);
        Task<string> ShowHelloAsync(HelloData name, CancellationToken cancellationToken);
    }
}
