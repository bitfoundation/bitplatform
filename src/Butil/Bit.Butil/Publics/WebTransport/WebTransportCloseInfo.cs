namespace Bit.Butil;

/// <summary>
/// How a WebTransport session ended.
/// </summary>
/// <param name="CloseCode">The application close code the peer sent, or 0.</param>
/// <param name="Reason">The peer's reason string, or empty.</param>
/// <param name="Error">
/// Set when the session died rather than closed - a network failure, a rejected certificate, a
/// protocol error. Empty for an orderly close by either side.
/// </param>
public sealed record WebTransportCloseInfo(int CloseCode, string Reason, string Error);
