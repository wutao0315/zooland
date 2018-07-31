using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.RemotingImpl.Support
{
    /// <summary>
    /// Configurable implementation of the <see cref="ILifetime"/> interface.
    /// </summary>
    /// <author>Bruno Baia</author>
    public class ConfigurableLifetime : ILifetime
    {
        #region ILifetime Members

        /// <summary>
        /// Gets or sets a value indicating whether this instance has infinite lifetime.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has infinite lifetime; otherwise, <c>false</c>.
        /// </value>
        public bool Infinite { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial lease time.
        /// </summary>
        /// <value>The initial lease time.</value>
        public TimeSpan InitialLeaseTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the amount of time lease 
        /// should be extended for on each call to this object.
        /// </summary>
        /// <value>The amount of time lease should be 
        /// extended for on each call to this object.</value>
        public TimeSpan RenewOnCallTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the amount of time lease manager  
        /// will for this object's sponsors to respond.
        /// </summary>
        /// <value>The amount of time lease manager will for this object's
        /// sponsors to respond.</value>
        public TimeSpan SponsorshipTimeout { get; set; } = TimeSpan.Zero;

        #endregion
    }
}
