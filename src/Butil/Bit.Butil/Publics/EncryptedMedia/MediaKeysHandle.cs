using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeys">MediaKeys</see>
/// object created by <see cref="EncryptedMedia.CreateMediaKeys"/> - one instance of the content
/// decryption module, serving one media element and any number of sessions.
/// </summary>
/// <remarks>
/// Dispose it when playback ends. That closes every session it opened and detaches the keys from the
/// element, which is what lets the decryption module - often a hardware pipeline that nothing else
/// can use meanwhile - be released.
/// </remarks>
public sealed class MediaKeysHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal MediaKeysHandle(IJSRuntime js, Guid id, MediaKeySystemAccessInfo access)
    {
        _js = js;
        _id = id;
        Access = access;
    }

    /// <summary>The internal keys id; sessions created from this handle are keyed under it.</summary>
    public Guid Id => _id;

    /// <summary>The key system that answered, and the configuration it resolved.</summary>
    public MediaKeySystemAccessInfo Access { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/setMediaKeys">HTMLMediaElement.setMediaKeys()</see>:
    /// hands these keys to the element that will play the protected content.
    /// </summary>
    /// <param name="mediaElement">The <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> to attach to.</param>
    /// <returns>False when the element already holds different keys, or refuses these.</returns>
    /// <remarks>
    /// Attach before the element starts loading protected media. An element with no keys stalls on
    /// the first encrypted frame instead of failing, which is a notoriously quiet way for a player to
    /// be broken.
    /// </remarks>
    public ValueTask<bool> AttachTo(ElementReference mediaElement)
        => _js.Invoke<bool>("BitButil.encryptedMedia.attach", _id, mediaElement);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeys/setServerCertificate">MediaKeys.setServerCertificate()</see>:
    /// gives the decryption module the licence server's certificate up front.
    /// </summary>
    /// <param name="certificate">The server certificate bytes, as the licence service publishes them.</param>
    /// <returns>True when the certificate was accepted; false when the key system doesn't use one, or rejected it.</returns>
    /// <remarks>
    /// Optional for most key systems and mandatory for a few, but useful everywhere it is accepted:
    /// it lets the module encrypt its very first licence request, which removes a round trip and
    /// stops the request from leaking device information in the clear.
    /// </remarks>
    public ValueTask<bool> SetServerCertificate(byte[] certificate)
        => _js.Invoke<bool>("BitButil.encryptedMedia.setServerCertificate", _id, certificate);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeys/createSession">MediaKeys.createSession()</see>:
    /// opens a session that will hold one licence.
    /// </summary>
    /// <param name="onMessage">
    /// Called with every request the session wants delivered to the licence server - including
    /// renewals raised long after playback started. Required: a session whose messages nobody
    /// carries can never be licensed.
    /// </param>
    /// <param name="sessionType">Whether the licence may be stored for later offline playback.</param>
    /// <param name="onKeyStatusesChange">
    /// Called whenever the session's key statuses change, which is how output restrictions and
    /// expiries surface.
    /// </param>
    /// <returns>The session handle, or <c>null</c> when the key system refuses the session type.</returns>
    /// <remarks>
    /// A new session produces nothing until <see cref="MediaKeySessionHandle.GenerateRequest(EncryptedMediaInitData)"/> feeds
    /// it the initialization data from <see cref="EncryptedMedia.SubscribeEncrypted"/>.
    /// </remarks>
    public async ValueTask<MediaKeySessionHandle?> CreateSession(Action<MediaKeyMessage> onMessage,
                                                                 MediaKeySessionType sessionType = MediaKeySessionType.Temporary,
                                                                 Action<MediaKeyStatusEntry[]>? onKeyStatusesChange = null)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        var sessionId = Guid.NewGuid();
        var handle = new MediaKeySessionHandle(_js, _id, sessionId, onMessage, onKeyStatusesChange);

        var created = await _js.Invoke<bool>("BitButil.encryptedMedia.createSession",
                                             _id, sessionId, MediaKeySystemConfiguration.ToName(sessionType), handle.CallbackRef,
                                             MediaKeySessionHandle.MessageMethodName, MediaKeySessionHandle.KeyStatusesMethodName);
        if (created is false)
        {
            await handle.DisposeAsync();
            return null;
        }

        return handle;
    }

    /// <summary>
    /// Closes every session this handle opened and detaches the keys from their element. Calling it
    /// again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.encryptedMedia.dispose", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
