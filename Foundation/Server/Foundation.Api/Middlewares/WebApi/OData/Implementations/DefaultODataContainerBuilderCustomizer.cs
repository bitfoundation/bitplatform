using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;

namespace Foundation.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultODataContainerBuilderCustomizer : IODataContainerBuilderCustomizer
    {
        public virtual void Customize(IContainerBuilder container)
        {
            container.AddService<ODataUriResolver, DefaultODataUriResolver>(ServiceLifetime.Singleton);
            container.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(ServiceLifetime.Singleton);
            container.AddService<ODataEnumDeserializer, DefaultODataEnumDeserializer>(ServiceLifetime.Singleton);
            container.AddService<ODataResourceDeserializer, DefaultODataResourceDeserializer>(ServiceLifetime.Singleton);
            container.AddService<ODataActionPayloadDeserializer, DefaultODataActionPayloadDeserializer>(ServiceLifetime.Singleton);
        }
    }
}
