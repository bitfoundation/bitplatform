using System.Runtime.Serialization;

namespace BlazorDual.Shared.Exceptions;

[Serializable]
public class UnknownException : ApplicationException
{
    public UnknownException()
        : base(nameof(AppStrings.UnknownException))
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

