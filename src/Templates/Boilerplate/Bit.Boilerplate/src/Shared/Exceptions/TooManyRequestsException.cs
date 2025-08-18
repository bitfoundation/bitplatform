using System.Net;

namespace Boilerplate.Shared.Exceptions;

public partial class TooManyRequestsException : RestException
{
    public TooManyRequestsException()
        : base(nameof(AppStrings.TooManyRequestExceptions))
    {
    }

    public TooManyRequestsException(string message)
        : base(message)
    {
    }

    public TooManyRequestsException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public TooManyRequestsException(LocalizedString message)
        : base(message)
    {
    }

    public TooManyRequestsException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
