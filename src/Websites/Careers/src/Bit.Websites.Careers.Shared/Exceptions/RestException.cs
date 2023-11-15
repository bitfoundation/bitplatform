using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class RestException : KnownException
{
    public RestException()
        : base(nameof(RestException))
    {
    }

    public RestException(string message)
        : base(message)
    {
    }

    public RestException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
}
