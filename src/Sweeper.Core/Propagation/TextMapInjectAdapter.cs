using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Propagation
{
    /**
 * A TextMap carrier for use with Tracer.inject() ONLY (it has no read methods).
 *
 * Note that the TextMap interface can be made to wrap around arbitrary data types (not just Map&lt;String, String&gt;
 * as illustrated here).
 *
 * @see Tracer#inject(SpanContext, Format, Object)
 */
    public sealed class TextMapInjectAdapter : ITextMap
    {
    private readonly IDictionary<string, string> map;

    public TextMapInjectAdapter(IDictionary<string, string> map)
    {
        this.map = map;
    }

        public IEnumerator<KeyValuePair<string, string>> iterator()
        {
            throw new Exception("TextMapInjectAdapter should only be used with Tracer.inject()");
        }
        public void put(string key, string value)
        {
            this.map.Add(key, value);
        }


        public KeyValuePair<string, string> Current => throw new NotImplementedException();

        object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

     

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

     

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
