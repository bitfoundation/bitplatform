namespace Boilerplate.Shared.Exceptions;

/// <summary>
/// This exception is thrown when there is a failure connecting client app to the backend api server.
/// </summary>
public partial class ServerConnectionException : KnownException
{
    public ServerConnectionException()
        : base(nameof(AppStrings.ServerConnectionException))
    {
    }

    public ServerConnectionException(string message)
        : base(message)
    {
    }

    public ServerConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
