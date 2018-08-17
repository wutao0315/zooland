using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Trace
{
    public class Span
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Parent ID
        /// </summary>
        public int ParentId { get; set; }
        /// <summary>
        /// Trace Id
        /// </summary>
        public long TraceId { get; set; }
        /// <summary>
        /// span Name
        /// </summary>
        public string Name { get; set; }
    }
}
