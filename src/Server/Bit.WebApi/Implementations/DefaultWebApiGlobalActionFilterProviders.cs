using System.Web.Http;
using Bit.WebApi.ActionFilters;
using Bit.WebApi.Contracts;

namespace Bit.WebApi.Implementations
{
    public class GlobalHostAuthenticationFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));
        }
    }

    public class GlobalDefaultExceptionHandlerActionFilterProvider<TExceptionHandlerFilterAttribute> : IWebApiConfigurationCustomizer
        where TExceptionHandlerFilterAttribute : ExceptionHandlerFilterAttribute, new()
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new TExceptionHandlerFilterAttribute());
        }
    }

    public class GlobalDefaultLogActionArgsActionFilterProvider<TLogActionArgs> : IWebApiConfigurationCustomizer
        where TLogActionArgs : LogActionArgsFilterAttribute, new()
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new TLogActionArgs());
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
