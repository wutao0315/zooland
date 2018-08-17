using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    /**
 * The {@link ScopeManager} interface abstracts both the activation of {@link Span} instances via
 * {@link ScopeManager#activate(Span, boolean)} and access to an active {@link Span}/{@link Scope}
 * via {@link ScopeManager#active()}.
 *
 * @see Scope
 * @see Tracer#scopeManager()
 */
    public interface IScopeManager
    {

        /**
         * Make a {@link Span} instance active.
         *
         * @param span the {@link Span} that should become the {@link #active()}
         * @param finishSpanOnClose whether span should automatically be finished when {@link Scope#close()} is called
         * @return a {@link Scope} instance to control the end of the active period for the {@link Span}. It is a
         * programming error to neglect to call {@link Scope#close()} on the returned instance.
         */
        IScope activate(ISpan span, bool finishSpanOnClose);

        /**
         * Return the currently active {@link Scope} which can be used to access the currently active
         * {@link Scope#span()}.
         *
         * <p>
         * If there is an {@link Scope non-null scope}, its wrapped {@link Span} becomes an implicit parent
         * (as {@link References#CHILD_OF} reference) of any
         * newly-created {@link Span} at {@link Tracer.SpanBuilder#startActive(boolean)} or {@link SpanBuilder#start()}
         * time rather than at {@link Tracer#buildSpan(String)} time.
         *
         * @return the {@link Scope active scope}, or null if none could be found.
         */
        IScope active();
    }
}
