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

    public class GlobalDefaultLogOperationInfoActionFilterProvider<TOperationInfoArgs> : IWebApiConfigurationCustomizer
        where TOperationInfoArgs : LogOperationInfoFilterAttribute, new()
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new TOperationInfoArgs());
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
