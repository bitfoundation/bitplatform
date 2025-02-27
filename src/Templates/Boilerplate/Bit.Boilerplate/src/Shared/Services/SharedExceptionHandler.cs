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

    protected Exception UnWrapException(Exception exception)
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

    protected bool IgnoreException(Exception exception)
    {
        if (exception is KnownException)
            return false;

        return exception is TaskCanceledException ||
            exception is OperationCanceledException ||
            exception is TimeoutException ||
            (exception.InnerException is not null && IgnoreException(exception.InnerException));
    }
}
