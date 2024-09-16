using System.Net;

namespace Boilerplate.Shared.Exceptions;

public partial class BadRequestException : RestException
{
    public BadRequestException()
        : base(nameof(AppStrings.BadRequestException))
    {
    }

    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public BadRequestException(LocalizedString message)
        : base(message)
    {
    }

    public BadRequestException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
