using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiGlobalActionFiltersProvider
    {
        void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration);
    }
}
