namespace Bit.Websites.Sales.Shared.Exceptions;
public class ServerConnectionException : UnknownException
{
    public ServerConnectionException()
        : base(nameof(ServerConnectionException))
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
