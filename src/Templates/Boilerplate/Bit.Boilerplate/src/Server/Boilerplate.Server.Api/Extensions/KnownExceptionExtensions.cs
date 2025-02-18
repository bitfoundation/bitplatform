namespace Boilerplate.Shared.Exceptions;

public static class KnownExceptionExtensions
{
    /// <summary>
    /// Custom properties specified here will be included in the client's response via <see cref="AppProblemDetails.Extensions"/>  
    /// and logged alongside general telemetry data, including the client's IP address etc.
    /// </summary>
    public static TException WithExtensionData<TException>(this TException exception, Dictionary<string, object?> data)
        where TException : KnownException
    {
        foreach (var item in data)
        {
            exception.WithExtensionData(item.Key, item.Value);
        }

        return exception;
    }

    /// <summary>
    /// <inheritdoc cref="WithExtensionData{TException}(TException, Dictionary{string, object?})"/>
    /// </summary>
    public static TException WithExtensionData<TException>(this TException exception, string key, object? value)
        where TException : KnownException
    {
        exception.Data["__AppProblemDetailsExtensionsData"] ??= new Dictionary<string, object?>();

        var appProblemExtensionsData = (Dictionary<string, object?>)exception.Data["__AppProblemDetailsExtensionsData"]!;

        appProblemExtensionsData[key] = value;

        return exception;
    }
}
