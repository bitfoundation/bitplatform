using System.Net;
using System.Runtime.Serialization;

namespace AdminPanel.Shared.Exceptions;

[Serializable]
public class BadRequestException : RestException
{
    public BadRequestException()
        : this(nameof(BadRequestException))
    {
    }

    public BadRequestException(string? message)
        : base(message)
    {
    }

    public BadRequestException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected BadRequestException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
