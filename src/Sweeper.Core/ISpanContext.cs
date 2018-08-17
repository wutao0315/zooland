using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    public interface ISpanContext
    {
        /**
     * @return all zero or more baggage items propagating along with the associated Span
     *
     * @see Span#setBaggageItem(String, String)
     * @see Span#getBaggageItem(String)
     */
        IEnumerable<IDictionary<string, string>> baggageItems();

    }
}
