using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Objects.Factory;
using Zooyard.Core;

namespace Spring.Services.Zooyard.Activation
{
    /// <summary>
    /// Factory that provides instances of <see cref="ServiceHost" /> 
    /// to host objects created with Spring's IoC container.
    /// </summary>
    /// <author>Bruno Baia</author>
    public class ServiceHostFactoryObject : IFactoryObject, IInitializingObject, IObjectFactoryAware, IDisposable
    {
        #region Logging

        private static readonly Common.Logging.ILog LOG = Common.Logging.LogManager.GetLogger(typeof(ServiceHostFactoryObject));

        #endregion

        #region Fields

    

        /// <summary>
        /// The owning factory.
        /// </summary>
        private IObjectFactory objectFactory;


        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the target object that should be exposed as a service.
        /// </summary>
        /// <value>
        /// The name of the target object that should be exposed as a service.
        /// </value>
        public IServer SpringServiceHost { get; set; }
        

        #endregion

        #region Constructor(s) / Destructor

        /// <summary>
        /// Creates a new instance of the 
        /// <see cref="Spring.ServiceModel.Activation.ServiceHostFactoryObject"/> class.
        /// </summary>
        public ServiceHostFactoryObject()
        {
        }

        #endregion

        #region IObjectFactoryAware Members

        /// <summary>
        /// Callback that supplies the owning factory to an object instance.
        /// </summary>
        /// <value>
        /// Owning <see cref="Spring.Objects.Factory.IObjectFactory"/>
        /// (may not be <see langword="null"/>). The object can immediately
        /// call methods on the factory.
        /// </value>
        /// <remarks>
        /// <p>
        /// Invoked after population of normal object properties but before an init
        /// callback like <see cref="Spring.Objects.Factory.IInitializingObject"/>'s
        /// <see cref="Spring.Objects.Factory.IInitializingObject.AfterPropertiesSet"/>
        /// method or a custom init-method.
        /// </p>
        /// </remarks>
        /// <exception cref="Spring.Objects.ObjectsException">
        /// In case of initialization errors.
        /// </exception>
        public virtual IObjectFactory ObjectFactory
        {
            protected get { return this.objectFactory; }
            set { this.objectFactory = value; }
        }

        #endregion

        #region IFactoryObject Members

        /// <summary>
        /// Return a <see cref="Spring.ServiceModel.SpringServiceHost" /> instance 
        /// managed by this factory.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="Spring.ServiceModel.SpringServiceHost" /> 
        /// managed by this factory.
        /// </returns>
        public virtual object GetObject()
        {
            return SpringServiceHost;
        }

        /// <summary>
        /// Return the <see cref="System.Type"/> of object that this
        /// <see cref="Spring.Objects.Factory.IFactoryObject"/> creates.
        /// </summary>
        public virtual Type ObjectType
        {
            get { return typeof(IServer); }
        }

        /// <summary>
        /// Always returns <see langword="false"/>
        /// </summary>
        public virtual bool IsSingleton
        {
            get { return false; }
        }

        #endregion

        #region IInitializingObject Members

        /// <summary>
        /// Publish the object.
        /// </summary>
        public virtual void AfterPropertiesSet()
        {
            ValidateConfiguration();

            //springServiceHost = new SpringServiceHost(TargetName, objectFactory, UseServiceProxyTypeCache, BaseAddresses);

            SpringServiceHost.Export();

            #region Instrumentation

            if (LOG.IsInfoEnabled)
            {
                LOG.Info($"The service '{ObjectType.Name}' is ready and can now be accessed.");
            }

            #endregion
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Close the SpringServiceHost
        /// </summary>
        public void Dispose()
        {
            if (SpringServiceHost != null)
            {
                SpringServiceHost.Dispose();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            //if (TargetName == null)
            //{
            //    throw new ArgumentException("The TargetName property is required.");
            //}
        }

        #endregion
    }
}
