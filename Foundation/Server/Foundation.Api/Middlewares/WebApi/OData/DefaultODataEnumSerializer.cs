using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Web.OData.Formatter.Serialization;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataEnumSerializer : ODataEnumSerializer
    {
        public DefaultODataEnumSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
        }

        public override ODataEnumValue CreateODataEnumValue(object graph, IEdmEnumTypeReference enumType, ODataSerializerContext writeContext)
        {
            ODataEnumValue result = base.CreateODataEnumValue(graph, enumType, writeContext);

            if (result != null && !string.IsNullOrEmpty(result.Value) && !string.IsNullOrEmpty(result.TypeName))
            {
                // EnumKey >> "Namespace.EnumType'EnumKey'"
                result = new ODataEnumValue($"{result.TypeName}'{result.Value}'");
            }

            return result;
        }
    }
}
