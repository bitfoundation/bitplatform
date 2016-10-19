using System.Web.Http;
using System.Web.Http.Filters;

namespace Foundation.Api.Middlewares.WebApi.Contracts
{
    public interface IWebApiGlobalActionFiltersProvider
    {
        void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration);
    }
}
