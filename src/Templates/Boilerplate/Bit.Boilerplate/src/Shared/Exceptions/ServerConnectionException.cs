namespace Boilerplate.Shared.Exceptions;
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
