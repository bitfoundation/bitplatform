using System.Net;

namespace Bit.Websites.Careers.Shared.Exceptions;

public class ResourceValidationException : RestException
{
    public ResourceValidationException(ErrorResourcePayload payload)
        : this(message: nameof(ResourceValidationException), payload)
    {

    }

    public ResourceValidationException(string message, ErrorResourcePayload payload)
        : base(message)
    {
        Payload = payload;
    }

    public ErrorResourcePayload Payload { get; set; } = new ErrorResourcePayload();

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
