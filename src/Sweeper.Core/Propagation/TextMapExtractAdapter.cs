using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Propagation
{
    /**
  * A TextMap carrier for use with Tracer.extract() ONLY (it has no mutating methods).
  *
  * Note that the TextMap interface can be made to wrap around arbitrary data types (not just Map&lt;String, String&gt;
  * as illustrated here).
  *
  * @see Tracer#extract(Format, Object)
  */
    public sealed class TextMapExtractAdapter : ITextMap
    {
        private readonly IDictionary<string, string> map;

        public TextMapExtractAdapter(IDictionary<string, string> map)
        {
            this.map = map;
        }

        public IEnumerable<KeyValuePair<string, string>> iterator()
        {
            return map.ToList();
        }

        public void put(string key, string value)
        {
            map.Add(key,value);
        }



       


        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.GetEnumerator();
        }
    }
}
