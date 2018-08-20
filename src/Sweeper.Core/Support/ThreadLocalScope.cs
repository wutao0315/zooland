using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Support
{
    public class ThreadLocalScope:IScope
    {
        private readonly ThreadLocalScopeManager scopeManager;
        private readonly ISpan wrapped;
        private readonly bool finishOnClose;
        private readonly ThreadLocalScope toRestore;

        public ThreadLocalScope(ThreadLocalScopeManager scopeManager, ISpan wrapped, bool finishOnClose)
        {
            this.scopeManager = scopeManager;
            this.wrapped = wrapped;
            this.finishOnClose = finishOnClose;
            this.toRestore = scopeManager.tlsScope.Value;
            scopeManager.tlsScope.Value=this;
        }


        public void close()
        {
            if (scopeManager.tlsScope.Value != this)
            {
                // This shouldn't happen if users call methods in the expected order. Bail out.
                return;
            }

            if (finishOnClose)
            {
                wrapped.finish();
            }

            scopeManager.tlsScope.Value=toRestore;
        }


        public ISpan span()
        {
            return wrapped;
        }
    }
}
