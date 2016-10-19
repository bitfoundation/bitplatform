using Autofac;
using Foundation.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Foundation.Api.Implementations
{
    public class AutofacScopeBasedDependencyResolver : IDependencyResolver
    {
        private readonly ILifetimeScope _lifetimeScope;

        protected AutofacScopeBasedDependencyResolver()
        {
        }

        public AutofacScopeBasedDependencyResolver(ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            _lifetimeScope = lifetimeScope;
        }

        public virtual TContract Resolve<TContract>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return _lifetimeScope.Resolve<TContract>();
        }

        public virtual IEnumerable<TContract> ResolveAll<TContract>(string name = null)
        {
            if (name != null)
                throw new NotSupportedException();

            return _lifetimeScope.Resolve<IEnumerable<TContract>>();
        }

        public virtual object GetService(TypeInfo serviceType)
        {
            return _lifetimeScope.ResolveOptional(serviceType);
        }

        public virtual object GetService(Type serviceType)
        {
            return _lifetimeScope.ResolveOptional(serviceType);
        }

        public virtual IDependencyResolver CreateScope()
        {
            return new AutofacScopeBasedDependencyResolver(_lifetimeScope.BeginLifetimeScope());
        }

        public virtual void Dispose()
        {
            _lifetimeScope.Dispose();
        }

        public virtual TContract ResolveOptional<TContract>(string name = null)
            where TContract : class
        {
            if (name != null)
                return _lifetimeScope.ResolveNamed<TContract>(name);

            return _lifetimeScope.ResolveOptional<TContract>();
        }

        public virtual object Resolve(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            if (name != null)
                return _lifetimeScope.ResolveNamed(name, contractType);

            return _lifetimeScope.Resolve(contractType);
        }

        public virtual object ResolveOptional(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            if (name != null)
                return _lifetimeScope.ResolveNamed(name, contractType);

            return _lifetimeScope.ResolveOptional(contractType);
        }

        public virtual IEnumerable<object> ResolveAll(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            if (name != null)
                return (IEnumerable<object>)_lifetimeScope.ResolveNamed(name, contractType);

            return (IEnumerable<object>)_lifetimeScope.Resolve(contractType);
        }

        public virtual bool IsRegistered<TContract>()
        {
            return _lifetimeScope.IsRegistered<TContract>();
        }

        public virtual bool IsRegistered(TypeInfo contractType)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            return _lifetimeScope.IsRegistered(contractType);
        }
    }
}
