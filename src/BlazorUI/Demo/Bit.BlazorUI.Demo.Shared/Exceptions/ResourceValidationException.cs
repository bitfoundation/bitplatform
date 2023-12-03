using System.Net;

namespace Bit.BlazorUI.Demo.Shared.Exceptions;

public class ResourceValidationException : RestException
{
    public ResourceValidationException(params LocalizedString[] errorMessages)
    : this([("*", errorMessages)])
    {

    }

    public ResourceValidationException((string propName, LocalizedString[] errorMessages)[] details)
        : this("*", details)
    {

    }

    public ResourceValidationException(Type resourceType, (string propName, LocalizedString[] errorMessages)[] details)
        : this(resourceType.FullName!, details)
    {

    }

    public ResourceValidationException(string resourceTypeName, (string propName, LocalizedString[] errorMessages)[] details)
        : this(new ErrorResourcePayload()
        {
            ResourceTypeName = resourceTypeName,
            Details = details.Select(propErrors => new PropertyErrorResourceCollection
            {
                Name = propErrors.propName,
                Errors = propErrors.errorMessages.Select(e => new ErrorResource()
                {
                    Key = e.Name,
                    Message = e.Value
                }).ToList()
            }).ToList()
        })
    {

    }

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
