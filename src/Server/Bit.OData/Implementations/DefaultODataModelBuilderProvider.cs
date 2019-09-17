using Bit.OData.Contracts;
using Microsoft.AspNet.OData.Builder;
using System.Web.Http;

namespace Bit.OData.Implementations
{
    public class DefaultODataModelBuilderProvider : IODataModelBuilderProvider
    {
        public virtual ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string @namespace)
        {
            return new ODataConventionModelBuilder(webApiConfig, isQueryCompositionMode: true)
            {
                ContainerName = containerName,
                Namespace = @namespace
            };
        }
    }
}
