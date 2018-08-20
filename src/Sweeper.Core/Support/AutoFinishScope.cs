using Sweeper.Core.Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Support
{
    public class AutoFinishScope : IScope
    {
        readonly AutoFinishScopeManager manager;
        readonly AtomicInteger refCount;
        private readonly ISpan wrapped;
        private readonly AutoFinishScope toRestore;

        public AutoFinishScope(AutoFinishScopeManager manager, AtomicInteger refCount, ISpan wrapped)
        {
            this.manager = manager;
            this.refCount = refCount;
            this.wrapped = wrapped;
            this.toRestore = manager.tlsScope.Value;
            manager.tlsScope.Value = this;
        }

        public class Continuation
        {
            readonly AutoFinishScopeManager manager;
            readonly AtomicInteger refCount;
            private readonly ISpan wrapped;

            public Continuation(AutoFinishScopeManager manager, AtomicInteger refCount, ISpan wrapped)
            {
                this.manager = manager;
                this.refCount = refCount;
                this.wrapped = wrapped;

                refCount.IncrementAndGet();
            }

            public AutoFinishScope activate()
            {
                return new AutoFinishScope(manager, refCount, wrapped);
            }
        }

        public Continuation capture()
        {
            return new Continuation(manager, refCount, wrapped);
        }


        public void close()
        {
            if (manager.tlsScope.Value != this)
            {
                return;
            }

            if (refCount.DecrementAndGet() == 0)
            {
                wrapped.finish();
            }

            manager.tlsScope.Value = toRestore;
        }


        public ISpan span()
        {
            return wrapped;
        }
    }
}
