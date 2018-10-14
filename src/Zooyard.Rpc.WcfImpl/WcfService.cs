using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
#if NET461
using System.ServiceModel.Description;
#endif 


namespace Zooyard.Rpc.WcfImpl
{
    public class WcfService:IDisposable
    {
        public object SingletonInstance { get; set; }
        public Type ContractType { get; set; }
        public IList<Uri> BaseAddresses { get; set; } = new List<Uri>();
        public Binding TheBinding { get; set; }
#if NET461
        public IList<IServiceBehavior> ServiceBehaviors { get; set; }
        private ServiceHost serviceHost { get; set; }
#endif 
        public void Open()
        {
            try
            {
                // Step 4 of the hosting procedure: Enable metadata exchange.
                //var smb = new ServiceMetadataBehavior();
                //smb.HttpGetEnabled = true;

#if NET461
                serviceHost = new ServiceHost(SingletonInstance, BaseAddresses.ToArray());
                serviceHost.AddServiceEndpoint(
                   ContractType,
                   TheBinding,
                   SingletonInstance.GetType().Name);

                if (ServiceBehaviors != null)
                {
                    foreach (var item in ServiceBehaviors)
                    {
                        serviceHost.Description.Behaviors.Add(item);
                    }
                }
                serviceHost.Open();
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            
        }

        public void Dispose()
        {
#if NET461
            if (serviceHost!=null)
            {
                serviceHost.Close();
                serviceHost.Abort();
            }
#endif
        }
    }
}
