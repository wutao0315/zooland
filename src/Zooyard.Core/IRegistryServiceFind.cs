using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IRegistryServiceFind
    {
        IList<URL> Find();
        IList<URL> Find(string name);
        IList<URL> FindWithVersion(string name, string version);
        IList<URL> Find(Predicate<KeyValuePair<string, string[]>> nameTagsPredicate, Predicate<URL> registryInformationPredicate);
        IList<URL> Find(Predicate<KeyValuePair<string, string[]>> predicate);
        IList<URL> Find(Predicate<URL> predicate);
        IList<URL> FindAll();
    }
}
