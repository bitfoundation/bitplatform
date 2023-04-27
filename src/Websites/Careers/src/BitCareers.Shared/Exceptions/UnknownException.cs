using System.Runtime.Serialization;

namespace BitCareers.Shared.Exceptions;

[Serializable]
public class UnknownException : ApplicationException
{
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

