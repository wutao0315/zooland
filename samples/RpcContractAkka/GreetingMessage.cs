using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractAkka
{
    public class GreetingMessage
    {
    }
    public class NameResult
    {
        public string Name { get; set; }
    }
    public class HelloResult
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Head { get; set; }
    }
}
