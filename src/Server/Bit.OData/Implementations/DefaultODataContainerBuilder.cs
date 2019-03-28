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

namespace Bit.OData.Implementations
{
    public class DefaultODataContainerBuilder : DefaultContainerBuilder, IContainerBuilder, IDisposable
    {
        public virtual IDependencyManager DependencyManager { get; set; }

        IDependencyResolver _childDependencyResolver;

        public override IServiceProvider BuildContainer()
        {
            AddDefaultServices();

            _childDependencyResolver = DependencyManager.CreateChildDependencyResolver(childDependencyManager =>
            {
                IServiceCollection services = (IServiceCollection)typeof(DefaultContainerBuilder).GetTypeInfo().GetField(nameof(services), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                childDependencyManager.Populate(services);
            });

            return _childDependencyResolver;
        }

        protected virtual void AddDefaultServices()
        {
            this.AddService<ODataUriResolver, DefaultODataUriResolver>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataDeserializerProvider, ExtendedODataDeserializerProvider>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataEnumSerializer, DefaultODataEnumSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<DefaultODataActionCreateUpdateParameterDeserializer>(Microsoft.OData.ServiceLifetime.Singleton);
            IEnumerable<IODataRoutingConvention> conventions = ODataRoutingConventions.CreateDefault();
            this.AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => conventions);
        }

        public void Dispose()
        {
            _childDependencyResolver?.Dispose();
        }
    }
}
