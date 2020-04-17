using Bit.Core.Contracts;
using Bit.OData.Serialization;
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

        public virtual IDependencyManager DependencyManager { get; set; } = default!;

        public virtual IServiceProvider BuildContainer()
        {
            AddDefaultServices();

            _childDependencyResolver = DependencyManager.CreateChildDependencyResolver(childDependencyManager =>
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

        public virtual void Dispose()
        {
            _childDependencyResolver?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_childDependencyResolver != null)
                await _childDependencyResolver.DisposeAsync();
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
