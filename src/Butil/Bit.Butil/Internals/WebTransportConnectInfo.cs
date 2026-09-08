namespace Bit.Butil;

/// <summary>
/// What the JS side reports back from a connection attempt. Internal because the public shape of
/// the answer is <see cref="WebTransportConnectResult"/>, which carries the session handle this
/// payload cannot.
/// </summary>
internal class WebTransportConnectInfo
{
    /// <summary>True once <c>WebTransport.ready</c> resolved.</summary>
    public bool Connected { get; set; }

    /// <summary>The failure's message, or empty when <see cref="Connected"/> is true.</summary>
    public string Error { get; set; } = string.Empty;
}
