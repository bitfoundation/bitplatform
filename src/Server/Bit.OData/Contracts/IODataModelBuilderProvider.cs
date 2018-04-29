using System.Web.Http;
using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    /// <summary>
    /// By implementing this, you can provide your own <see cref="System.Web.OData.Builder.ODataModelBuilder"/>.
    /// Default implementation <see cref="Implementations.DefaultODataModelBuilderProvider"/> uses <see cref="System.Web.OData.Builder.ODataConventionModelBuilder"/>
    /// </summary>
    public interface IODataModelBuilderProvider
    {
        ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string @namespace);
    }
}
