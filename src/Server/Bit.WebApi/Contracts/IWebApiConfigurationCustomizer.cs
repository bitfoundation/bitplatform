using System.Web.Http;

namespace Bit.WebApi.Contracts
{
    public interface IWebApiConfigurationCustomizer
    {
        void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration);
    }
}
