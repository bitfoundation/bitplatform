using System.Web.Http;
using System.Web.OData.Builder;
using Bit.OData.Contracts;

namespace Bit.OData.Implementations
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
