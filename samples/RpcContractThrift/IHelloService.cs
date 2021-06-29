
using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        string CallNameVoid();
        void CallName(string name);
        void CallVoid();
        string Hello(string name);
        HelloData SayHello(string name);
        string ShowHello(HelloData name);
    }
}
