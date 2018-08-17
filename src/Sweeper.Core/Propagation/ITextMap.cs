using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Propagation
{
    /**
 * TextMap is a built-in carrier for Tracer.inject() and Tracer.extract(). TextMap implementations allows Tracers to
 * read and write key:value String pairs from arbitrary underlying sources of data.
 *
 * @see io.opentracing.Tracer#inject(SpanContext, Format, Object)
 * @see io.opentracing.Tracer#extract(Format, Object)
 */
    public interface ITextMap : IEnumerator<KeyValuePair<string, string>>
    {
        /**
         * Gets an iterator over arbitrary key:value pairs from the TextMapReader.
         *
         * @return entries in the TextMap backing store; note that for some Formats, the iterator may include entries that
         * were never injected by a Tracer implementation (e.g., unrelated HTTP headers)
         *
         * @see io.opentracing.Tracer#extract(Format, Object)
         * @see Format.Builtin#TEXT_MAP
         * @see Format.Builtin#HTTP_HEADERS
         */
        IEnumerator<KeyValuePair<string, string>> iterator();

        /**
         * Puts a key:value pair into the TextMapWriter's backing store.
         *
         * @param key a String, possibly with constraints dictated by the particular Format this TextMap is paired with
         * @param value a String, possibly with constraints dictated by the particular Format this TextMap is paired with
         *
         * @see io.opentracing.Tracer#inject(io.opentracing.SpanContext, Format, Object)
         * @see Format.Builtin#TEXT_MAP
         * @see Format.Builtin#HTTP_HEADERS
         */
        void put(string key, string value);
    }
}
