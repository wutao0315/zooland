using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IRegistryHost: IRegistryService, IRegistryServiceFind, IRegistryHealthChecks, IRegistryKeyValues
    {

    }
}
