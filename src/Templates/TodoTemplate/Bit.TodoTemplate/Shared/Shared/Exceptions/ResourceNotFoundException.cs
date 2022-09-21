using System.Net;
using System.Runtime.Serialization;

namespace TodoTemplate.Shared.Exceptions;

[Serializable]
public class ResourceNotFoundException : RestException
{
    public ResourceNotFoundException()
        : base(nameof(AppStrings.ResourceNotFoundException))
    {
    }

    public ResourceNotFoundException(string message)
        : base(message)
    {
    }

    public ResourceNotFoundException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ResourceNotFoundException(LocalizedString message)
        : base(message)
    {
    }

    public ResourceNotFoundException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ResourceNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
