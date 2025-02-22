namespace Boilerplate.Shared.Exceptions;

public abstract partial class KnownException : Exception
{
    public KnownException(string message)
        : base(message)
    {
        Key = message;
    }

    public KnownException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Key = message;
    }

    public KnownException(LocalizedString message)
        : base(message.Value)
    {
        Key = message.Name;
    }

    public KnownException(LocalizedString message, Exception? innerException)
        : base(message.Value, innerException)
    {
        Key = message.Name;
    }

    public string? Key { get; set; }

    /// <summary>
    /// Read KnownExceptionExtensions.WithExtensionData comments.
    /// </summary>
    public bool TryGetExtensionDataValue<T>(string key, out T value)
    {
        value = default!;

        if (Data[key] is object valueObj)
        {
            if (valueObj is T)
            {
                value = (T)valueObj;
            }
            else if (valueObj is JsonElement jsonElement)
            {
                value = jsonElement.Deserialize(AppJsonContext.Default.Options.GetTypeInfo<T>())!;
            }
            return true;
        }

        return false;
    }
}
