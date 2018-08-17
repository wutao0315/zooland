using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    public interface ISpan
    {
        /**
    * Retrieve the associated SpanContext.
    *
    * This may be called at any time, including after calls to finish().
    *
    * @return the SpanContext that encapsulates Span state that should propagate across process boundaries.
    */
        ISpanContext context();

        /**
         * Set a key:value tag on the Span.
         */
        ISpan setTag(string key, string value);

        /** Same as {@link #setTag(String, String)}, but for boolean values. */
        ISpan setTag(string key, bool value);

        /** Same as {@link #setTag(String, String)}, but for numeric values. */
        ISpan setTag(string key, int value);

        /**
         * Log key:value pairs to the Span with the current walltime timestamp.
         *
         * <p><strong>CAUTIONARY NOTE:</strong> not all Tracer implementations support key:value log fields end-to-end.
         * Caveat emptor.
         *
         * <p>A contrived example (using Guava, which is not required):
         * <pre><code>
         span.log(
         ImmutableMap.Builder<String, Object>()
         .put("event", "soft error")
         .put("type", "cache timeout")
         .put("waited.millis", 1500)
         .build());
         </code></pre>
         *
         * @param fields key:value log fields. Tracer implementations should support String, numeric, and boolean values;
         *               some may also support arbitrary Objects.
         * @return the Span, for chaining
         * @see Span#log(String)
         */
        ISpan log(IDictionary<string, object> fields);

        /**
         * Like log(Map&lt;String, Object&gt;), but with an explicit timestamp.
         *
         * <p><strong>CAUTIONARY NOTE:</strong> not all Tracer implementations support key:value log fields end-to-end.
         * Caveat emptor.
         *
         * @param timestampMicroseconds The explicit timestamp for the log record. Must be greater than or equal to the
         *                              Span's start timestamp.
         * @param fields key:value log fields. Tracer implementations should support String, numeric, and boolean values;
         *               some may also support arbitrary Objects.
         * @return the Span, for chaining
         * @see Span#log(long, String)
         */
        ISpan log(long timestampMicroseconds, IDictionary<string, object> fields);

        /**
         * Record an event at the current walltime timestamp.
         *
         * Shorthand for
         *
         * <pre><code>
         span.log(Collections.singletonMap("event", event));
         </code></pre>
         *
         * @param event the event value; often a stable identifier for a moment in the Span lifecycle
         * @return the Span, for chaining
         */
        ISpan log(string eventData);

        /**
         * Record an event at a specific timestamp.
         *
         * Shorthand for
         *
         * <pre><code>
         span.log(timestampMicroseconds, Collections.singletonMap("event", event));
         </code></pre>
         *
         * @param timestampMicroseconds The explicit timestamp for the log record. Must be greater than or equal to the
         *                              Span's start timestamp.
         * @param event the event value; often a stable identifier for a moment in the Span lifecycle
         * @return the Span, for chaining
         */
        ISpan log(long timestampMicroseconds, string eventData);

        /**
         * Sets a baggage item in the Span (and its SpanContext) as a key/value pair.
         *
         * Baggage enables powerful distributed context propagation functionality where arbitrary application data can be
         * carried along the full path of request execution throughout the system.
         *
         * Note 1: Baggage is only propagated to the future (recursive) children of this SpanContext.
         *
         * Note 2: Baggage is sent in-band with every subsequent local and remote calls, so this feature must be used with
         * care.
         *
         * @return this Span instance, for chaining
         */
        ISpan setBaggageItem(string key, string value);

    /**
     * @return the value of the baggage item identified by the given key, or null if no such item could be found
     */
    string getBaggageItem(string key);

        /**
         * Sets the string name for the logical operation this span represents.
         *
         * @return this Span instance, for chaining
         */
        ISpan setOperationName(string operationName);

        /**
         * Sets the end timestamp to now and records the span.
         *
         * <p>With the exception of calls to {@link #context}, this should be the last call made to the span instance.
         * Future calls to {@link #finish} are defined as noops, and future calls to methods other than {@link #context}
         * lead to undefined behavior.
         *
         * @see Span#context()
         */
        void finish();

        /**
         * Sets an explicit end timestamp and records the span.
         *
         * <p>With the exception of calls to Span.context(), this should be the last call made to the span instance, and to
         * do otherwise leads to undefined behavior.
         *
         * @param finishMicros an explicit finish time, in microseconds since the epoch
         *
         * @see Span#context()
         */
        void finish(long finishMicros);
    }
}
