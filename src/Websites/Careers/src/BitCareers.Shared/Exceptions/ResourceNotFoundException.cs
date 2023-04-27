using System.Net;
using System.Runtime.Serialization;

namespace BitCareers.Shared.Exceptions;

[Serializable]
public class ResourceNotFoundException : RestException
{
    public ResourceNotFoundException(string message)
        : base(message)
    {
    }

    public ResourceNotFoundException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ResourceNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
