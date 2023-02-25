using System.Net;
using System.Runtime.Serialization;

namespace BlazorDual.Shared.Exceptions;

[Serializable]
public class TooManyRequestsExceptions : RestException
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

    protected TooManyRequestsExceptions(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
