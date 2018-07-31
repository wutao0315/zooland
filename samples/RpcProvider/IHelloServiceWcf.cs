using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RpcProvider
{
    [DataContract]
    public class HelloResult
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Gender { get; set; }
        [DataMember]
        public string Head { get; set; }
    }


    [ServiceContract(Namespace = "http://RpcProvider")]
    public interface IHelloServiceWcf
    {
        [OperationContract]
        string CallNameVoid();
        [OperationContract]
        void CallName(string name);
        [OperationContract]
        void CallVoid();
        [OperationContract]
        string Hello(string name);
        [OperationContract]
        HelloResult SayHello(string name);
        [OperationContract]
        string ShowHello(HelloResult name);
    }
    
}
