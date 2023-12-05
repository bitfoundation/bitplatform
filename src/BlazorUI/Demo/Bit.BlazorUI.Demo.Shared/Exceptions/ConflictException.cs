using System.Net;

namespace Bit.BlazorUI.Demo.Shared.Exceptions;

public class ConflictException : RestException
{
    public ConflictException()
        : this(nameof(ConflictException))
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ConflictException(LocalizedString message)
        : base(message)
    {
    }

    public ConflictException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
