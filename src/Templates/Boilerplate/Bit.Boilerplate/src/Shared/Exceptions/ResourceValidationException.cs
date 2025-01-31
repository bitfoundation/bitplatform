using System.Net;

namespace Boilerplate.Shared.Exceptions;

public partial class ResourceValidationException : RestException
{
    public ResourceValidationException(params LocalizedString[] errorMessages)
        : this([("*", errorMessages)])
    {

    }

    public ResourceValidationException(params (string propName, LocalizedString[] errorMessages)[] details)
        : this(new ErrorResourcePayload()
        {
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

    public ResourceValidationException(ErrorResourcePayload? payload)
        : this(message: nameof(AppStrings.ResourceValidationException), payload)
    {

    }

    public ResourceValidationException(string message, ErrorResourcePayload? payload)
        : base(message)
    {
        Payload = payload ?? new();
    }

    public ErrorResourcePayload Payload { get; set; } = new ErrorResourcePayload();

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
