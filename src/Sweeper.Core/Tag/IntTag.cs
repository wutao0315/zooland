using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{
    public class IntTag : AbstractTag<int>
    {
        public IntTag(string key) : base(key)
        {
        }

        protected override void set(ISpan span, int tagValue)
        {
            span.setTag(base.key, tagValue);
        }
    }

}
