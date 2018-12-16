using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Bit.WebApi.ActionFilters
{
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            IDependencyResolver scopeDependencyResolver = actionExecutedContext.Request.GetOwinContext().GetDependencyResolver();

            IScopeStatusManager scopeStatusManager = scopeDependencyResolver.Resolve<IScopeStatusManager>();
            IExceptionToHttpErrorMapper exceptionToHttpErrorMapper = scopeDependencyResolver.Resolve<IExceptionToHttpErrorMapper>();

            Exception exception = actionExecutedContext.Exception;
            actionExecutedContext.Response = CreateErrorResponseMessage(actionExecutedContext, exceptionToHttpErrorMapper, exception);

            string reasonPhrase = exceptionToHttpErrorMapper.GetReasonPhrase(exception);
            actionExecutedContext.Response.ReasonPhrase = reasonPhrase;
            if (!actionExecutedContext.Response.Headers.Contains("Reason-Phrase"))
                actionExecutedContext.Response.Headers.Add("Reason-Phrase", reasonPhrase);

            if (scopeStatusManager.WasSucceeded())
                scopeStatusManager.MarkAsFailed(exception.Message);

            return base.OnExceptionAsync(actionExecutedContext, cancellationToken);
        }

        protected virtual HttpResponseMessage CreateErrorResponseMessage(HttpActionExecutedContext actionExecutedContext, IExceptionToHttpErrorMapper exceptionToHttpErrorMapper, Exception exception)
        {
            return actionExecutedContext.Request.CreateErrorResponse(exceptionToHttpErrorMapper.GetStatusCode(exception), new HttpError() { Message = exceptionToHttpErrorMapper.GetMessage(exception) });
        }
    }
}
