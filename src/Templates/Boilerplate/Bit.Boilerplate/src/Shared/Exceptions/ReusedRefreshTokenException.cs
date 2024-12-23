using System.Net;

namespace Boilerplate.Shared.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a refresh token has been reused and is no longer valid.
/// </summary>
/// <remarks>
/// Refresh token rotation ensures that each refresh token can only be used once. 
/// This exception is typically thrown to indicate a potential security issue or misuse of the refresh token.
/// </remarks>
public class ReusedRefreshTokenException : RestException
{
    public ReusedRefreshTokenException()
        : base(nameof(AppStrings.ConcurrentUserSessionOnTheSameDevice))
    {
    }

    public ReusedRefreshTokenException(string message)
        : base(message)
    {
    }

    public ReusedRefreshTokenException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ReusedRefreshTokenException(LocalizedString message)
        : base(message)
    {
    }

    public ReusedRefreshTokenException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}
