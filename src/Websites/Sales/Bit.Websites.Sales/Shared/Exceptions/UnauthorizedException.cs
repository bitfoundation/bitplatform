using System.Net;
using System.Runtime.Serialization;

namespace Bit.Websites.Sales.Shared.Exceptions;

[Serializable]
public class UnauthorizedException : RestException
{
    public UnauthorizedException()
        : this(nameof(UnauthorizedException))
    {
    }

    public UnauthorizedException(string? message)
        : base(message)
    {
    }

    public UnauthorizedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected UnauthorizedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}
