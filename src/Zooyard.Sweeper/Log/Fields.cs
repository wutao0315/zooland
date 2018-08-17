using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Log
{
    public class Fields
    {
        private Fields()
        {
        }

        /**
         * The type or "kind" of an error (only for event="error" logs). E.g., "Exception", "OSError"
         */
        public const string ERROR_KIND = "error.kind";

        /**
         * The actual Throwable/Exception/Error object instance itself. E.g., A java.lang.UnsupportedOperationException instance
         */
        public const string ERROR_OBJECT = "error.object";

        /**
         * A stable identifier for some notable moment in the lifetime of a Span. For instance, a mutex
         * lock acquisition or release or the sorts of lifetime events in a browser page load described
         * in the Performance.timing specification. E.g., from Zipkin, "cs", "sr", "ss", or "cr". Or,
         * more generally, "initialized" or "timed out". For errors, "error"
         */
        public const string EVENT = "event";

        /**
         * A concise, human-readable, one-line message explaining the event. E.g., "Could not connect
         * to backend", "Cache invalidation succeeded"
         */
        public const string MESSAGE = "message";

        /**
         * A stack trace in platform-conventional format; may or may not pertain to an error. 
         */
        public const string STACK = "stack";
    }
}
