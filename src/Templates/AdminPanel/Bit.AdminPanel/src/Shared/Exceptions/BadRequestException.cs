using System.Net;
using System.Runtime.Serialization;

namespace AdminPanel.Shared.Exceptions;

[Serializable]
public class BadRequestException : RestException
{
    public BadRequestException()
        : base(nameof(AppStrings.BadRequestException))
    {
    }

    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public BadRequestException(LocalizedString message)
        : base(message)
    {
    }

    public BadRequestException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected BadRequestException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
