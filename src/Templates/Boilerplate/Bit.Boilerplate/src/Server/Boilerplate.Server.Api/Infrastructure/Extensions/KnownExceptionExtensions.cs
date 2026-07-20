namespace Boilerplate.Shared.Infrastructure.Exceptions;

public static class KnownExceptionExtensions
{
    extension<TException>(TException exception) where TException : KnownException
    {
        /// <summary>
        /// Custom properties specified here will be included in the client's response via <see cref="AppProblemDetails.Extensions"/>
        /// and logged alongside general telemetry data, including the client's IP address etc.
        /// </summary>
        public TException WithExtensionData(Dictionary<string, object?> data)
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
        public TException WithExtensionData(string key, object? value)
        {
            exception.Data["__AppProblemDetailsExtensionsData"] ??= new Dictionary<string, object?>();

            var appProblemExtensionsData = (Dictionary<string, object?>)exception.Data["__AppProblemDetailsExtensionsData"]!;

            appProblemExtensionsData[key] = value;

            return exception;
        }
    }
}
