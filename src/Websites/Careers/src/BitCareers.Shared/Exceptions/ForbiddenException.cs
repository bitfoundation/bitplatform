using System.Net;
using System.Runtime.Serialization;

namespace BitCareers.Shared.Exceptions;

[Serializable]
public class ForbiddenException : RestException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ForbiddenException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}
