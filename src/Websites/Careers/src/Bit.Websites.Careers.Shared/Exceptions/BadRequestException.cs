using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class BadRequestException : RestException
{
    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
