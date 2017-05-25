using System.Web.Http;
using System.Web.OData.Builder;
using Bit.Api.Middlewares.WebApi.OData.Contracts;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultODataModelBuilderProvider : IODataModelBuilderProvider
    {
        public virtual ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string @namespace)
        {
            return new ODataConventionModelBuilder(webApiConfig)
            {
                ContainerName = containerName,
                Namespace = @namespace
            };
        }
    }
}
