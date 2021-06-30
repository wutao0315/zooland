
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
        [Description("CallNameVoid|get&url")]
        Task<string> CallNameVoidAsync();
        [Description("CallName|get&url")]
        Task CallNameAsync(string name);

        [Description("CallVoid|get&url")]
        Task CallVoidAsync();
        [Description("Hello|get&url")]
        Task<string> HelloAsync(string name);
        [Description("SayHello|get&url")]
        Task<HelloResult> SayHelloAsync(string name);
        [Description("ShowHello|post&json")]
        Task<string> ShowHelloAsync(HelloResult name);



        [Description("CallNameVoid|get&url")]
        string CallNameVoid();
        [Description("CallName|get&url")]
        void CallName(string name);
        [Description("CallVoid|get&url")]
        void CallVoid();
        [Description("Hello|get&url")]
        string Hello(string name);
        [Description("SayHello|get&url")]
        HelloResult SayHello(string name);
        [Description("ShowHello|post&json")]
        string ShowHello(HelloResult name);
    }
}
