using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Owin;
using System;
using System.Net.Http;

namespace Bit.OData.Serialization
{
    public class DefaultODataPrimitiveSerializer : ODataPrimitiveSerializer
    {
        public override ODataPrimitiveValue? CreateODataPrimitiveValue(object graph,
            IEdmPrimitiveTypeReference primitiveType, ODataSerializerContext writeContext)
        {
            if (writeContext == null)
                throw new ArgumentNullException(nameof(writeContext));

            ODataPrimitiveValue? result = base.CreateODataPrimitiveValue(graph, primitiveType, writeContext);

            if (result?.Value is DateTimeOffset date)
            {
                IDependencyResolver dependencyResolver = writeContext.Request.GetOwinContext()
                                    .GetDependencyResolver();

                ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

                result = new ODataPrimitiveValue(timeZoneManager.MapFromServerToClient(date));
            }

            return result;
        }
    }
}