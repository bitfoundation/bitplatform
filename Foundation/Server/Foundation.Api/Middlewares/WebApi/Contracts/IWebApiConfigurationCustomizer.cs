using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiConfigurationCustomizer
    {
        void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration);
    }
}
