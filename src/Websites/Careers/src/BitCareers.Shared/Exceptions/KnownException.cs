using System.Runtime.Serialization;

namespace BitCareers.Shared.Exceptions;

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

    protected KnownException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public string? Key { get; set; }
}
