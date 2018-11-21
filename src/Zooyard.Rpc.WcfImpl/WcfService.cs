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
    public class WcfService : IDisposable
    {
        private readonly object _singletonInstance;
        private readonly Type _contractType;
        private readonly IList<Uri> _baseAddresses= new List<Uri>();
        private readonly Binding _binding;

#if NET461
        private readonly IList<IServiceBehavior> _serviceBehaviors;
        private ServiceHost serviceHost;
        public WcfService(object singletonInstance,
        Type contractType,
        IList<Uri> baseAddresses,
        Binding binding,
        IList<IServiceBehavior> serviceBehaviors)
        {
            _singletonInstance = singletonInstance;
            _contractType = contractType;
            _baseAddresses = baseAddresses;
            _binding = binding;
            _serviceBehaviors = serviceBehaviors;
        }
#endif 
        public void Open()
        {
            try
            {
                // Step 4 of the hosting procedure: Enable metadata exchange.
                //var smb = new ServiceMetadataBehavior();
                //smb.HttpGetEnabled = true;

#if NET461
                serviceHost = new ServiceHost(_singletonInstance, _baseAddresses.ToArray());
                serviceHost.AddServiceEndpoint(
                   _contractType,
                   _binding,
                   _singletonInstance.GetType().Name);

                if (_serviceBehaviors != null)
                {
                    foreach (var item in _serviceBehaviors)
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
