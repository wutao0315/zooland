using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{
    public class IntOrStringTag : IntTag
    {
        public IntOrStringTag(string key) : base(key)
        {
        }

        protected override void set(ISpan span, int tagValue)
        {
            base.set(span, tagValue);
        }


        //protected override void set(ISpan span, string tagValue)
        //{
        //    span.setTag(base.key, tagValue);
        //}
    }
}

