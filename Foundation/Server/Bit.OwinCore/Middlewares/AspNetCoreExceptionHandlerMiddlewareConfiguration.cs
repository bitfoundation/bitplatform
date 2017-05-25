using System;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Metadata;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreExceptionHandlerMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            aspNetCoreApp.UseMiddleware<AspNetCoreExceptionHandlerMiddleware>();
        }

        public virtual RegisterKind GetRegisterKind()
        {
            return RegisterKind.BeforeOwinPiepline;
        }
    }

    public class AspNetCoreExceptionHandlerMiddleware
    {
        private readonly RequestDelegate Next;

        public AspNetCoreExceptionHandlerMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            IScopeStatusManager scopeStatusManager = context.RequestServices.GetService<IScopeStatusManager>();

            ILogger logger = context.RequestServices.GetService<ILogger>();

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

                    if (responseStatusCodeIsErrorCodeBecauseOfSomeClientBasedReason || context.Features.Get<IHttpResponseFeature>().ReasonPhrase == FoundationMetadataBuilder.KnownError)
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
                    IExceptionToHttpErrorMapper exceptionToHttpErrorMapper = context.RequestServices.GetService<IExceptionToHttpErrorMapper>();
                    context.Response.StatusCode = Convert.ToInt32(exceptionToHttpErrorMapper.GetStatusCode(exp));
                    await context.Response.WriteAsync(exceptionToHttpErrorMapper.GetMessage(exp) , context.RequestAborted);
                    context.Features.Get<IHttpResponseFeature>().ReasonPhrase = exceptionToHttpErrorMapper.GetReasonPhrase(exp);
                }
                throw;
            }
        }
    }
}
