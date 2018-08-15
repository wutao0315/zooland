using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfService:IDisposable
    {
        public object SingletonInstance { get; set; }
        public Type ContractType { get; set; }
        public IList<Uri> BaseAddresses { get; set; } = new List<Uri>();

        public IList<IServiceBehavior> ServiceBehaviors { get; set; }
        public Binding TheBinding { get; set; }

        private ServiceHost serviceHost { get; set; }
        public void Open()
        {
            try
            {
                serviceHost = new ServiceHost(SingletonInstance, BaseAddresses.ToArray());
                serviceHost.AddServiceEndpoint(
                   ContractType,
                   TheBinding,
                   SingletonInstance.GetType().Name);

                // Step 4 of the hosting procedure: Enable metadata exchange.
                //var smb = new ServiceMetadataBehavior();
                //smb.HttpGetEnabled = true;

                if (ServiceBehaviors != null)
                {
                    foreach (var item in ServiceBehaviors)
                    {
                        serviceHost.Description.Behaviors.Add(item);
                    }
                }
                serviceHost.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            
        }

        public void Dispose()
        {
            if (serviceHost!=null)
            {
                serviceHost.Close();
                serviceHost.Abort();
            }
        }
    }
}
