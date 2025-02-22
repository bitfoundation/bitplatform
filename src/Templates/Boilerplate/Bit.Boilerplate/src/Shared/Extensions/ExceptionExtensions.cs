namespace System;

public static class ExceptionExtensions
{
    /// <summary>
    /// Any custom properties specified here will be recorded along with general telemetry data, including the client's IP address.
    /// </summary>
    public static TException WithData<TException>(this TException exception, Dictionary<string, object?> data)
        where TException : Exception
    {
        foreach (var item in data)
        {
            exception.Data[item.Key] = item.Value;
        }

        return exception;
    }

    /// <summary>
    /// <inheritdoc cref="WithData{TException}(TException, Dictionary{string, object?})"/>
    /// </summary>
    public static TException WithData<TException>(this TException exception, string key, object? value)
        where TException : Exception
    {
        exception.Data[key] = value;

        return exception;
    }
}
