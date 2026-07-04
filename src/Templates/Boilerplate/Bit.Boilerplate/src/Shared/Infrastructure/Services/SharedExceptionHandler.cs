using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Boilerplate.Shared.Infrastructure.Services;

public partial class SharedExceptionHandler
{
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer { get; set; } = default!;

    protected string GetExceptionMessageToShow(Exception exception)
    {
        if (exception is KnownException)
            return exception.Message;

        if (AppEnvironment.IsDevelopment())
            return exception.ToString();

        return Localizer[nameof(AppStrings.UnknownException)];
    }

    protected string GetExceptionMessageToLog(Exception exception)
    {
        var exceptionMessageToLog = exception.Message;

        if (exception.InnerException is not null)
            exceptionMessageToLog += $"{Environment.NewLine}{GetExceptionMessageToLog(exception.InnerException)}";

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                exceptionMessageToLog += $"{Environment.NewLine}{GetExceptionMessageToLog(innerException)}";
            }
        }

        return exceptionMessageToLog;
    }

    public Exception UnWrapException(Exception exception)
    {
        if (exception is TargetInvocationException)
        {
            return exception.InnerException ?? exception;
        }

        return exception;
    }

    public virtual bool IgnoreException(Exception exception)
    {
        // Ignoring exception here will prevent it from being logged in both client and server.

        if (exception is ClientNotSupportedException)
            return true; // Example of an exception that we might want to ignore.

        if (exception.InnerException is not null && IgnoreException(exception.InnerException))
            return true;

        if (exception is AggregateException aggExp)
        {
            foreach (var innerException in aggExp.InnerExceptions)
            {
                if (IgnoreException(innerException))
                    return true;
            }
        }

        return false;
    }

    protected virtual IDictionary<string, object?> GetExceptionData(Exception exp)
    {
        var data = new Dictionary<string, object?>();

        foreach (var item in exp.Data.Keys.Cast<string>()
            .Zip(exp.Data.Values.Cast<object?>())
            .ToDictionary(item => item.First, item => item.Second))
        {
            if (item.Value is Dictionary<string, object?> innerData) // ProblemDetails is a Dictionary<string, object?>, so we need to flatten it into the main data dictionary
            {
                foreach (var innerItem in innerData)
                {
                    data[innerItem.Key] = innerItem.Value;
                }
            }
            else
            {
                data[item.Key] = item.Value;
            }
        }

        if (exp.InnerException is not null)
        {
            var innerData = GetExceptionData(exp.InnerException);

            foreach (var innerDataItem in innerData)
            {
                data[innerDataItem.Key] = innerDataItem.Value;
            }
        }

        if (exp is AggregateException aggExp && aggExp.InnerExceptions.Any())
        {
            foreach (var innerException in aggExp.InnerExceptions)
            {
                var innerData = GetExceptionData(innerException);

                foreach (var innerDataItem in innerData)
                {
                    data[innerDataItem.Key] = innerDataItem.Value;
                }
            }
        }

        if (exp is ResourceValidationException resValExp)
        {
            foreach (var detail in resValExp.Payload.Details)
            {
                foreach (var error in detail.Errors)
                {
                    data[$"{detail.Name}:{error.Key}"] = error.Message;
                }
            }
        }

        if (exp is KnownException)
        {
            data["KnownException"] = true;
        }

        if (IsTransientException(exp))
        {
            data["TransientException"] = true;
        }

        data["ExceptionId"] = Guid.CreateVersion7(); // This will remain consistent across different registered loggers, such as Sentry, Application Insights, etc.

        return data;
    }

    public virtual bool IsTransientException(Exception? exp)
    {
        return (exp is TimeoutException)
             || (exp is WebException webExp && webExp.WithData("Status", webExp.Status).Status is WebExceptionStatus.ConnectFailure)
             || (exp?.InnerException is not null && IsTransientException(exp.InnerException))
             || (exp is HttpIOException httpIOExp && httpIOExp.WithData("HttpRequestError", httpIOExp.HttpRequestError).HttpRequestError is not HttpRequestError.UserAuthenticationError)
             || (exp is AggregateException aggExp && aggExp.InnerExceptions.Any(IsTransientException))
             || (exp is SocketException sockExp && sockExp.WithData("SocketErrorCode", sockExp.SocketErrorCode).SocketErrorCode is SocketError.HostNotFound or SocketError.HostUnreachable or SocketError.HostDown or SocketError.TimedOut)
             || (exp is HttpRequestException reqExp && reqExp.WithData("StatusCode", reqExp.StatusCode).StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout)
             || (exp is HttpProtocolException proExp && proExp.WithData("HttpRequestError", proExp.HttpRequestError).WithData("ErrorCode", proExp.ErrorCode).HttpRequestError is not HttpRequestError.UserAuthenticationError);
    }
}
