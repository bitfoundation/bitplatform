namespace System;

public static class ExceptionExtensions
{
    extension<TException>(TException exception)
        where TException : Exception
    {
        /// <summary>
        /// Any custom properties specified here will be recorded along with general telemetry data, including the client's IP address.
        /// </summary>
        public TException WithData(Dictionary<string, object?> data)
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
        public TException WithData(string key, object? value)
        {
            exception.Data[key] = value;

            return exception;
        }
    }
}
