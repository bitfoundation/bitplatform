using Microsoft.Web.Http.Description;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Bit.WebApi.Contracts
{
    public delegate HttpServer WebApiHttpServerFactory(HttpConfiguration webApiConfiguration);
    public delegate IInlineConstraintResolver WebApiInlineConstraintResolverFactory();
    public delegate VersionedApiExplorer WebApiExplorerFactory(HttpConfiguration webApiConfiguration);
    // You can customize HttpConfiguration (webApiConfiguration) using IWebApiConfigurationCustomizer
}
