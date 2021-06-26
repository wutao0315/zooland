
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractGrpc
{
    public interface IHelloService
    {
        Task<NameResult> CallNameVoidAsync(Void voidData);
        Task<Void> CallNameAsync(NameResult name);
        Task<Void> CallVoidAsync(Void voidData);
        Task<NameResult> HelloAsync(NameResult name);
        Task<HelloResult> SayHelloAsync(NameResult name);
        Task<NameResult> ShowHelloAsync(HelloResult name);
    }
}
