using Sweeper.Core;
using Sweeper.Core.Propagation;
using Sweeper.Core.Support;
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
        
        private readonly IPropagator propagator;

        public MockTracer() : this(new ThreadLocalScopeManager(), new TextMapPropagator())
        {
        }

        public MockTracer(IScopeManager scopeManager) : this(scopeManager, new TextMapPropagator())
        {
        }

        public MockTracer(IScopeManager scopeManager, IPropagator propagator)
        {
            this.ScopeManager = scopeManager;
            this.propagator = propagator;
        }

        /**
         * Create a new MockTracer that passes through any calls to inject() and/or extract().
         */
        public MockTracer(IPropagator propagator) : this(new ThreadLocalScopeManager(), propagator)
        {

        }


        public List<MockSpan> FinishedSpans { get;private set; } = new List<MockSpan>();
        public IScopeManager ScopeManager { get; private set; }
        /**
         * Clear the finishedSpans() queue.
         *
         * Note that this does *not* have any effect on Spans created by MockTracer that have not finish()ed yet; those
         * will still be enqueued in finishedSpans() when they finish().
         */
        public void reset()
        {
            this.FinishedSpans.Clear();
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
        }
        public sealed class PrinterPropagator:IPropagator
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
        }
        public sealed class TextMapPropagator : IPropagator
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
                int? spanId = null;
                IDictionary<string, string> baggage = new Dictionary<string, string>();

                if (!(carrier is ITextMap))
                {
                    throw new Exception("Unknown carrier");
                }

                var textMap = (ITextMap)carrier;
                foreach (var entry in textMap)
                {
                    if (TRACE_ID_KEY.Equals(entry.Key))
                    {
                        traceId = Convert.ToInt64(entry.Value);
                    }
                    else if (SPAN_ID_KEY.Equals(entry.Key))
                    {
                        spanId = Convert.ToInt32(entry.Value);
                    }
                    else if (entry.Key.StartsWith(BAGGAGE_KEY_PREFIX))
                    {
                        string key = entry.Key.Substring((BAGGAGE_KEY_PREFIX.Length));
                        baggage.Add(key, entry.Value);
                    }
                }

                if (traceId != null && spanId != null)
                {
                    return new MockSpan.MockContext(traceId.Value, spanId.Value, baggage);
                }

                return null;
            }
        }



        public ISpanBuilder buildSpan(string operationName)
        {
            return new SpanBuilder(operationName,this);
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
            IScope scope = this.ScopeManager.active();
            return scope == null ? null : scope.span();
        }

        public void appendFinishedSpan(MockSpan mockSpan)
        {
            this.FinishedSpans.Add(mockSpan);
            this.onSpanFinished(mockSpan);
        }

        public ISpanContext activeSpanContext()
        {
            ISpan span = activeSpan();
            if (span == null)
            {
                return null;
            }

            return span.Context;
        }

        public sealed class SpanBuilder : ISpanBuilder
        {
            private readonly string operationName;
            private long startMicros;
            private List<MockSpan.Reference> references = new List<MockSpan.Reference>();
            private bool ignoringActiveSpan;
            private IDictionary<string, object> initialTags = new Dictionary<string, object>();
            private readonly MockTracer tracer;

            public SpanBuilder(string operationName, MockTracer tracer)
            {
                this.operationName = operationName;
                this.tracer = tracer;
            }

            public ISpanBuilder asChildOf(ISpanContext parent)
            {
                return addReference(References.CHILD_OF, parent);
            }

            public ISpanBuilder asChildOf(ISpan parent)
            {
                if (parent == null)
                {
                    return this;
                }
                return addReference(References.CHILD_OF, parent.Context);
            }

            public ISpanBuilder ignoreActiveSpan()
            {
                ignoringActiveSpan = true;
                return this;
            }
            public ISpanBuilder addReference(string referenceType, ISpanContext referencedContext)
            {
                if (referencedContext != null)
                {
                    this.references.Add(new MockSpan.Reference((MockSpan.MockContext)referencedContext, referenceType));
                }
                return this;
            }

            public ISpanBuilder withTag(string key, string value)
            {
                this.initialTags.Add(key, value);
                return this;
            }

            public ISpanBuilder withTag(string key, bool value)
            {
                this.initialTags.Add(key, value);
                return this;
            }

            public ISpanBuilder withTag(string key, int value)
            {
                this.initialTags.Add(key, value);
                return this;
            }

            public ISpanBuilder withStartTimestamp(long microseconds)
            {
                this.startMicros = microseconds;
                return this;
            }

            public IScope startActive(bool finishOnClose)
            {
                return tracer.ScopeManager.activate(this.startManual(), finishOnClose);
            }

            public ISpan start()
            {
                return startManual();
            }

            public ISpan startManual()
            {
                if (this.startMicros == 0)
                {
                    this.startMicros = MockSpan.NowMicros();
                }
                ISpanContext activeSpanContext = tracer.activeSpanContext();
                if (references.Count == 0 && !ignoringActiveSpan && activeSpanContext != null)
                {
                    references.Add(new MockSpan.Reference((MockSpan.MockContext)activeSpanContext, References.CHILD_OF));
                }
                return new MockSpan(tracer, operationName, startMicros, initialTags, references);
            }
        }
    }
}



