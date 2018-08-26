using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Propagation
{
    public interface IFormat<C>
    {
       
    }
    public sealed class Builtin<C>: IFormat<C>
    {

        private readonly string name;

        private Builtin(string name)
        {
            this.name = name;
        }

        /**
         * The TEXT_MAP format allows for arbitrary String-&gt;String map encoding of SpanContext state for
         * Tracer.inject and Tracer.extract.
         *
         * Unlike HTTP_HEADERS, the builtin TEXT_MAP format expresses no constraints on keys or values.
         *
         * @see io.opentracing.Tracer#inject(SpanContext, Format, Object)
         * @see io.opentracing.Tracer#extract(Format, Object)
         * @see Format
         * @see Builtin#HTTP_HEADERS
         */
        public readonly static IFormat<ITextMap> TEXT_MAP = new Builtin<ITextMap>("TEXT_MAP");

        /**
         * The HTTP_HEADERS format allows for HTTP-header-compatible String-&gt;String map encoding of SpanContext state
         * for Tracer.inject and Tracer.extract.
         *
         * I.e., keys written to the TextMap MUST be suitable for HTTP header keys (which are poorly defined but
         * certainly restricted); and similarly for values (i.e., URL-escaped and "not too long").
         *
         * @see io.opentracing.Tracer#inject(SpanContext, Format, Object)
         * @see io.opentracing.Tracer#extract(Format, Object)
         * @see Format
         * @see Builtin#TEXT_MAP
         */
        public readonly static IFormat<ITextMap> HTTP_HEADERS = new Builtin<ITextMap>("HTTP_HEADERS");

        /**
         * The BINARY format allows for unconstrained binary encoding of SpanContext state for Tracer.inject and
         * Tracer.extract.
         *
         * @see io.opentracing.Tracer#inject(SpanContext, Format, Object)
         * @see io.opentracing.Tracer#extract(Format, Object)
         * @see Format
         */
        public readonly static IFormat<byte[]> BINARY = new Builtin<byte[]>("BINARY");

        /**
        * @return Short name for built-in formats as they tend to show up in exception messages.
        */
        public override string ToString()
        {
            return typeof(Builtin<>).Name + "." + name;
            // return base.ToString();
        }
    }
}
