using System.Web.Http;

namespace Bit.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiConfigurationCustomizer
    {
        void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration);
    }
}
