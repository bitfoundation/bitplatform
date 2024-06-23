using System.Net;

namespace Bit.Websites.Sales.Shared.Exceptions;

public class TooManyRequestsExceptions : RestException
{
    public TooManyRequestsExceptions()
        : base(nameof(TooManyRequestsExceptions))
    {
    }

    public TooManyRequestsExceptions(string message)
        : base(message)
    {
    }

    public TooManyRequestsExceptions(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
