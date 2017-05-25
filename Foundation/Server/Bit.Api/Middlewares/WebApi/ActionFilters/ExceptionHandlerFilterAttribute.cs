using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.OData.Extensions;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.OData;
using Microsoft.Owin;

namespace Bit.Api.Middlewares.WebApi.ActionFilters
{
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override async Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            IDependencyResolver scopeDependencyResolver = actionExecutedContext.Request.GetOwinContext().GetDependencyResolver();

            IScopeStatusManager scopeStatusManager = scopeDependencyResolver.Resolve<IScopeStatusManager>();
            IExceptionToHttpErrorMapper exceptionToHttpErrorMapper = scopeDependencyResolver.Resolve<IExceptionToHttpErrorMapper>();

            Exception exception = actionExecutedContext.Exception;

            actionExecutedContext.Response =
                actionExecutedContext.Request.CreateErrorResponse(exceptionToHttpErrorMapper.GetStatusCode(exception), new ODataError() { Message = exceptionToHttpErrorMapper.GetMessage(exception) });

            actionExecutedContext.Response.ReasonPhrase = exceptionToHttpErrorMapper.GetReasonPhrase(exception);

            if (scopeStatusManager.WasSucceeded())
                scopeStatusManager.MarkAsFailed();

            await base.OnExceptionAsync(actionExecutedContext, cancellationToken);
        }
    }
}
