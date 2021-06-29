
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractHttp
{
    public interface IHelloService
    {
        Task<string> CallNameVoidAsync();
        Task CallNameAsync(string name);
        Task CallVoidAsync();
        Task<string> HelloAsync(string name);
        Task<HelloResult> SayHelloAsync(string name);
        Task<string> ShowHelloAsync(HelloResult name);


        string CallNameVoid();
        void CallName(string name);
        void CallVoid();
        string Hello(string name);
        HelloResult SayHello(string name);
        string ShowHello(HelloResult name);
    }
}
