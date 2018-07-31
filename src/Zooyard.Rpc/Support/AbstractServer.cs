using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractServer : IServer
    {
        /// <summary>
        /// 注册中心发现机制
        /// </summary>
        public IRegistryHost RegistryHost { get; set; }
        public string Address { get; set; }


        public void Export()
        {
            //first start the service provider
            DoExport();

            if (!string.IsNullOrWhiteSpace(Address))
            {
                var url = URL.valueOf(Address);
                //registe this provoder
                RegistryHost.RegisterService(url);
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(Address))
            {
                var url = URL.valueOf(Address);
                //first unregiste this provider
                RegistryHost.DeregisterService(url);
            }
            //them stop the provider
            DoDispose();
        }

        public abstract void DoDispose();
        
        public abstract void DoExport();
    }
}
