using System.Net;
using System.Runtime.Serialization;

namespace BlazorDual.Shared.Exceptions;

[Serializable]
public class ResourceValidationException : RestException
{
    public ResourceValidationException(params LocalizedString[] errorMessages)
    : this(new[] { ("*", errorMessages) })
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
        : this(new ResourceValidationExceptionPayload()
        {
            ResourceTypeName = resourceTypeName,
            Details = details.Select(propErrors => new ResourceValidationExceptionPropertyErrors
            {
                Property = propErrors.propName,
                Messages = propErrors.errorMessages.Select(e => new ResourceValidationExceptionPropertyError()
                {
                    Key = e.Name,
                    Message = e.Value
                }).ToList()
            }).ToList()
        })
    {

    }

    public ResourceValidationException(ResourceValidationExceptionPayload payload)
        : this(message: nameof(ResourceNotFoundException), payload)
    {

    }

    public ResourceValidationException(string message, ResourceValidationExceptionPayload payload)
        : base(message)
    {
        Payload = payload;
    }

    protected ResourceValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public ResourceValidationExceptionPayload Payload { get; set; } = new ResourceValidationExceptionPayload();

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
