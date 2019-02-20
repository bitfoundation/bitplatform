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

        private (bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason, bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason, bool responseIsOk) GetResponseStatus(IOwinContext context)
        {
            string statusCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
            bool responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason = statusCode.StartsWith("5", StringComparison.InvariantCultureIgnoreCase);
            bool responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason = statusCode.StartsWith("4", StringComparison.InvariantCultureIgnoreCase);
            return (responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason, responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason, responseIsOk: !responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason && !responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason);
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
                    // Create a backup header for reason phrase + enhance its value if possible.
                    // We're able to modify reason phrase and other headers in this callback and in catch part of this class only.
                    // We may not change response headers after await Next.Invoke(context); Codes after Next.Invoke gets invoked after OnSendingHeaders.
                    string reasonPhrase = context.Response.ReasonPhrase;
                    // It can gets filled by
                    // 1- WebApi's routing, when no routing gets matched with value "Not Found".
                    // 2- In catch part of this class with either "KnownError" & "UnknownError" values.
                    // 3- In ExceptionHandlerFilterAttribute class with either "KnownError" & "UnknownError" values.
                    // 4- Other codes! It might be empty too!

                    bool responseIsOk = GetResponseStatus(context).responseIsOk;

                    if (!responseIsOk)
                    {
                        if (string.IsNullOrEmpty(reasonPhrase))
                            reasonPhrase = BitMetadataBuilder.UnknownError;
                        else if (!string.Equals(reasonPhrase, BitMetadataBuilder.KnownError, StringComparison.CurrentCultureIgnoreCase) && !string.Equals(reasonPhrase, BitMetadataBuilder.UnknownError, StringComparison.CurrentCultureIgnoreCase))
                            reasonPhrase = $"{BitMetadataBuilder.UnknownError}:{reasonPhrase}";
                    }
                    if (!responseIsOk)
                    {
                        context.Response.ReasonPhrase = reasonPhrase;
                        if (!context.Response.Headers.ContainsKey("Reason-Phrase"))
                        {
                            context.Response.Headers.Add("Reason-Phrase", new[] { reasonPhrase });
                        }
                    }
                }, state: null);

                await Next.Invoke(context);

                var status = GetResponseStatus(context);
                if (status.responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason ||
                    status.responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason)
                {
                    string reasonPhrase = context.Response.ReasonPhrase;

                    scopeStatusManager.MarkAsFailed(reasonPhrase);

                    logger.AddLogData("ResponseStatusCode", context.Response.StatusCode);
                    logger.AddLogData("ResponseReasonPhrase", reasonPhrase);

                    if (status.responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason || reasonPhrase == BitMetadataBuilder.KnownError)
                    {
                        await logger.LogWarningAsync("Response has failed status code because of some client side reason");
                    }
                    else if (status.responseStatusCodeIsErrorCodeBecauseOfSomeServerBasedReason)
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