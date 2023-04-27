using System.Net;
using System.Runtime.Serialization;

namespace BitCareers.Shared.Exceptions;

[Serializable]
public class ConflictException : RestException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ConflictException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
