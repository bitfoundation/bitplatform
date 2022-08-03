using System.Net;
using System.Runtime.Serialization;

namespace Bit.Websites.Sales.Shared.Exceptions;

[Serializable]
public class ConflictException : RestException
{
    public ConflictException()
        : this(nameof(ConflictException))
    {
    }

    public ConflictException(string? message)
        : base(message)
    {
    }

    public ConflictException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ConflictException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
