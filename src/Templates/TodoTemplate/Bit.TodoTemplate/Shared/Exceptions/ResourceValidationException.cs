using System.Net;
using System.Runtime.Serialization;

namespace TodoTemplate.Shared.Exceptions;

[Serializable]
public class ResourceValidationException : RestException
{
    public ResourceValidationException(LocalizedString errorMessage)
        : this(new[] { errorMessage })
    {

    }

    public ResourceValidationException(params LocalizedString[] errorMessages)
        : this(errorMessages.Select(errMsg => ("*", string.Empty, new[] { errMsg })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, LocalizedString errorMessages)[] errors)
        : this(errors.Select(err => (err.propName, string.Empty, new[] { err.errorMessages })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, Type resourceType, LocalizedString errorMessages)[] errors)
        : this(errors.Select(err => (err.propName, err.resourceType.Name, new[] { err.errorMessages })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, string resourceTypeName, LocalizedString[] errorMessages)[] errors)
        : this(new List<ResourceValidationExceptionPayload>(errors
                .Select(e => new ResourceValidationExceptionPayload
                {
                    ResourceTypeName = e.resourceTypeName,
                    Property = e.propName,
                    Errors = e.errorMessages.Select(err => new ResourceValidationExceptionPayloadError { Key = err.Name, Message = err.Value }).ToArray()
                })))
    {

    }

    public ResourceValidationException(List<ResourceValidationExceptionPayload> error)
        : base(message: nameof(AppStrings.ResourceValidationException))
    {
        Details = error;
    }

    protected ResourceValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public List<ResourceValidationExceptionPayload> Details { get; set; } = new List<ResourceValidationExceptionPayload>();

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
