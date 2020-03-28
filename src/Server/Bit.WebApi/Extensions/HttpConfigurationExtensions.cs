using Bit.WebApi.Contracts;
using Microsoft.Web.Http.Description;
using Swashbuckle.Application;
using System.Web.Http.Description;

namespace System.Web.Http
{
    public static class HttpConfigurationExtensions
    {
        public static void EnableMultiVersionWebApiSwaggerWithUI(this HttpConfiguration httpConfiguration,
            Action<SwaggerDocsConfig> customizeSwagger = null,
            Action<SwaggerUiConfig> customizeSwaggerUi = null,
            Action<VersionedApiExplorer> customizeApiExplorer = null)
        {
            httpConfiguration.Properties.TryAdd("MultiVersionSwaggerConfiguration", new Action(() =>
            {
                VersionedApiExplorer apiExplorer = ((WebApiExplorerFactory)httpConfiguration.DependencyResolver.GetService(typeof(WebApiExplorerFactory)))(httpConfiguration);

                customizeApiExplorer?.Invoke(apiExplorer);

                httpConfiguration.EnableSwagger("{apiVersion}/swagger", swagger =>
                {
                    swagger.MultipleApiVersions((apiDescription, version) => apiDescription.GetGroupName() == version,
                                info =>
                                {
                                    foreach (ApiDescriptionGroup group in apiExplorer.ApiDescriptions)
                                    {
                                        info.Version(group.Name, $"API {group.ApiVersion}");
                                    }
                                });

                    swagger.ApplyDefaultApiConfig(httpConfiguration);

                    customizeSwagger?.Invoke(swagger);

                }).EnableBitSwaggerUi(swaggerUi => customizeSwaggerUi?.Invoke(swaggerUi));

            }));
        }
    }
}
