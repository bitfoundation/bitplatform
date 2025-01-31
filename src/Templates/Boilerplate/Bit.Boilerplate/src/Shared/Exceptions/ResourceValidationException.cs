using System.Net;

namespace Boilerplate.Shared.Exceptions;

public partial class ResourceValidationException : RestException
{
    public ResourceValidationException(params LocalizedString[] errorMessages)
    : this([("*", errorMessages)])
    {

    }

    public ResourceValidationException(params (string propName, LocalizedString[] errorMessages)[] details)
        : this("*", details)
    {

    }

    public ResourceValidationException(Type resourceType, params (string propName, LocalizedString[] errorMessages)[] details)
        : this(resourceType.FullName!, details)
    {

    }

    public ResourceValidationException(string resourceTypeName, params (string propName, LocalizedString[] errorMessages)[] details)
        : this(new ModelStateErrors()
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

    public ResourceValidationException(ModelStateErrors modelErrors)
        : this(message: nameof(AppStrings.ResourceValidationException), modelErrors)
    {

    }

    public ResourceValidationException(string message, ModelStateErrors? payload)
        : base(message)
    {
        Payload = payload ?? new();
    }

    public ModelStateErrors Payload { get; set; } = new ModelStateErrors();

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
