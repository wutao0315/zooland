using Sweeper.Core;
using Sweeper.Core.Atomic;
using Sweeper.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Mock
{
    public sealed class MockSpan : ISpan
    {
        // A simple-as-possible (consecutive for repeatability) id generator.
        private static AtomicLong nextId = new AtomicLong(0);

        //private readonly MockTracer mockTracer;
        private MockContext context;
        //private readonly long parentId; // 0 if there's no parent.
        //private readonly long startMicros;
        //private bool finished;
        //private long finishMicros;
        //private readonly IDictionary<string, Object> tags;
        //private readonly List<LogEntry> logEntries = new List<LogEntry>();
        //private readonly List<Reference> references;

        private readonly List<Exception> errors = new List<Exception>();

        public bool Finished { get; set; }

        public MockTracer MockTracer { get; set; }

        public string OperationName { get; private set; }



        public ISpan setOperationName(string operationName)
        {
            finishedCheck("Setting operationName {%s} on already finished span", operationName);
            this.OperationName = operationName;
            return this;
        }

        /**
         * @return the spanId of the Span's first {@value References#CHILD_OF} reference, or the first reference of any type, or 0 if no reference exists.
         *
         * @see MockContext#spanId()
         * @see MockSpan#references()
         */
        public long ParentId { get; set; }
        public long StartMicros { get; set; }
        /**
         * @return the finish time of the Span; only valid after a call to finish().
         */
        public long FinishMicros { get; set; }
        //public long finishMicros()
        //{
        //    assert finishMicros > 0 : "must call finish() before finishMicros()";
        //    return finishMicros;
        //}

        /**
         * @return a copy of all tags set on this Span.
         */
        public IDictionary<string, object> Tags { get; set; }
        //public IDictionary<string, object> tags()
        //{
        //    return new Dictionary<string,object>(this.tags);
        //}
        /**
         * @return a copy of all log entries added to this Span.
         */
        public IList<LogEntry> LogEntries { get; set; }
        //public List<LogEntry> logEntries()
        //{
        //    return new List<LogEntry>(this.logEntries);
        //}

        /**
         * @return a copy of exceptions thrown by this class (e.g. adding a tag after span is finished).
         */
        public List<Exception> generatedErrors()
        {
            return new List<Exception>(errors);
        }

        public IList<Reference> References { get; set; }
        //public List<Reference> references()
        //{
        //    return new List<Reference>(references);
        //}

        public ISpanContext Context { get { return context; } }
        //public MockContext context()
        //    {
        //        return this.context;
        //    }


        public void finish()
        {
            this.finish(NowMicros());
        }


        public void finish(long finishMicros)
        {
            finishedCheck("Finishing already finished span");
            this.FinishMicros = finishMicros;
            this.MockTracer.appendFinishedSpan(this);
            this.Finished = true;
        }


        public ISpan setTag(string key, string value)
        {
            return setObjectTag(key, value);
        }


        public ISpan setTag(string key, bool value)
        {
            return setObjectTag(key, value);
        }


        public ISpan setTag(string key, int value)
        {
            return setObjectTag(key, value);
        }

        private ISpan setObjectTag(string key, object value)
        {
            finishedCheck("Adding tag {%s:%s} to already finished span", key, value);
            Tags.Add(key, value);
            return this;
        }


        public ISpan log(IDictionary<string, object> fields)
        {
            return log(NowMicros(), fields);
        }


        public ISpan log(long timestampMicros, IDictionary<string, object> fields)
        {
            finishedCheck("Adding logs %s at %d to already finished span", fields, timestampMicros);
            this.LogEntries.Add(new LogEntry(timestampMicros, fields));
            return this;
        }


        public ISpan log(string eventData)
        {
            return this.log(NowMicros(), eventData);
        }


        public ISpan log(long timestampMicroseconds, string eventData)
        {
            return this.log(timestampMicroseconds, new Dictionary<string, object> { { "event", eventData } });
        }


        public ISpan setBaggageItem(string key, string value)
        {
            finishedCheck("Adding baggage {%s:%s} to already finished span", key, value);
            this.context = this.context.withBaggageItem(key, value);
            return this;
        }


        public string getBaggageItem(string key)
        {
            return this.context.getBaggageItem(key);
        }

        /**
         * MockContext implements a Dapper-like opentracing.SpanContext with a trace- and span-id.
         *
         * Note that parent ids are part of the MockSpan, not the MockContext (since they do not need to propagate
         * between processes).
         */
        public sealed class MockContext : ISpanContext
        {
            //private readonly long traceId;
            //private readonly IDictionary<string, string> baggage;
            //private readonly long spanId;
            public long TraceId { get; private set; }
            public long SpanId { get; private set; }
            public IDictionary<string, string> Baggage { get; private set; }

            /**
             * A package-protected constructor to create a new MockContext. This should only be called by MockSpan and/or
             * MockTracer.
             *
             * @param baggage the MockContext takes ownership of the baggage parameter
             *
             * @see MockContext#withBaggageItem(string, string)
             */
            public MockContext(long traceId, long spanId, IDictionary<string, string> baggage)
            {
                this.Baggage = baggage;
                this.TraceId = traceId;
                this.SpanId = spanId;
            }

            public string getBaggageItem(string key) { return this.Baggage[key]; }


            /**
             * Create and return a new (immutable) MockContext with the added baggage item.
             */
            public MockContext withBaggageItem(string key, string val)
            {
                IDictionary<string, string> newBaggage = new Dictionary<string, string>(this.Baggage);
                newBaggage.Add(key, val);
                return new MockContext(this.TraceId, this.SpanId, newBaggage);
            }

            public IEnumerable<KeyValuePair<string, string>> baggageItems()
            {
                return Baggage.ToList();
            }
        }

        public sealed class LogEntry
        {
            public long TimestampMicros { get; private set; }
            public IDictionary<string, object> Fields { get; private set; }

            public LogEntry(long timestampMicros, IDictionary<string, object> fields)
            {
                this.TimestampMicros = timestampMicros;
                this.Fields = fields;
            }

        }

        public sealed class Reference
        {
            private readonly MockContext context;
            private readonly string referenceType;

            public Reference(MockContext context, string referenceType)
            {
                this.context = context;
                this.referenceType = referenceType;
            }

            public MockContext getContext()
            {
                return context;
            }

            public string getReferenceType()
            {
                return referenceType;
            }


            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o == null || this.GetType() != o.GetType()) return false;
                var reference = (Reference)o;
                return context == reference.context && referenceType == reference.referenceType;
            }


            public int hashCode()
            {
                return this.GetHashCode();
                //return Objects.hash(context, referenceType);
            }
        }

        internal MockSpan(MockTracer tracer, string operationName, long startMicros, IDictionary<string, object> initialTags, List<Reference> refs)
        {
            this.MockTracer = tracer;
            this.OperationName = operationName;
            this.StartMicros = startMicros;
            if (initialTags == null)
            {
                this.Tags = new Dictionary<string, object>();
            }
            else
            {
                this.Tags = new Dictionary<string, object>(initialTags);
            }
            if (refs == null)
            {
                this.References = new List<Reference>();
            }
            else
            {
                this.References = new List<Reference>(refs);
            }
            var parent = findPreferredParentRef(this.References);
            if (parent == null)
            {
                // We're a root Span.
                this.context = new MockContext(NextId(), NextId(), new Dictionary<string, string>());
                this.ParentId = 0;
            }
            else
            {
                // We're a child Span.
                this.context = new MockContext(parent.TraceId, NextId(), mergeBaggages(this.References));
                this.ParentId = parent.SpanId;
            }
        }

        private static MockContext findPreferredParentRef(IList<Reference> references)
        {
            if (references.Count == 0)
            {
                return null;
            }
            foreach (var reference in references)
            {
                if (Sweeper.Core.References.CHILD_OF.Equals(reference.getReferenceType()))
                {
                    return reference.getContext();
                }
            }
            return references[0].getContext();
        }

        private static IDictionary<string, string> mergeBaggages(IList<Reference> references)
        {
            var baggage = new Dictionary<string, string>();
            foreach (var refItem in references)
            {
                if (refItem.getContext().Baggage == null)
                {
                    continue;
                }
                foreach (var item in refItem.getContext().Baggage)
                {
                    baggage.Add(item.Key, item.Value);
                }
            }
            return baggage;
        }

        public long NextId()
        {
            return nextId.AddAndGet(1);
        }

        public static long NowMicros()
        {
            return DateTime.Now.CurrentTimeMillis() * 1000;
        }

        private void finishedCheck(string format, params object[] args)
        {
            if (Finished)
            {
                var ex = new Exception(string.Format(format, args));
                errors.Add(ex);
                throw ex;
            }
        }


        public override string ToString()
        {
            return $@"{{  traceId:{context.TraceId}, spanId:{context.SpanId}, parentId:{ParentId}, operationName:{OperationName} }}";
        }

    }
}
