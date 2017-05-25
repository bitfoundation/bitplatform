using System;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Metadata;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class OwinExceptionHandlerMiddleware : OwinMiddleware
    {
        public OwinExceptionHandlerMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IScopeStatusManager scopeStatusManager = dependencyResolver.Resolve<IScopeStatusManager>();

            ILogger logger = dependencyResolver.Resolve<ILogger>();

            try
            {
                await Next.Invoke(context);
                string statusCode = context.Response.StatusCode.ToString();
                bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason = statusCode.StartsWith("5");
                bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason = statusCode.StartsWith("4");
                if (responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason ||
                    responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason)
                {
                    scopeStatusManager.MarkAsFailed();

                    logger.AddLogData("ResponseStatusCode", statusCode);
                    logger.AddLogData("ResponseReasonPhrase", context.Response.ReasonPhrase);

                    if (responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason || context.Response.ReasonPhrase == FoundationMetadataBuilder.KnownError)
                    {
                        await logger.LogWarningAsync("Response has failed status code because of some client side reason");
                    }
                    else if (responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason)
                    {
                        await logger.LogFatalAsync("Response has failed status code because of some server side reason");
                    }
                }
                else if (!scopeStatusManager.WasSucceeded())
                {
                    await logger.LogFatalAsync("Scope was failed");
                }
                else
                {
                    scopeStatusManager.MarkAsSucceeded();
                }
            }
            catch (Exception exp)
            {
                if (scopeStatusManager.WasSucceeded())
                    scopeStatusManager.MarkAsFailed();
                await logger.LogExceptionAsync(exp, "Request-Execution-Exception");
                string statusCode = context.Response.StatusCode.ToString();
                bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason = statusCode.StartsWith("5");
                bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason = statusCode.StartsWith("4");
                if (responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason == false && responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason == false)
                {
                    IExceptionToHttpErrorMapper exceptionToHttpErrorMapper = dependencyResolver.Resolve<IExceptionToHttpErrorMapper>();
                    context.Response.StatusCode = Convert.ToInt32(exceptionToHttpErrorMapper.GetStatusCode(exp));
                    await context.Response.WriteAsync(exceptionToHttpErrorMapper.GetMessage(exp), context.Request.CallCancelled);
                    context.Response.ReasonPhrase = exceptionToHttpErrorMapper.GetReasonPhrase(exp);
                }
                throw;
            }
        }
    }
}