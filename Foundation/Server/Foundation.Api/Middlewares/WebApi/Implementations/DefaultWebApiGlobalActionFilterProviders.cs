using System;
using System.Collections.Generic;
using System.Web.Http;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.ActionFilters;
using Foundation.Core.Contracts;

namespace Foundation.Api.Middlewares.WebApi.Implementations
{
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
