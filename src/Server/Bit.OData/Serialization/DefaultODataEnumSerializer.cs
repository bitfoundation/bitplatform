using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData.Formatter.Serialization;

namespace Bit.OData.Serialization
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

            if (writeContext.Request.Headers.TryGetValues("Bit-Client-Type", out IEnumerable<string> values) && values.Any(v => string.Equals(v, "TS-Client", System.StringComparison.InvariantCultureIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(result?.Value) && !string.IsNullOrEmpty(result.TypeName))
                {
                    // EnumKey >> "Namespace.EnumType'EnumKey'"
                    result = new ODataEnumValue($"{result.TypeName}'{result.Value}'");
                }
            }

            return result;
        }
    }
}
