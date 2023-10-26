using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class ForbiddenException : RestException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}
