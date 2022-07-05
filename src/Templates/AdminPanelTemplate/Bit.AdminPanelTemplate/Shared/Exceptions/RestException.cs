using System.Net;
using System.Runtime.Serialization;

namespace AdminPanelTemplate.Shared.Exceptions;

[Serializable]
public class RestException : KnownException
{
    public RestException()
        : this(nameof(RestException))
    {
    }

    public RestException(string? message)
        : base(message)
    {
    }

    public RestException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected RestException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
}
