using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Utils;

namespace Zooyard.Rpc.RemotingImpl
{
    public class RemotingService : IDisposable
    {
        public MarshalByRefObject TheRemoteObject { get; set; }

        /// <summary>
        /// Gets or sets the name of the remote application.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the name of the exported remote service.
        /// <remarks>
        /// The name that will be used in the URI to refer to this service.
        /// This will be of the form, tcp://host:port/ServiceName or
        /// tcp://host:port/ApplicationName/ServiceName
        /// </remarks>
        /// </summary>
        public string ServiceName { get; set; }



        public IChannelReceiver Channel { get; set; }
        public bool EnsureSecurity { get; set; } = false;

        private ObjRef objRef { get; set; }
        //private MarshalByRefObject remoteObject;

        //public string RegisterName { get; set; } = "RegisterWellKnownServiceType";
        //public IList<object> RegisterParams { get; set; } = new List<object>();

        public void Open()
        {

            if (!ChannelServices.RegisteredChannels.Contains(Channel))
            {
                ChannelServices.RegisterChannel(Channel, EnsureSecurity);
            }

            var objectUri = (!(string.IsNullOrEmpty(ApplicationName)) ? $"{ApplicationName}/{ServiceName}" : ServiceName);

            objRef = RemotingServices.Marshal(TheRemoteObject, objectUri);
            
            #region Instrumentation

            //if (LOG.IsDebugEnabled)
            //{
            //    LOG.Debug(String.Format("Target '{0}' exported as '{1}'.", targetName, objectUri));
            //}

            #endregion
        }

        /// <summary>
        /// Stops exporting the object identified by <see cref="TargetName"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (objRef!=null)
                {
                    RemotingServices.Unmarshal(objRef);
                    objRef = null;
                }

                if (TheRemoteObject != null)
                {
                    RemotingServices.Disconnect(TheRemoteObject);
                    TheRemoteObject = null;
                }

                if (ChannelServices.RegisteredChannels.Contains(Channel))
                {
                    Channel.StopListening(null);
                    ChannelServices.UnregisterChannel(Channel);
                }
              
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        /// <summary>
        /// Cleanup before GC
        /// </summary>
        ~RemotingService()
        {
            Dispose(false);
        }
    }
    
}
