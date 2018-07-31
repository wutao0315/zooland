using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IRegistryHealthChecks
    {
        /// <summary>
        /// registry HealthCheck
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string RegisterHealthCheck(URL url);
        /// <summary>
        /// unregistry HealthCheck
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        bool DeregisterHealthCheck(URL url);
    }
}
