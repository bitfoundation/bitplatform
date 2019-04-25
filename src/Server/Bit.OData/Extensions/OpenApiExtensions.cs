using Bit.OData.Implementations;
using Bit.WebApi.Implementations;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Swashbuckle.OData;
using System.Web.Http;

namespace Swashbuckle.Application
{
    public static class OpenApiExtensions
    {
        /// <summary>
        /// Calls <see cref="OpenApiExtensions.ApplyDefaultODataConfig(SwaggerDocsConfig, HttpConfiguration)"/>
        /// | Ignores ODataQueryOptions parameter type
        /// | Uses <see cref="ODataSwaggerProvider"/>
        /// </summary>
        public static SwaggerDocsConfig ApplyDefaultODataConfig(this SwaggerDocsConfig doc, HttpConfiguration webApiConfig)
        {
            doc.DocumentFilter<RemoveDefaultODataNamespaceFromSwaggerDocumentFilter>();
            doc.ApplyDefaultApiConfig(webApiConfig);
            doc.OperationFilter<OpenApiIgnoreParameterTypeOperationFilter<ODataQueryOptions>>();
            doc.CustomProvider(defaultProvider => new ODataSwaggerProvider(defaultProvider, doc, webApiConfig).Configure(odataConfig =>
            {
                odataConfig.EnableSwaggerRequestCaching();
                odataConfig.IncludeNavigationProperties();
                odataConfig.SetAssembliesResolver((System.Web.Http.Dispatcher.IAssembliesResolver)webApiConfig.DependencyResolver.GetService(typeof(System.Web.Http.Dispatcher.IAssembliesResolver)));
            }));

            doc.GroupActionsBy(apiDesc => $"[{((ODataRoute)apiDesc.Route).RoutePrefix}] {apiDesc.ActionDescriptor.ControllerDescriptor.ControllerName}");

            return doc;
        }
    }
}
