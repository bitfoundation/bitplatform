using System.Net;

namespace Bit.BlazorUI.Demo.Shared.Exceptions;

public class UnauthorizedException : RestException
{
    public UnauthorizedException()
        : base(nameof(UnauthorizedException))
    {
    }

    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public UnauthorizedException(LocalizedString message)
        : base(message)
    {
    }

    public UnauthorizedException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}
