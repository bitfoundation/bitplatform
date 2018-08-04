using Bit.WebApi.ActionFilters;
using Bit.WebApi.Contracts;
using System.Web.Http;

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

    public class GlobalDefaultLogOperationArgsActionFilterProvider<TOperationArgsArgs> : IWebApiConfigurationCustomizer
        where TOperationArgsArgs : LogOperationArgsFilterAttribute, new()
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new TOperationArgsArgs());
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
