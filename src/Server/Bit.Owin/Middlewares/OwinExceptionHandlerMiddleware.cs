using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Metadata;
using Microsoft.Owin;
using System;
using System.Globalization;
using System.Threading.Tasks;

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
                context.Response.OnSendingHeaders((state) =>
                {
                    // Create a backup header for reason phrase.
                    string reasonPhrase = context.Response.ReasonPhrase;
                    if (!string.IsNullOrEmpty(reasonPhrase) && !context.Response.Headers.ContainsKey("Reason-Phrase"))
                        context.Response.Headers.Add("Reason-Phrase", new[] { reasonPhrase });
                }, state: null);

                await Next.Invoke(context);

                string statusCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
                bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason = statusCode.StartsWith("5", StringComparison.InvariantCultureIgnoreCase);
                bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason = statusCode.StartsWith("4", StringComparison.InvariantCultureIgnoreCase);
                if (responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason ||
                    responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason)
                {
                    string reasonPhrase = context.Response.ReasonPhrase ?? "UnknownReasonPhrase";

                    scopeStatusManager.MarkAsFailed(reasonPhrase);

                    logger.AddLogData("ResponseStatusCode", statusCode);
                    logger.AddLogData("ResponseReasonPhrase", reasonPhrase);

                    if (responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason || reasonPhrase == BitMetadataBuilder.KnownError)
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
                    await logger.LogFatalAsync($"Scope was failed: {scopeStatusManager.FailureReason}");
                }
                else
                {
                    scopeStatusManager.MarkAsSucceeded();
                }
            }
            catch (Exception exp)
            {
                if (scopeStatusManager.WasSucceeded())
                    scopeStatusManager.MarkAsFailed(exp.Message);
                await logger.LogExceptionAsync(exp, "Request-Execution-Exception");
                string statusCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
                bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason = statusCode.StartsWith("5", StringComparison.InvariantCultureIgnoreCase);
                bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason = statusCode.StartsWith("4", StringComparison.InvariantCultureIgnoreCase);
                if (responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason == false && responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason == false)
                {
                    IExceptionToHttpErrorMapper exceptionToHttpErrorMapper = dependencyResolver.Resolve<IExceptionToHttpErrorMapper>();
                    context.Response.StatusCode = Convert.ToInt32(exceptionToHttpErrorMapper.GetStatusCode(exp), CultureInfo.InvariantCulture);
                    context.Response.ReasonPhrase = exceptionToHttpErrorMapper.GetReasonPhrase(exp);
                    await context.Response.WriteAsync(exceptionToHttpErrorMapper.GetMessage(exp), context.Request.CallCancelled);
                }
                throw;
            }
        }
    }
}