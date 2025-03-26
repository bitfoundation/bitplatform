using System.Net;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Services;

public partial class ServerExceptionHandler : SharedExceptionHandler, IProblemDetailsWriter
{
    [AutoInject] private IHostEnvironment env = default!;
    [AutoInject] private ILogger<ServerExceptionHandler> logger = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    private static readonly Guid appSessionId = Guid.NewGuid();

    public bool CanWrite(ProblemDetailsContext context) => true;

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;

        // Using the Request-Id header, one can find the log for server-related exceptions
        httpContext.Response.Headers.Append(HeaderNames.RequestId, httpContext.TraceIdentifier);

        if (context.Exception is null)
            return;

        var exception = UnWrapException(context.Exception);

        Handle(exception, null, httpContext, out var statusCode, out var problemDetail);
        httpContext.Response.StatusCode = statusCode;

        if (exception is AuthenticationFailureException)
        {
            httpContext.Response.Redirect($"{Urls.SignInPage}?error={Uri.EscapeDataString(exception.Message)}");
            return;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetail!, jsonSerializerOptions.GetTypeInfo<ProblemDetails>(), cancellationToken: httpContext.RequestAborted);
    }

    private void Handle(Exception exception,
        Dictionary<string, object?>? parameters,
        HttpContext? httpContext,
        out int statusCode,
        out ProblemDetails? problemDetails)
    {
        var data = new Dictionary<string, object?>()
        {
            { "ActivityId", Activity.Current?.Id },
            { "ParentActivityId", Activity.Current?.ParentId },
            { "ServerAppSessionId", appSessionId },
            { "AppVersion", typeof(ServerExceptionHandler).Assembly.GetName().Version },
            { "Culture", CultureInfo.CurrentUICulture.Name },
            { "Environment", env.EnvironmentName },
            { "ServerDateTime", DateTimeOffset.UtcNow.ToString("u") },
        };

        string? instance = null;
        string? traceIdentifier = null;

        try
        {
            if (httpContext is not null)
            {
                traceIdentifier = httpContext.TraceIdentifier;
                instance = $"{httpContext.Request.Method} {httpContext.Request.GetUri().PathAndQuery}";

                data["Instance"] = instance;
                data["RequestId"] = httpContext.TraceIdentifier;
                data["UserId"] = httpContext.User.IsAuthenticated() ? httpContext.User.GetUserId() : null;
                data["UserSessionId"] = httpContext.User.IsAuthenticated() ? httpContext.User.GetSessionId() : null;
                data["ClientIP"] = httpContext.Connection.RemoteIpAddress;
            }
        }
        catch (ObjectDisposedException) { /* The HttpContext from IHttpContextAccessor may be disposed at any time if the exception is handled within Task.Run or similar situations. */ }

        var knownException = exception as KnownException;

        statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

        // The details of all of the exceptions are returned only in dev mode. in any other modes like production, only the details of the known exceptions are returned.
        var message = GetExceptionMessageToShow(exception);
        var exceptionKey = knownException?.Key ?? nameof(UnknownException);

        foreach (var item in GetExceptionData(exception))
        {
            if (item.Value is Dictionary<string, object?> appProblemExtensionsData)
            {
                foreach (var innerDataItem in appProblemExtensionsData)
                {
                    data[innerDataItem.Key] = innerDataItem.Value;
                }

                continue;
            }

            data[item.Key] = item.Value;
        }

        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                data[parameter.Key] = parameter.Value;
            }
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

        if (instance is null || traceIdentifier is null)
        {
            problemDetails = null;
            return;
        }

        if (exception is KnownException && message == exceptionKey)
        {
            message = Localizer[message];
        }

        problemDetails = new ProblemDetails
        {
            Title = message,
            Status = statusCode,
            Type = knownException?.GetType().FullName ?? typeof(UnknownException).FullName,
            Instance = instance,
            Extensions = new Dictionary<string, object?>()
            {
                { "key", exceptionKey },
                { "traceId", traceIdentifier }
            }
        };

        if (exception.Data["__AppProblemDetailsExtensionsData"] is Dictionary<string, object?> errorExtensions)
        {
            foreach (var item in errorExtensions)
            {
                problemDetails.Extensions[item.Key] = item.Value;
            }
        }

        if (exception is ResourceValidationException validationException)
        {
            problemDetails.Extensions.Add("payload", validationException.Payload);
        }
    }

    public void Handle(Exception exp,
        Dictionary<string, object?>? parameters = null)
    {
        Handle(UnWrapException(exp), parameters, httpContextAccessor.HttpContext, out var _, out var _);
    }
}
