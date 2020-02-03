using Autofac;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Autofac.Integration.SignalR
{
    /// <summary>
    /// Autofac implementation of the <see cref="IDependencyResolver"/> interface.
    /// </summary>
    public class AutofacDependencyResolver : DefaultDependencyResolver
    {
        readonly ILifetimeScope _lifetimeScope;

        protected AutofacDependencyResolver()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacDependencyResolver" /> class.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope that services will be resolved from.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="lifetimeScope" /> is <see langword="null" />.
        /// </exception>
        public AutofacDependencyResolver(ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException("lifetimeScope");

            _lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// Gets the Autofac implementation of the dependency resolver.
        /// </summary>
        public static AutofacDependencyResolver Current
        {
            get { return GlobalHost.DependencyResolver as AutofacDependencyResolver; }
        }

        /// <summary>
        /// Gets the <see cref="ILifetimeScope"/> that was provided to the constructor.
        /// </summary>
        public virtual ILifetimeScope LifetimeScope
        {
            get { return _lifetimeScope; }
        }

        /// <summary>
        /// Get a single instance of a service.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>The single instance if resolved; otherwise, <c>null</c>.</returns>
        public override object GetService(Type serviceType)
        {
            return _lifetimeScope.ResolveOptional(serviceType) ?? base.GetService(serviceType);
        }

        /// <summary>
        /// Gets all available instances of a services.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>The list of instances if any were resolved; otherwise, an empty list.</returns>
        public override IEnumerable<object> GetServices(Type serviceType)
        {
            var enumerableServiceType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            var instance = (IEnumerable<object>)_lifetimeScope.Resolve(enumerableServiceType);
            return instance.Any() ? instance : base.GetServices(serviceType);
        }
    }
}
