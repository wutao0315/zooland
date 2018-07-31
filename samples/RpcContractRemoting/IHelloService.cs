using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractRemoting
{
    public interface IHelloService
    {
        string CallNameVoid();
        void CallName(string name);
        void CallVoid();
        string Hello(string name);
        HelloResult SayHello(string name);
        string ShowHello(HelloResult name);
    }
}
