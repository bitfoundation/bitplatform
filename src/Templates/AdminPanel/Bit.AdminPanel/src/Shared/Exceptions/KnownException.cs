using System.Runtime.Serialization;

namespace AdminPanel.Shared.Exceptions;

[Serializable]
public abstract class KnownException : ApplicationException
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

    protected KnownException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public string? Key { get; set; }
}
