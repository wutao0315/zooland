
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractGrpc
{
    public interface IHelloService
    {
        NameResult CallNameVoid(Void voidData);
        Void CallName(NameResult name);
        Void CallVoid(Void voidData);
        NameResult Hello(NameResult name);
        HelloResult SayHello(NameResult name);
        NameResult ShowHello(HelloResult name);
    }
}
