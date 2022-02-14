using System.Runtime.Serialization;

namespace TodoTemplate.Shared.Exceptions;

[Serializable]
public class KnownException : ApplicationException
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
