namespace Bit.Bcrdt;

/// <summary>
/// Thrown by <see cref="IBcrdtTransport.ConnectAsync"/> when the peer refused the connection.
/// </summary>
/// <remarks>
/// Distinct from an ordinary connection failure because retrying cannot help: the same credentials
/// will be refused again. The <c>BcrdtSyncProvider</c> treats it as final, stops reconnecting and raises
/// its <c>AuthorizationDenied</c> event.
/// </remarks>
public sealed class BcrdtTransportAuthorizationException : Exception
{
    /// <summary>Creates an exception with a default message.</summary>
    public BcrdtTransportAuthorizationException()
        : base("The peer refused the connection.")
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    public BcrdtTransportAuthorizationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that revealed the refusal.</summary>
    public BcrdtTransportAuthorizationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
