using System.Net;
using System.Runtime.Serialization;
using Microsoft.Extensions.Localization;

namespace AdminPanel.Shared.Exceptions;

[Serializable]
public class UnauthorizedException : RestException
{
    public UnauthorizedException()
        : base(nameof(AppStrings.UnauthorizedException))
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

    protected UnauthorizedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}
