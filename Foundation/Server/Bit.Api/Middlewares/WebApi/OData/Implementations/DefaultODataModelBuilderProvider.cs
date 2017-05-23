using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using System.Web.Http;
using System.Web.OData.Builder;

namespace Foundation.Api.Middlewares.WebApi.OData.Implementations
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
