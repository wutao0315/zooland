using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift;
using Thrift.Server;
using Thrift.Transports;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClient : AbstractClient
    {
        public override URL Url { get; }
        /// <summary>
        /// 传输层
        /// </summary>
        public TClientTransport Transport { get; private set; }
        public IDisposable Thriftclient { get; private set; }

        public ThriftClient(TClientTransport transport, IDisposable thriftclient,URL url)
        {
            this.Transport = transport;
            this.Thriftclient = thriftclient;
            this.Url = url;
        }


        public override IInvoker Refer()
        {
            this.Open();
            //thrift client service
            return new ThriftInvoker(Thriftclient);
        }

        public override void Open()
        {
            if (Transport != null && !Transport.IsOpen)
            {
                Transport.OpenAsync().GetAwaiter().GetResult();
            }
        }

        public override void Close()
        {
            if (Transport != null && Transport.IsOpen)
            {
                Transport.Close();
            }
        }

        public override void Dispose()
        {
            if (Transport != null)
            {
                Close();
                Transport.Dispose();
            }
            if (Thriftclient != null)
            {
                Thriftclient.Dispose();
            }
        }
        

        
    }
}
