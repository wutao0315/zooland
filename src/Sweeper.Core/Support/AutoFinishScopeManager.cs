using Sweeper.Core.Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sweeper.Core.Support
{
    public class AutoFinishScopeManager : IScopeManager
    {
        public ThreadLocal<AutoFinishScope> tlsScope = new ThreadLocal<AutoFinishScope>();

        public IScope activate(ISpan span, bool finishOnClose)
        {
            return new AutoFinishScope(this, new AtomicInteger(1), span);
        }

        public IScope active()
        {
            return tlsScope.Value;
        }
    }
}
