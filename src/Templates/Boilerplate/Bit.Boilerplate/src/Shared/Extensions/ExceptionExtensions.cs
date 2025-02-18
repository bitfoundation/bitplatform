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
            exception.Data.Add(item.Key, item.Value);
        }

        return exception;
    }
}
