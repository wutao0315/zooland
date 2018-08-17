using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    public sealed class References
    {
        /**
     * See http://opentracing.io/spec/#causal-span-references for more information about CHILD_OF references
     */
        public const string CHILD_OF = "child_of";

    /**
     * See http://opentracing.io/spec/#causal-span-references for more information about FOLLOWS_FROM references
     */
    public const string FOLLOWS_FROM = "follows_from";
    }
}
