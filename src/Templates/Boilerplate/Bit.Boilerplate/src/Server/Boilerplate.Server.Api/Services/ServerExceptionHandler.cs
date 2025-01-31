using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Services;

public partial class ServerExceptionHandler : SharedExceptionHandler, IProblemDetailsWriter
{
    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    public bool CanWrite(ProblemDetailsContext context) => true;

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var e = context.Exception;
        var httpContext = context.HttpContext;

        // Using the Request-Id header, one can find the log for server-related exceptions
        httpContext.Response.Headers.Append(HeaderNames.RequestId, httpContext.TraceIdentifier);

        var exception = UnWrapException(e);

        if (exception is AuthenticationFailureException)
        {
            httpContext.Response.Redirect($"{Urls.SignInPage}?error={Uri.EscapeDataString(exception.Message)}");
            return;
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

        var problemDetail = new ProblemDetails
        {
            Title = message,
            Status = statusCode,
            Type = knownException?.GetType().FullName ?? typeof(UnknownException).FullName,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.GetUri().PathAndQuery}",
            Extensions = new Dictionary<string, object?>()
            {
                { "key", key },
                { "traceId", httpContext.TraceIdentifier }
            }
        };

        if (exception is ResourceValidationException validationException)
        {
            problemDetail.Extensions.Add("payload", validationException.Payload);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetail, jsonSerializerOptions.GetTypeInfo<ProblemDetails>(), cancellationToken: httpContext.RequestAborted);
    }
}
