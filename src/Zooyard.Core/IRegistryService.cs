using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IRegistryService
    {
        /// <summary>
        /// registry
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        URL RegisterService(URL url);
        /// <summary>
        /// unregistry
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        bool DeregisterService(URL url);
    }
}
