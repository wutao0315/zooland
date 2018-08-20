using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweeper.Core;


namespace Sweeper.Noop
{
    public sealed class NoopTracerFactory
    {
        private NoopTracerFactory() { }
        public static ITracer create()
        {
            return NoopTracerImpl.INSTANCE;
        }
    }

}



