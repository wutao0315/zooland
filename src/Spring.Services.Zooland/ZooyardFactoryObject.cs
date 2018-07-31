using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Spring.Objects.Factory;
using Zooyard.Core;

namespace Spring.Services.Zooyard
{
    public class ZooyardFactoryObject<T> : ZooyardFactory<T>, IFactoryObject where T:class
    {
        #region Logging

        private static readonly ILog Log = LogManager.GetLogger(typeof(ZooyardFactoryObject<>));

        #endregion
        
        public Type ObjectType => typeof(T);

        public bool IsSingleton => true;

        public object GetObject()
        {
            #region Instrumentation

            if (Log.IsDebugEnabled)
            {
                Log.Debug($"Creating service of type '{typeof(T).FullName}' for the specified endpoint '{1}'...");
            }

            #endregion

            return this.CreateYard();//this.CreateChannel();
        }
    }
}
