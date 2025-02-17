namespace System;

public static class ExceptionExtensions
{
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
