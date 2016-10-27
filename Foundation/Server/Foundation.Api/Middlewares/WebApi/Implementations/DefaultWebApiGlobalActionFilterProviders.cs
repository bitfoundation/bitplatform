using Foundation.Api.Middlewares.WebApi.ActionFilters;
using Foundation.Api.Middlewares.WebApi.Contracts;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Implementations
{
    public class GlobalHostAuthenticationFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));
        }
    }

    public class GlobalDefaultExceptionHandlerActionFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new ExceptionHandlerFilterAttribute());
        }
    }

    public class GlobalDefaultLogActionArgsActionFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new LogActionArgsFilterAttribute());
        }
    }

    public class GlobalDefaultRequestModelStateValidatorActionFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestModelStateValidatorActionFilterAttribute());
        }
    }
}
