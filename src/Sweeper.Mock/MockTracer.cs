using Sweeper.Core;
using Sweeper.Core.Propagation;
using Sweeper.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Mock
{
    public class MockTracer : ITracer
    {
        private readonly List<MockSpan> finishedSpans = new List<MockSpan>();
        private readonly IPropagator propagator;
        private readonly IScopeManager scopeManager;

        public MockTracer()
        {
            this(new ThreadLocalScopeManager(), IPropagator.TEXT_MAP);
        }

        public MockTracer(IScopeManager scopeManager)
        {
            this(scopeManager, IPropagator.TEXT_MAP);
        }

        public MockTracer(IScopeManager scopeManager, IPropagator propagator)
        {
            this.scopeManager = scopeManager;
            this.propagator = propagator;
        }

        /**
         * Create a new MockTracer that passes through any calls to inject() and/or extract().
         */
        public MockTracer(IPropagator propagator)
        {
            this(new ThreadLocalScopeManager(), propagator);
        }

        /**
         * Clear the finishedSpans() queue.
         *
         * Note that this does *not* have any effect on Spans created by MockTracer that have not finish()ed yet; those
         * will still be enqueued in finishedSpans() when they finish().
         */
        public void reset()
        {
            this.finishedSpans.clear();
        }

        /**
         * @return a copy of all finish()ed MockSpans started by this MockTracer (since construction or the last call to
         * MockTracer.reset()).
         *
         * @see MockTracer#reset()
         */
        public List<MockSpan> finishedSpans()
        {
            return new List<MockSpan>(this.finishedSpans);
        }

        /**
         * Noop method called on {@link Span#finish()}.
         */
        protected void onSpanFinished(MockSpan mockSpan)
        {
        }

        /**
         * Propagator allows the developer to intercept and verify any calls to inject() and/or extract().
         *
         * By default, MockTracer uses Propagator.PRINTER which simply logs such calls to System.out.
         *
         * @see MockTracer#MockTracer(Propagator)
         */
        public interface IPropagator
        {
            void inject<C>(MockSpan.MockContext ctx, IFormat<C> format, C carrier);
            MockSpan.MockContext extract<C>(IFormat<C> format, C carrier);

            IPropagator PRINTER = new IPropagator()
            {

            public void inject<C>(MockSpan.MockContext ctx, IFormat<C> format, C carrier)
            {
                Console.WriteLine($"inject({ctx}, {format}, {carrier})");
            }


            public MockSpan.MockContext extract<C>(IFormat<C> format, C carrier)
            {
                Console.WriteLine($"extract({format}, {carrier})");
                return null;
            }
        };

        IPropagator TEXT_MAP = new IPropagator()
        {
            public static readonly string SPAN_ID_KEY = "spanid";
        public static readonly string TRACE_ID_KEY = "traceid";
        public static readonly string BAGGAGE_KEY_PREFIX = "baggage-";

        public void inject<C>(MockSpan.MockContext ctx, IFormat<C> format, C carrier)
        {
            if (carrier is ITextMap)
            {
                ITextMap textMap = (ITextMap)carrier;
                foreach (var entry in ctx.baggageItems())
                {
                    textMap.put(BAGGAGE_KEY_PREFIX + entry.Key, entry.Value);
                }
                textMap.put(SPAN_ID_KEY, ctx.SpanId.ToString());
                textMap.put(TRACE_ID_KEY, ctx.TraceId.ToString());
            }
            else
            {
                throw new Exception("Unknown carrier");
            }
        }

        public MockSpan.MockContext extract<C>(IFormat<C> format, C carrier)
        {
            long? traceId = null;
            long? spanId = null;
            IDictionary<string, string> baggage = new Dictionary<string, string>();

            if (carrier is ITextMap)
            {
                ITextMap textMap = (ITextMap)carrier;
                foreach (var entry in textMap)
                {
                    if (TRACE_ID_KEY.Equals(entry.Key))
                    {
                        traceId = Long.valueOf(entry.getValue());
                    }
                    else if (SPAN_ID_KEY.Equals(entry.Key))
                    {
                        spanId = Long.valueOf(entry.getValue());
                    }
                    else if (entry.Key.StartsWith(BAGGAGE_KEY_PREFIX))
                    {
                        string key = entry.Key.Substring((BAGGAGE_KEY_PREFIX.Length));
                        baggage.Add(key, entry.Value);
                    }
                }
            }
            else
            {
                throw new Exception("Unknown carrier");
            }

            if (traceId != null && spanId != null)
            {
                return new MockSpan.MockContext(traceId.Value, spanId.Value, baggage);
            }

            return null;
        }
    };
}

public IScopeManager scopeManager()
{
    return this.scopeManager;
}

public SpanBuilder buildSpan(string operationName)
{
    return new SpanBuilder(operationName);
}

public void inject<C>(ISpanContext spanContext, IFormat<C> format, C carrier)
{
    this.propagator.inject((MockSpan.MockContext)spanContext, format, carrier);
}

public ISpanContext extract<C>(IFormat<C> format, C carrier)
{
    return this.propagator.extract(format, carrier);
}

public ISpan activeSpan()
{
    IScope scope = this.scopeManager.active();
    return scope == null ? null : scope.span();
}

public void appendFinishedSpan(MockSpan mockSpan)
{
    this.finishedSpans.add(mockSpan);
    this.onSpanFinished(mockSpan);
}

private ISpanContext activeSpanContext()
{
    ISpan span = activeSpan();
    if (span == null)
    {
        return null;
    }

    return span.context();
}

public sealed class SpanBuilder : ISpanBuilder
{
    private readonly string operationName;
    private long startMicros;
    private List<MockSpan.Reference> references = new List<MockSpan.Reference>();
    private bool ignoringActiveSpan;
    private IDictionary<string, object> initialTags = new Dictionary<string, object>();

    SpanBuilder(string operationName)
    {
        this.operationName = operationName;
    }

    public SpanBuilder asChildOf(ISpanContext parent)
    {
        return addReference(References.CHILD_OF, parent);
    }

    public SpanBuilder asChildOf(ISpan parent)
    {
        if (parent == null)
        {
            return this;
        }
        return addReference(References.CHILD_OF, parent.context());
    }

    public SpanBuilder ignoreActiveSpan()
    {
        ignoringActiveSpan = true;
        return this;
    }
    public SpanBuilder addReference(string referenceType, ISpanContext referencedContext)
    {
        if (referencedContext != null)
        {
            this.references.Add(new MockSpan.Reference((MockSpan.MockContext)referencedContext, referenceType));
        }
        return this;
    }

    public SpanBuilder withTag(string key, string value)
    {
        this.initialTags.Add(key, value);
        return this;
    }

    public SpanBuilder withTag(string key, bool value)
    {
        this.initialTags.Add(key, value);
        return this;
    }

    public SpanBuilder withTag(string key, int value)
    {
        this.initialTags.Add(key, value);
        return this;
    }

    public SpanBuilder withStartTimestamp(long microseconds)
    {
        this.startMicros = microseconds;
        return this;
    }

    public IScope startActive(bool finishOnClose)
    {
        return MockTracer.this.scopeManager().activate(this.startManual(), finishOnClose);
    }

    public MockSpan start()
    {
        return startManual();
    }

    public MockSpan startManual()
    {
        if (this.startMicros == 0)
        {
            this.startMicros = MockSpan.NowMicros();
        }
        ISpanContext activeSpanContext = activeSpanContext();
        if (references.Count == 0 && !ignoringActiveSpan && activeSpanContext != null)
        {
            references.Add(new MockSpan.Reference((MockSpan.MockContext)activeSpanContext, References.CHILD_OF));
        }
        return new MockSpan(MockTracer.this, operationName, startMicros, initialTags, references);
    }
}



