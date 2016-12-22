using Foundation.Api.Contracts;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Foundation.Api.Middlewares.WebApi.ActionFilters
{
    public class OwinActionFilterAttribute : ActionFilterAttribute, IExceptionFilter, IFilter
    {
        public virtual IOwinActionFilter OwinActionFilter { get; }

        public OwinActionFilterAttribute(Type owinMiddlewareActionFilter)
        {
            if (owinMiddlewareActionFilter == null)
                throw new ArgumentNullException(nameof(owinMiddlewareActionFilter));

            OwinActionFilter = (IOwinActionFilter)Activator.CreateInstance(owinMiddlewareActionFilter);
        }

        public virtual bool AllowMultiple => true;

        public virtual async Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {

        }

        public virtual async Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            await OwinActionFilter.OnExceptionAsync(actionExecutedContext.Request.GetOwinContext(), actionExecutedContext.Exception, cancellationToken);
        }

        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            await OwinActionFilter.OnActionExecutingAsync(actionContext.Request.GetOwinContext(), cancellationToken);
            await base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            await OwinActionFilter.OnActionExecutedAsync(actionExecutedContext.Request.GetOwinContext(), cancellationToken);
            await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}
