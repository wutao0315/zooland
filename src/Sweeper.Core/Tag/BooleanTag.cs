using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{
    public class BooleanTag : AbstractTag<bool> {
    public BooleanTag(string key):base(key)
    {
    }

        protected override void set(ISpan span, bool tagValue)
    {
        span.setTag(base.key, tagValue);
    }
}
}

