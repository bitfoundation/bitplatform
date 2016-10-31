using System;
using System.Net.Http;
using System.Web.OData.Formatter.Serialization;
using Foundation.Core.Contracts;
using Microsoft.OData.Edm;
using Foundation.Api.Contracts;
using Microsoft.OData;
using Microsoft.Owin;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataPrimitiveSerializer : ODataPrimitiveSerializer
    {
        public override ODataPrimitiveValue CreateODataPrimitiveValue(object graph,
            IEdmPrimitiveTypeReference primitiveType, ODataSerializerContext writeContext)
        {
            ODataPrimitiveValue result = base.CreateODataPrimitiveValue(graph, primitiveType, writeContext);

            if (result?.Value is DateTimeOffset)
            {
                DateTimeOffset date = (DateTimeOffset)result.Value;

                IDependencyResolver dependencyResolver = writeContext.Request.GetOwinContext()
                                    .GetDependencyResolver();

                ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

                result = new ODataPrimitiveValue(timeZoneManager.MapFromServerToClient(date));
            }

            return result;
        }
    }
}