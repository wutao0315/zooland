using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    public interface ISpanBuilder
    {

        /**
         * A shorthand for addReference(References.CHILD_OF, parent).
         *
         * <p>
         * If parent==null, this is a noop.
         */
        ISpanBuilder asChildOf(ISpanContext parent);

        /**
         * A shorthand for addReference(References.CHILD_OF, parent.context()).
         *
         * <p>
         * If parent==null, this is a noop.
         */
        ISpanBuilder asChildOf(ISpan parent);

        /**
         * Add a reference from the Span being built to a distinct (usually parent) Span. May be called multiple times
         * to represent multiple such References.
         *
         * <p>
         * If
         * <ul>
         * <li>the {@link Tracer}'s {@link ScopeManager#active()} is not null, and
         * <li>no <b>explicit</b> references are added via {@link SpanBuilder#addReference}, and
         * <li>{@link SpanBuilder#ignoreActiveSpan()} is not invoked,
         * </ul>
         * ... then an inferred {@link References#CHILD_OF} reference is created to the
         * {@link ScopeManager#active()} {@link SpanContext} when either {@link SpanBuilder#startActive(boolean)} or
         * {@link SpanBuilder#start} is invoked.
         *
         * @param referenceType the reference type, typically one of the constants defined in References
         * @param referencedContext the SpanContext being referenced; e.g., for a References.CHILD_OF referenceType, the
         *                          referencedContext is the parent. If referencedContext==null, the call to
         *                          {@link #addReference} is a noop.
         *
         * @see io.opentracing.References
         */
        ISpanBuilder addReference(string referenceType, ISpanContext referencedContext);

        /**
         * Do not create an implicit {@link References#CHILD_OF} reference to the {@link ScopeManager#active()}).
         */
        ISpanBuilder ignoreActiveSpan();

        /** Same as {@link Span#setTag(String, String)}, but for the span being built. */
        ISpanBuilder withTag(string key, string value);

        /** Same as {@link Span#setTag(String, boolean)}, but for the span being built. */
        ISpanBuilder withTag(string key, bool value);

        /** Same as {@link Span#setTag(String, Number)}, but for the span being built. */
        ISpanBuilder withTag(string key, int value);

        /** Specify a timestamp of when the Span was started, represented in microseconds since epoch. */
        ISpanBuilder withStartTimestamp(long microseconds);

        /**
         * Returns a newly started and activated {@link Scope}.
         *
         * <p>
         * The returned {@link Scope} supports try-with-resources. For example:
         * <pre><code>
         *     try (Scope scope = tracer.buildSpan("...").startActive(true)) {
         *         // (Do work)
         *         scope.span().setTag( ... );  // etc, etc
         *     }
         *     // Span does finishes automatically only when 'finishSpanOnClose' is true
         * </code></pre>
         *
         * <p>
         * If
         * <ul>
         * <li>the {@link Tracer}'s {@link ScopeManager#active()} is not null, and
         * <li>no <b>explicit</b> references are added via {@link SpanBuilder#addReference}, and
         * <li>{@link SpanBuilder#ignoreActiveSpan()} is not invoked,
         * </ul>
         * ... then an inferred {@link References#CHILD_OF} reference is created to the
         * {@link ScopeManager#active()}'s {@link SpanContext} when either
         * {@link SpanBuilder#start()} or {@link SpanBuilder#startActive} is invoked.
         *
         * <p>
         * Note: {@link SpanBuilder#startActive(boolean)} is a shorthand for
         * {@code tracer.scopeManager().activate(spanBuilder.start(), finishSpanOnClose)}.
         *
         * @param finishSpanOnClose whether span should automatically be finished when {@link Scope#close()} is called
         * @return a {@link Scope}, already registered via the {@link ScopeManager}
         *
         * @see ScopeManager
         * @see Scope
         */
        IScope startActive(bool finishSpanOnClose);


        /**
         * Like {@link #startActive()}, but the returned {@link Span} has not been registered via the
         * {@link ScopeManager}.
         *
         * @see SpanBuilder#startActive(boolean)
         * @return the newly-started Span instance, which has *not* been automatically registered
         *         via the {@link ScopeManager}
         */
        ISpan start();
    }
}
