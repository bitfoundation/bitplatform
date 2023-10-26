using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class ConflictException : RestException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
