using System.Reflection;

namespace Boilerplate.Shared.Services;

public partial class SharedExceptionHandler
{
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer { get; set; } = default!;

    protected string GetExceptionMessageToShow(Exception exception)
    {
        if (exception is KnownException)
            return exception.Message;

        if (AppEnvironment.IsDev())
            return exception.ToString();

        return Localizer[nameof(AppStrings.UnknownException)];
    }

    protected string GetExceptionMessageToLog(Exception exception)
    {
        var exceptionMessageToLog = exception.Message;
        var innerException = exception.InnerException;

        while (innerException is not null)
        {
            exceptionMessageToLog += $"{Environment.NewLine}{innerException.Message}";
            innerException = innerException.InnerException;
        }

        return exceptionMessageToLog;
    }

    public Exception UnWrapException(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerException ?? aggregateException;
        }
        else if (exception is TargetInvocationException)
        {
            return exception.InnerException ?? exception;
        }

        return exception;
    }

    public virtual bool IgnoreException(Exception exception)
    {
        // Ignoring exception here will prevent it from being logged in both client and server.

        if (exception is ClientNotSupportedException)
            return true; // See ExceptionDelegatingHandler

        if (exception is KnownException)
            return false;

        return exception.InnerException is not null && IgnoreException(exception.InnerException);
    }

    protected IDictionary<string, object?> GetExceptionData(Exception exp)
    {
        var data = exp.Data.Keys.Cast<string>()
            .Zip(exp.Data.Values.Cast<object?>())
            .ToDictionary(item => item.First, item => item.Second);

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

        if (exp.InnerException is not null)
        {
            var innerData = GetExceptionData(exp.InnerException);

            foreach (var innerDataItem in innerData)
            {
                data[innerDataItem.Key] = innerDataItem.Value;
            }
        }

        return data;
    }
}
