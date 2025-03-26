using System.Net;
using Boilerplate.Client.Core.Services;
using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Server.Web.Services;

public partial class WebServerExceptionHandler : ClientExceptionHandlerBase
{
    [AutoInject] IHttpContextAccessor httpContextAccessor = default!;

    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        if (httpContextAccessor.HttpContext is not null && httpContextAccessor.HttpContext.Response.HasStarted is false)
        {
            // This method is invoked for exceptions occurring in Blazor Server and during pre-rendering.
            // While setting a status code is not meaningful in Blazor Server or streaming pre-rendering 
            // (since the response has already started), we can still set response codes for non-streaming pre-rendering scenarios.  
            // A key factor that disables streaming during pre-rendering is response caching (see AppResponseCachePolicy.cs's AppResponseCachePolicy__DisableStreamPrerendering).  
            // By setting the status code here, we ensure that a faulted response is not cached.
            var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);
            httpContextAccessor.HttpContext.Response.StatusCode = statusCode;
        }

        base.Handle(exception, displayKind, parameters);
    }
}
