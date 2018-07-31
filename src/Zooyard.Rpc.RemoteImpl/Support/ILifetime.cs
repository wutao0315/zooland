using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.RemotingImpl.Support
{
    /// <summary>
    /// Defines lifetime's properties of remote objects that is used by Spring.
    /// </summary>
    /// <author>Bruno Baia</author>
    public interface ILifetime
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance has infinite lifetime.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has infinite lifetime; otherwise, <c>false</c>.
        /// </value>
        bool Infinite { get; }

        /// <summary>
        /// Gets the initial lease time.
        /// </summary>
        /// <value>The initial lease time.</value>
        TimeSpan InitialLeaseTime { get; }

        /// <summary>
        /// Gets the amount of time lease 
        /// should be extended for on each call to this object.
        /// </summary>
        /// <value>The amount of time lease should be 
        /// extended for on each call to this object.</value>
        TimeSpan RenewOnCallTime { get; }

        /// <summary>
        /// Gets the amount of time lease manager 
        /// will for this object's sponsors to respond.
        /// </summary>
        /// <value>The amount of time lease manager will for this object's
        /// sponsors to respond.</value>
        TimeSpan SponsorshipTimeout { get; }
    }
}
