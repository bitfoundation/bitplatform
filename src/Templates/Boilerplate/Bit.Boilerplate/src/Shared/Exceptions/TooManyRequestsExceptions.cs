using System.Net;

namespace Boilerplate.Shared.Exceptions;

public partial class TooManyRequestsExceptions : RestException
{
    public TooManyRequestsExceptions()
        : base(nameof(AppStrings.TooManyRequestsExceptions))
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

    public TooManyRequestsExceptions(LocalizedString message)
        : base(message)
    {
    }

    public TooManyRequestsExceptions(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
