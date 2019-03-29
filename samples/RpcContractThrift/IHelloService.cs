
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
        string CallNameVoid();
        void CallName(string name);
        void CallVoid();
        string Hello(string name);
        HelloData SayHello(string name);
        string ShowHello(HelloData name);
    }
}
