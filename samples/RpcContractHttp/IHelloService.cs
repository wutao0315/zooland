
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractHttp
{
    public interface IHelloService
    {
        Task<string> CallNameVoid();
        Task CallName(string name);
        Task CallVoid();
        Task<string> Hello(string name);
        Task<HelloResult> SayHello(string name);
        Task<string> ShowHello(HelloResult name);
    }
}
