using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{
    public abstract class AbstractTag<T>
    {
        protected readonly string key;

        public AbstractTag(string tagKey)
        {
            this.key = tagKey;
        }

        public string getKey()
        {
            return key;
        }

        protected abstract void set(ISpan span, T tagValue);
    }
}


