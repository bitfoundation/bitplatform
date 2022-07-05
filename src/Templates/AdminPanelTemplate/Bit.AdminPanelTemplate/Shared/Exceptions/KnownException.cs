using System.Runtime.Serialization;

namespace AdminPanelTemplate.Shared.Exceptions;

[Serializable]
public abstract class KnownException : ApplicationException
{
    public KnownException()
        : this(nameof(KnownException))
    {
    }

    public KnownException(string? message)
        : base(message)
    {
    }

    public KnownException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected KnownException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
