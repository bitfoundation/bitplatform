using System.Web.Http;
using System.Web.OData.Builder;

namespace Foundation.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IODataModelBuilderProvider
    {
        ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string @namespace);
    }
}
