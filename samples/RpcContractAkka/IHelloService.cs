
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractAkka
{
    public interface IHelloService
    {
        NameResult CallNameVoid();
        void CallName(NameResult name);
        void CallVoid();
        NameResult Hello(NameResult name);
        HelloResult SayHello(NameResult name);
        NameResult ShowHello(HelloResult name);
    }
}
