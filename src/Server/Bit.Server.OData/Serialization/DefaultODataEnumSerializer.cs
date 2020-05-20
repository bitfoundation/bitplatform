using Bit.Core.Contracts;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Owin;
using System;
using System.Net.Http;

namespace Bit.OData.Serialization
{
    public class DefaultODataEnumSerializer : ODataEnumSerializer
    {
        public DefaultODataEnumSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
        }

        public override ODataEnumValue? CreateODataEnumValue(object graph, IEdmEnumTypeReference enumType, ODataSerializerContext writeContext)
        {
            if (writeContext == null)
                throw new ArgumentNullException(nameof(writeContext));

            ODataEnumValue? result = base.CreateODataEnumValue(graph, enumType, writeContext);

            string? clientType = writeContext.Request.GetOwinContext().GetDependencyResolver().Resolve<IRequestInformationProvider>().BitClientType;

            if (string.Equals(clientType, "TS-Client", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!string.IsNullOrEmpty(result?.Value) && !string.IsNullOrEmpty(result!.TypeName))
                {
                    // EnumKey >> "Namespace.EnumType'EnumKey'"
                    result = new ODataEnumValue($"{result.TypeName}'{result.Value}'");
                }
            }

            return result;
        }
    }
}
