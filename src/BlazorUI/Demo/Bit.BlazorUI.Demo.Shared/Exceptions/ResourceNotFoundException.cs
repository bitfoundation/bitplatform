using System.Net;

namespace Bit.BlazorUI.Demo.Shared.Exceptions;

public class ResourceNotFoundException : RestException
{
    public ResourceNotFoundException()
        : base(nameof(ResourceNotFoundException))
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

    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
