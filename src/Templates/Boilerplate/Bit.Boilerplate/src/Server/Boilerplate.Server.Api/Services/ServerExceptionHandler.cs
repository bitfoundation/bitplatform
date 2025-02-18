using System.Net;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Services;

public partial class ServerExceptionHandler : SharedExceptionHandler, IProblemDetailsWriter
{
    [AutoInject] private IHostEnvironment env = default!;
    [AutoInject] private ILogger<ServerExceptionHandler> logger = default!;
    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    private static readonly Guid appSessionId = Guid.NewGuid();

    public bool CanWrite(ProblemDetailsContext context) => context.Exception is not null;

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var e = context.Exception!;

        var httpContext = context.HttpContext;

        // Using the Request-Id header, one can find the log for server-related exceptions
        httpContext.Response.Headers.Append(HeaderNames.RequestId, httpContext.TraceIdentifier);

        var exception = UnWrapException(e);

        var knownException = exception as KnownException;

        // The details of all of the exceptions are returned only in dev mode. in any other modes like production, only the details of the known exceptions are returned.
        var message = GetExceptionMessageToShow(exception);
        var exceptionKey = knownException?.Key ?? nameof(UnknownException);

        var data = new Dictionary<string, object?>()
        {
            { "RequestId", httpContext.TraceIdentifier },
            { "ActivityId", Activity.Current?.Id },
            { "ParentActivityId", Activity.Current?.ParentId },
            { "UserId", httpContext.User.IsAuthenticated() ? httpContext.User.GetUserId() : null },
            { "UserSessionId", httpContext.User.IsAuthenticated() ? httpContext.User.GetSessionId() : null },
            { "AppSessionId", appSessionId },
            { "AppVersion", typeof(ServerExceptionHandler).Assembly.GetName().Version },
            { "Culture", CultureInfo.CurrentUICulture.Name },
            { "Environment", env.EnvironmentName },
            { "ServerDateTime", DateTimeOffset.UtcNow.ToString("u") },
            { "ClientIP", httpContext.Connection.RemoteIpAddress }
        };

        foreach (var key in exception.Data.Keys)
        {
            var keyAsString = key.ToString()!;

            var value = exception.Data[keyAsString]!;

            if (keyAsString == "AppProblemExtensions" && value is Dictionary<string,object?> appProblemExtensionsData)
            {
                foreach (var innerDataItem in appProblemExtensionsData)
                {
                    data[innerDataItem.Key] = innerDataItem.Value;
                }

                continue;
            }

            data[keyAsString] = value;
        }

        using (var scope = logger.BeginScope(data))
        {
            var exceptionMessageToLog = GetExceptionMessageToLog(exception);

            if (exception is KnownException)
            {
                logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                logger.LogCritical(exception, exceptionMessageToLog);
            }
        }

        if (exception is AuthenticationFailureException)
        {
            httpContext.Response.Redirect($"{Urls.SignInPage}?error={Uri.EscapeDataString(exception.Message)}");
            return;
        }

        var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

        if (exception is KnownException && message == exceptionKey)
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
                { "key", exceptionKey },
                { "traceId", httpContext.TraceIdentifier }
            }
        };

        if (exception.Data["AppProblemExtensions"] is Dictionary<string, object?> errorExtensions)
        {
            foreach (var item in errorExtensions)
            {
                problemDetail.Extensions[item.Key] = item.Value;
            }
        }

        if (exception is ResourceValidationException validationException)
        {
            problemDetail.Extensions.Add("payload", validationException.Payload);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetail, jsonSerializerOptions.GetTypeInfo<ProblemDetails>(), cancellationToken: httpContext.RequestAborted);
    }
}
