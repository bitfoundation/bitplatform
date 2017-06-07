using System.Web.Http;
using Bit.Api.Middlewares.WebApi.ActionFilters;
using Bit.Api.Middlewares.WebApi.Contracts;

namespace Bit.Api.Middlewares.WebApi.Implementations
{
    public class GlobalHostAuthenticationFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));
        }
    }

    public class GlobalDefaultExceptionHandlerActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new ExceptionHandlerFilterAttribute());
        }
    }

    public class GlobalDefaultLogActionArgsActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new LogActionArgsFilterAttribute());
        }
    }

    public class GlobalDefaultRequestModelStateValidatorActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestModelStateValidatorActionFilterAttribute());
        }
    }
}
