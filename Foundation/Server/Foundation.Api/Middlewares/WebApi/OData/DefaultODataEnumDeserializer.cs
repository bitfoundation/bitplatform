using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Web.OData.Formatter.Deserialization;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataEnumDeserializer : ODataEnumDeserializer
    {
        public override object ReadInline(object item, IEdmTypeReference edmType, ODataDeserializerContext readContext)
        {
            ODataEnumValue valueItem = item as ODataEnumValue;
            if (valueItem != null && !string.IsNullOrEmpty(valueItem.Value) && !string.IsNullOrEmpty(valueItem.TypeName))
            {
                // "Namespace.EnumType'EnumKey'" >> EnumKey
                valueItem = new ODataEnumValue(valueItem.Value.Replace(valueItem.TypeName, string.Empty).Replace("'", string.Empty));
            }
            object result = base.ReadInline(valueItem, edmType, readContext);
            return result;
        }
    }
}
