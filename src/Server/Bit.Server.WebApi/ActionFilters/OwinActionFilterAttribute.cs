using Bit.Owin.Contracts;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.WebApi.ActionFilters
{
    public class OwinActionFilterAttribute : ActionFilterAttribute, IExceptionFilter, IFilter
    {
        public virtual IOwinActionFilter OwinActionFilter { get; }

        public OwinActionFilterAttribute(Type owinMiddlewareActionFilter)
        {
            if (owinMiddlewareActionFilter == null)
                throw new ArgumentNullException(nameof(owinMiddlewareActionFilter));

            OwinActionFilter = (IOwinActionFilter)(Activator.CreateInstance(owinMiddlewareActionFilter)!);
        }

        public override bool AllowMultiple => true;

        public virtual Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            return OwinActionFilter.OnExceptionAsync(actionExecutedContext.Request.GetOwinContext(), actionExecutedContext.Exception);
        }

        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (actionContext == null)
                throw new ArgumentNullException(nameof(actionContext));

            await OwinActionFilter.OnActionExecutingAsync(actionContext.Request.GetOwinContext());
            await base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            await OwinActionFilter.OnActionExecutedAsync(actionExecutedContext.Request.GetOwinContext());
            await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}
