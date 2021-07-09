
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace RpcContractHttp
{
    [Description("/hello")]
    public interface IHelloService
    {
        [Description("CallNameVoid|get&_")]
        Task<string> CallNameVoidAsync();
        [Description("CallName|get&_")]
        Task CallNameAsync(string name);

        [Description("CallVoid|get&_")]
        Task CallVoidAsync();
        [Description("Hello|get&_")]
        Task<string> HelloAsync(string name);
        [Description("SayHello|get&_")]
        Task<HelloResult> SayHelloAsync(string name);
        [Description("ShowHello|post&application/json")]
        Task<string> ShowHelloAsync(HelloResult name);



        [Description("CallNameVoid|get&_")]
        string CallNameVoid();
        [Description("CallName|get&_")]
        void CallName(string name);
        [Description("CallVoid|get&_")]
        void CallVoid();
        [Description("Hello|get&_")]
        string Hello(string name);
        [Description("SayHello|get&_")]
        HelloResult SayHello(string name);
        [Description("ShowHello|post&application/json")]
        string ShowHello(HelloResult name);
    }
}
