using Microsoft.AspNet.OData.Builder;
using System.Web.Http;

namespace Bit.OData.Contracts
{
    /// <summary>
    /// By implementing this, you can provide your own <see cref="Microsoft.AspNet.OData.Builder.ODataModelBuilder"/>.
    /// Default implementation <see cref="Implementations.DefaultODataModelBuilderProvider"/> uses <see cref="Microsoft.AspNet.OData.Builder.ODataConventionModelBuilder"/>
    /// </summary>
    public interface IODataModelBuilderProvider
    {
        ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string? @namespace);
    }
}
