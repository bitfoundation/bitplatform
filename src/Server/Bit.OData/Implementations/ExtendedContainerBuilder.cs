using Bit.OData.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using System;
using System.Reflection;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;

namespace Bit.OData.Implementations
{
    public class ExtendedContainerBuilder : DefaultContainerBuilder, IContainerBuilder
    {
        public override IServiceProvider BuildContainer()
        {
            AddDefaultServices();

            IServiceCollection services = (IServiceCollection)GetType().GetTypeInfo().BaseType.GetField(nameof(services), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

            return (IServiceProvider)services.GetType().GetTypeInfo().Assembly.GetType(typeof(ServiceCollectionContainerBuilderExtensions).GetTypeInfo().FullName).GetMethod(nameof(ServiceCollectionContainerBuilderExtensions.BuildServiceProvider), new[] { typeof(IServiceCollection) }).Invoke(null, new object[] { services });
        }

        protected virtual void AddDefaultServices()
        {
            this.AddService<ODataUriResolver, DefaultODataUriResolver>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataDeserializerProvider, ExtendedODataDeserializerProvider>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<ODataEnumSerializer, DefaultODataEnumSerializer>(Microsoft.OData.ServiceLifetime.Singleton);
            this.AddService<DefaultODataActionParameterDeserializer>(Microsoft.OData.ServiceLifetime.Singleton);
        }
    }
}
