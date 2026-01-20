using System.Net;

namespace Boilerplate.Shared.Infrastructure.Exceptions;

public partial class RestException : KnownException
{
    public RestException()
        : base(nameof(AppStrings.RestException))
    {
    }

    public RestException(string message)
        : base(message)
    {
    }

    public RestException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public RestException(LocalizedString message)
        : base(message)
    {
    }

    public RestException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
}
