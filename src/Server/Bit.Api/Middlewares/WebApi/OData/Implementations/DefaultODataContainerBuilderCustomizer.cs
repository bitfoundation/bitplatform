using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Microsoft.OData;
using Microsoft.OData.UriParser;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultODataContainerBuilderCustomizer : IODataContainerBuilderCustomizer
    {
        public virtual void Customize(IContainerBuilder container)
        {
            container.AddService<ODataUriResolver, DefaultODataUriResolver>(ServiceLifetime.Singleton);
            container.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(ServiceLifetime.Singleton);
            container.AddService<ODataEnumDeserializer, DefaultODataEnumDeserializer>(ServiceLifetime.Singleton);
            container.AddService<ODataDeserializerProvider, ExtendedODataDeserializerProvider>(ServiceLifetime.Singleton);
            container.AddService<ODataEnumSerializer, DefaultODataEnumSerializer>(ServiceLifetime.Singleton);
            container.AddService<ODataResourceDeserializer, DefaultODataResourceDeserializer>(ServiceLifetime.Singleton);
            container.AddService<ODataActionPayloadDeserializer, DefaultODataActionPayloadDeserializer>(ServiceLifetime.Singleton);
            container.AddService<DefaultODataParameterDeserializer>(ServiceLifetime.Singleton);
        }
    }
}
