using Microsoft.JSInterop;

namespace Bit.Bcrdt;

/// <summary>
/// Creates a <see cref="BcrdtBroadcastChannelTransport"/> per document, on a channel named after it.
/// </summary>
/// <remarks>
/// The channel of a document is <c>{prefix}/{documentId}</c>: one per document, so a tab hears only about the
/// documents it has open, and prefixed, so applications on one origin can keep theirs apart the way they choose
/// different IndexedDB databases.
/// </remarks>
public sealed class BcrdtBroadcastChannelTransportFactory : IBcrdtTransportFactory
{
    private readonly IJSRuntime _js;
    private readonly string _prefix;

    /// <summary>Creates a factory over the browser this application runs in.</summary>
    /// <param name="js">The JavaScript runtime of the browser.</param>
    /// <param name="channelPrefix">What every channel's name begins with.</param>
    public BcrdtBroadcastChannelTransportFactory(IJSRuntime js, string channelPrefix = BcrdtBroadcastChannelTransport.DefaultChannelPrefix)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrEmpty(channelPrefix);

        _js = js;
        _prefix = channelPrefix;
    }

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        return new BcrdtBroadcastChannelTransport(_js, $"{_prefix}/{documentId}");
    }
}
