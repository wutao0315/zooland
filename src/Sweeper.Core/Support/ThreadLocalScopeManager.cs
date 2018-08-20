using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sweeper.Core.Support
{
    public class ThreadLocalScopeManager : IScopeManager
    {
        //final ThreadLocal<ThreadLocalScope> tlsScope = new ThreadLocal<ThreadLocalScope>();
        public readonly ThreadLocal<ThreadLocalScope> tlsScope = new ThreadLocal<ThreadLocalScope>();

        public IScope activate(ISpan span, bool finishOnClose)
        {
            return new ThreadLocalScope(this, span, finishOnClose);
        }


        public IScope active()
        {
            return tlsScope.Value;
        }
    }
}
