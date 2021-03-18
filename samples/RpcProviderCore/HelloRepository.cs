using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcProviderCore
{
    public interface IHelloRepository
    {
        string SayHello();
    
    }
    public class HelloRepository: IHelloRepository
    {
        public string SayHello() 
        {
            return "Hello Repository";
        }
    }
}
