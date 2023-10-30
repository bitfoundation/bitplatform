using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class UnauthorizedException : RestException
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}
