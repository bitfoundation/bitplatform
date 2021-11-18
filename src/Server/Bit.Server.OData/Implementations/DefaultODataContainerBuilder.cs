using Bit.Core.Contracts;
using Bit.OData.Serialization;
using Bit.Owin.Implementations;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.OData.Implementations
{
    public class DefaultODataContainerBuilder : IContainerBuilder, IDisposable, IAsyncDisposable
    {
        private readonly DefaultContainerBuilder _defaultContainerBuilder = new DefaultContainerBuilder();
        private IDependencyResolver? _childDependencyResolver;

        public virtual IServiceProvider BuildContainer()
        {
            AddDefaultServices();

            _childDependencyResolver = DefaultDependencyManager.Current.CreateChildDependencyResolver(childDependencyManager =>
            {
                IServiceCollection services = (IServiceCollection)(typeof(DefaultContainerBuilder).GetTypeInfo().GetField(nameof(services), BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_defaultContainerBuilder) ?? throw new InvalidOperationException($"{nameof(services)} field could not be found in {typeof(DefaultContainerBuilder).FullName}"));

                childDependencyManager.Populate(services);
            });

            return _childDependencyResolver;
        }

        protected virtual void AddDefaultServices()
        {
            this.AddService<ODataUriResolver, BitUnqualifiedCallAndEnumPrefixFreeResolver>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataDeserializerProvider, ExtendedODataDeserializerProvider>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataEnumSerializer, DefaultODataEnumSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<DefaultODataActionCreateUpdateParameterDeserializer>(Microsoft.OData.ServiceLifetime.Singleton);
            IEnumerable<IODataRoutingConvention> conventions = ODataRoutingConventions.CreateDefault();
            this.AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => conventions);
        }

        private bool isDisposed;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            _childDependencyResolver?.Dispose();
            isDisposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (isDisposed) return;
            if (_childDependencyResolver != null)
                await _childDependencyResolver.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        public virtual IContainerBuilder AddService(Microsoft.OData.ServiceLifetime lifetime, Type serviceType, Type implementationType)
        {
            _defaultContainerBuilder.AddService(lifetime, serviceType, implementationType);
            return this;
        }

        public virtual IContainerBuilder AddService(Microsoft.OData.ServiceLifetime lifetime, Type serviceType, Func<IServiceProvider, object> implementationFactory)
        {
            _defaultContainerBuilder.AddService(lifetime, serviceType, implementationFactory);
            return this;
        }
    }
}
