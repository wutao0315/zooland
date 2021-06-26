
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
        Task<string> CallNameVoidAsync();
        Task CallNameAsync(string name);
        Task CallVoidAsync();
        Task<string> HelloAsync(string name);
        Task<HelloData> SayHelloAsync(string name);
        Task<string> ShowHelloAsync(HelloData name);
    }
}
