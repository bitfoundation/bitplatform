namespace Boilerplate.Shared.Infrastructure.Exceptions;

/// <summary>
/// This exception is thrown when there is a failure connecting client app to the backend api server.
/// </summary>
public partial class TransientException : KnownException
{
    public TransientException()
        : base(nameof(AppStrings.ServerConnectionException))
    {
    }

    public TransientException(string message)
        : base(message)
    {
    }

    public TransientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
