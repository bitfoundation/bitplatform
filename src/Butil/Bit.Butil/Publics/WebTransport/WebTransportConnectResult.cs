namespace Bit.Butil;

/// <summary>
/// The outcome of <see cref="WebTransport.Connect"/>.
/// </summary>
/// <param name="Session">
/// The established session, or null when the connection failed. Dispose it when you're done.
/// </param>
/// <param name="Error">
/// Why the connection failed - a rejected certificate, an unreachable host, a URL that isn't
/// <c>https:</c>, or no WebTransport support. Empty when <paramref name="Session"/> is set.
/// </param>
public sealed record WebTransportConnectResult(WebTransportHandle? Session, string Error);
