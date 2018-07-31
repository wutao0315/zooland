using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractRemoting
{
    [Serializable]
    public class HelloResult
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Head { get; set; }
    }
}
