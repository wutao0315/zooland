using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.RemotingImpl.Support
{
    /// <summary>
    /// This class extends <see cref="MarshalByRefObject"/> to allow users
    /// to define object lifecycle details by simply setting its properties.
    /// </summary>
    /// <remarks>
    /// <p>
    /// Remoting exporters uses this class as a base proxy class
    /// in order to support lifecycle configuration when exporting 
    /// a remote object.
    /// </p>
    /// </remarks>
    /// <author>Aleksandar Seovic</author>
    public abstract class BaseRemoteObject : MarshalByRefObject
    {


        #region Constructor(s) / Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRemoteObject"/> class.
        /// </summary>
        public BaseRemoteObject()
        { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance has infinite lifetime.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance has infinite lifetime; 
        /// otherwise, <see langword="false" /> .
        /// </value>
        public bool IsInfinite { get; set; } = false;

        /// <summary>
        /// Gets or sets the initial lease time.
        /// </summary>
        /// <value>The initial lease time.</value>
        public TimeSpan InitialLeaseTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the amount of time lease should be 
        /// extended for on each call to this object.
        /// </summary>
        /// <value>The amount of time lease should be 
        /// extended for on each call to this object.</value>
        public TimeSpan RenewOnCallTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the amount of time lease manager will for this object's
        /// sponsors to respond.
        /// </summary>
        /// <value>The amount of time lease manager will for this object's
        /// sponsors to respond.</value>
        public TimeSpan SponsorshipTimeout { get; set; } = TimeSpan.Zero;

        #endregion

        #region Methods

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <remarks>
        /// <p>
        /// This method uses property values to configure <see cref="ILease"/> for this object.
        /// </p>
        /// <p>
        /// It is very much inspired by Ingo Rammer's example in Chapter 6 of "Advanced .NET Remoting",
        /// but is modified slightly to make it more "Spring-friendly". Basically, the main difference is that
        /// instead of pulling lease configuration from the .NET config file, this implementation relies
        /// on Spring DI to get appropriate values injected, which makes it much more flexible.
        /// </p>
        /// </remarks>
        /// <returns>
        /// An object of type <see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> used to control the
        /// lifetime policy for this instance. This is the current lifetime service object for
        /// this instance if one exists; otherwise, a new lifetime service object initialized to the value
        /// of the <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime" qualify="true"/> property.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
        public override object InitializeLifetimeService()
        {
            if (this.IsInfinite)
            {
                return null;
            }

            ILease lease = (ILease)base.InitializeLifetimeService();
            if (this.InitialLeaseTime != TimeSpan.Zero)
            {
                lease.InitialLeaseTime = this.InitialLeaseTime;
            }
            if (this.RenewOnCallTime != TimeSpan.Zero)
            {
                lease.RenewOnCallTime = this.RenewOnCallTime;
            }
            if (this.SponsorshipTimeout != TimeSpan.Zero)
            {
                lease.SponsorshipTimeout = this.SponsorshipTimeout;
            }

            return lease;
        }

        #endregion
    }
}
