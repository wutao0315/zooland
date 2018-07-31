using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IRegistryKeyValues
    {
         void KeyValuePut(string key, string value);
         string KeyValueGet(string key);
         void KeyValueDelete(string key);
         void KeyValueDeleteTree(string prefix);
         string[] KeyValuesGetKeys(string prefix);
    }
}
