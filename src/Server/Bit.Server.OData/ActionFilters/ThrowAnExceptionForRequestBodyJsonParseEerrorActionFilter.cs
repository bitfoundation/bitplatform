using Bit.WebApi.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.OData.ActionFilters
{
    public class ThrowAnExceptionForRequestBodyJsonParseErrorActionFilter : ActionFilterAttribute, IWebApiConfigurationCustomizer
    {
        public void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            webApiConfiguration.Filters.Add(this);
        }

        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (actionContext == null)
                throw new ArgumentNullException(nameof(actionContext));

            if (actionContext.Request.Properties.TryGetValue("Request_Body_Json_Parse_Error", out object? exception) && exception is Exception exp)
                throw exp;

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }
    }
}
