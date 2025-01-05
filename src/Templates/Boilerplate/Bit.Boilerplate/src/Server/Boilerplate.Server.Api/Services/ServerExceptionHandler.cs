using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Services;

public partial class ServerExceptionHandler : SharedExceptionHandler, IExceptionHandler
{
    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception e, CancellationToken cancellationToken)
    {
        // Using the Request-Id header, one can find the log for server-related exceptions
        httpContext.Response.Headers.Append(HeaderNames.RequestId, httpContext.TraceIdentifier);

        var exception = UnWrapException(e);

        if (exception is AuthenticationFailureException)
        {
            httpContext.Response.Redirect($"{Urls.SignInPage}?error={Uri.EscapeDataString(exception.Message)}");

            return true;
        }

        var knownException = exception as KnownException;

        // The details of all of the exceptions are returned only in dev mode. in any other modes like production, only the details of the known exceptions are returned.
        var key = knownException?.Key ?? nameof(UnknownException);
        var message = GetExceptionMessageToShow(exception);

        var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

        if (exception is KnownException && message == key)
        {
            message = Localizer[message];
        }

        var restExceptionPayload = new RestErrorInfo
        {
            Key = key,
            Message = message,
            ExceptionType = knownException?.GetType().FullName ?? typeof(UnknownException).FullName
        };

        if (exception is ResourceValidationException validationException)
        {
            restExceptionPayload.Payload = validationException.Payload;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(restExceptionPayload, jsonSerializerOptions.GetTypeInfo<RestErrorInfo>(), cancellationToken: cancellationToken);

        return true;
    }
}
