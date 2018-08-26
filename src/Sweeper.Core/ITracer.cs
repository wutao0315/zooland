using Sweeper.Core.Propagation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    /**
  * Tracer is a simple, thin interface for Span creation and propagation across arbitrary transports.
  */
    public interface ITracer
    {

        /**
         * @return the current {@link ScopeManager}, which may be a noop but may not be null.
         */
        IScopeManager ScopeManager { get; }

        /**
         * @return the active {@link Span}. This is a shorthand for Tracer.scopeManager().active().span(),
         * and null will be returned if {@link Scope#active()} is null.
         */
        ISpan activeSpan();

        /**
         * Return a new SpanBuilder for a Span with the given `operationName`.
         *
         * <p>You can override the operationName later via {@link Span#setOperationName(String)}.
         *
         * <p>A contrived example:
         * <pre><code>
         *   Tracer tracer = ...
         *
         *   // Note: if there is a `tracer.active()` Scope, its `span()` will be used as the target
         *   // of an implicit CHILD_OF Reference for "workScope.span()" when `startActive()` is invoked.
         *   try (Scope workScope = tracer.buildSpan("DoWork").startActive()) {
         *       workScope.span().setTag("...", "...");
         *       // etc, etc
         *   }
         *
         *   // It's also possible to create Spans manually, bypassing the ScopeManager activation.
         *   Span http = tracer.buildSpan("HandleHTTPRequest")
         *                     .asChildOf(rpcSpanContext)  // an explicit parent
         *                     .withTag("user_agent", req.UserAgent)
         *                     .withTag("lucky_number", 42)
         *                     .start();
         * </code></pre>
         */
        ISpanBuilder buildSpan(string operationName);

    /**
     * Inject a SpanContext into a `carrier` of a given type, presumably for propagation across process boundaries.
     *
     * <p>Example:
     * <pre><code>
     * Tracer tracer = ...
     * Span clientSpan = ...
     * TextMap httpHeadersCarrier = new AnHttpHeaderCarrier(httpRequest);
     * tracer.inject(span.context(), Format.Builtin.HTTP_HEADERS, httpHeadersCarrier);
     * </code></pre>
     *
     * @param <C> the carrier type, which also parametrizes the Format.
     * @param spanContext the SpanContext instance to inject into the carrier
     * @param format the Format of the carrier
     * @param carrier the carrier for the SpanContext state. All Tracer.inject() implementations must support
     *                io.opentracing.propagation.TextMap and java.nio.ByteBuffer.
     *
     * @see io.opentracing.propagation.Format
     * @see io.opentracing.propagation.Format.Builtin
     */
     void inject<C>(ISpanContext spanContext, IFormat<C> format, C carrier);

    /**
     * Extract a SpanContext from a `carrier` of a given type, presumably after propagation across a process boundary.
     *
     * <p>Example:
     * <pre><code>
     * Tracer tracer = ...
     * TextMap httpHeadersCarrier = new AnHttpHeaderCarrier(httpRequest);
     * SpanContext spanCtx = tracer.extract(Format.Builtin.HTTP_HEADERS, httpHeadersCarrier);
     * ... = tracer.buildSpan('...').asChildOf(spanCtx).startActive();
     * </code></pre>
     *
     * If the span serialized state is invalid (corrupt, wrong version, etc) inside the carrier this will result in an
     * IllegalArgumentException. If the span serialized state is missing the method returns null.
     *
     * @param <C> the carrier type, which also parametrizes the Format.
     * @param format the Format of the carrier
     * @param carrier the carrier for the SpanContext state. All Tracer.extract() implementations must support
     *                io.opentracing.propagation.TextMap and java.nio.ByteBuffer.
     *
     * @return the SpanContext instance holding context to create a Span, null otherwise.
     *
     * @see io.opentracing.propagation.Format
     * @see io.opentracing.propagation.Format.Builtin
     */
    ISpanContext extract<C>(IFormat<C> format, C carrier);

    }
    
}
