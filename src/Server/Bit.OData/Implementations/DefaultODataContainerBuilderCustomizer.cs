using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using Bit.OData.Contracts;
using Bit.OData.Serialization;
using Microsoft.OData;
using Microsoft.OData.UriParser;

namespace Bit.OData.Implementations
{
    public class DefaultODataContainerBuilderCustomizer : IODataContainerBuilderCustomizer
    {
        public virtual void Customize(IContainerBuilder container)
        {
            container.AddService<ODataUriResolver, DefaultODataUriResolver>(ServiceLifetime.Singleton);
            container.AddService<ODataPrimitiveSerializer, DefaultODataPrimitiveSerializer>(ServiceLifetime.Singleton);
            container.AddService<ODataDeserializerProvider, ExtendedODataDeserializerProvider>(ServiceLifetime.Singleton);
            container.AddService<ODataEnumSerializer, DefaultODataEnumSerializer>(ServiceLifetime.Singleton);
            container.AddService<DefaultODataActionParameterDeserializer>(ServiceLifetime.Singleton);
        }
    }
}
