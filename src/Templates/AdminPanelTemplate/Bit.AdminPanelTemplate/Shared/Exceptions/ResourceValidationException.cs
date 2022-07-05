using System.Net;
using System.Runtime.Serialization;

namespace AdminPanelTemplate.Shared.Exceptions;

[Serializable]
public class ResourceValidationException : RestException
{
    public ResourceValidationException(params string[] errorMessages)
        : this(errorMessages.Select(errMsg => (string.Empty, string.Empty, new[] { errMsg })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, string errorMessages)[] errors)
        : this(errors.Select(err => (err.propName, string.Empty, new[] { err.errorMessages })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, Type resourceType, string errorMessages)[] errors)
        : this(errors.Select(err => (err.propName, err.resourceType.Name, new[] { err.errorMessages })).ToArray())
    {

    }

    public ResourceValidationException(params (string propName, string resourceTypeName, string[] errorMessages)[] errors)
        : this(new List<ResourceValidationExceptionPayload>(errors
                .Select(e => new ResourceValidationExceptionPayload
                {
                    ResourceTypeName = e.resourceTypeName,
                    Property = e.propName,
                    Messages = e.errorMessages
                })))
    {

    }

    public ResourceValidationException(List<ResourceValidationExceptionPayload> error)
        : base(message: nameof(ResourceValidationException))
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
