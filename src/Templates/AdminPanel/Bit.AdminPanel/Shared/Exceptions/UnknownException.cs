using System.Runtime.Serialization;

namespace AdminPanel.Shared.Exceptions;

[Serializable]
public class UnknownException : ApplicationException
{
    public UnknownException()
        : this(nameof(UnknownException))
    {
    }

    public UnknownException(string message)
        : base(message)
    {
    }

    public UnknownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected UnknownException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}

