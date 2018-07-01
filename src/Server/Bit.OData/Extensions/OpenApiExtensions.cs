using Bit.WebApi.Implementations;
using Microsoft.AspNet.OData.Query;
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
            doc.ApplyDefaultApiConfig(webApiConfig);
            doc.OperationFilter<OpenApiIgnoreParameterTypeOperationFilter<ODataQueryOptions>>();
            doc.CustomProvider(defaultProvider => new ODataSwaggerProvider(defaultProvider, doc, webApiConfig).Configure(odataConfig =>
            {
                odataConfig.EnableSwaggerRequestCaching();
                odataConfig.IncludeNavigationProperties();
                odataConfig.SetAssembliesResolver((System.Web.Http.Dispatcher.IAssembliesResolver)webApiConfig.DependencyResolver.GetService(typeof(System.Web.Http.Dispatcher.IAssembliesResolver)));
            }));

            return doc;
        }
    }
}
