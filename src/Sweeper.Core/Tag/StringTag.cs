using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{
    public class StringTag : AbstractTag<string>
    {
        public StringTag(string key) : base(key)
        {
        }


        protected override void set(ISpan span, string tagValue)
        {
            span.setTag(base.key, tagValue);
        }
    }
}

