using System.Net;
using System.Runtime.Serialization;

namespace Bit.Websites.Sales.Shared.Exceptions;

[Serializable]
public class TooManyRequestsExceptions : RestException
{
    public TooManyRequestsExceptions()
        : this(nameof(TooManyRequestsExceptions))
    {
    }

    public TooManyRequestsExceptions(string? message)
        : base(message)
    {
    }

    public TooManyRequestsExceptions(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected TooManyRequestsExceptions(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
