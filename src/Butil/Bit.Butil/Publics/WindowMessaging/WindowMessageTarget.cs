using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Another browsing context to post to: an iframe's document, the parent, the top of the frame tree,
/// the opener, or a window this page opened. Created by <see cref="WindowMessaging.Frame"/> and its
/// siblings.
/// </summary>
/// <remarks>
/// A target is a description, not a reference - the window it names is looked up on each send.
/// That matters for an iframe, whose content window is replaced on every navigation: a target made
/// once keeps addressing whatever document is in the frame now, rather than a stale one.
/// </remarks>
public sealed class WindowMessageTarget
{
    private readonly IJSRuntime _js;
    private readonly string _kind;
    private readonly ElementReference? _element;
    private readonly string? _windowId;

    internal WindowMessageTarget(IJSRuntime js, string kind, ElementReference? element, string? windowId)
    {
        _js = js;
        _kind = kind;
        _element = element;
        _windowId = windowId;
    }

    /// <summary>
    /// Posts a message, serialized as JSON.
    /// </summary>
    /// <param name="value">The payload. The receiver sees the parsed JSON.</param>
    /// <param name="targetOrigin">
    /// The origin the receiving document must have for the message to be delivered, as
    /// <c>"https://example.com"</c>. <c>"*"</c> means any, and hands your payload to whatever
    /// document happens to be in that frame - name the origin unless the message is genuinely
    /// public.
    /// </param>
    /// <param name="options">Serializer options.</param>
    /// <returns>False when the target window is not there: a frame that has not loaded, a popup that was closed, an opener that was withheld.</returns>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostMessage<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, string targetOrigin, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetOrigin);
        return _js.Invoke<bool>("BitButil.windowMessaging.postJson",
            _kind, _element, _windowId, targetOrigin, JsonSerializer.Serialize(value, options));
    }

    /// <summary>
    /// Posts raw bytes.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <param name="targetOrigin">The origin the receiving document must have. See <see cref="PostMessage"/>.</param>
    /// <param name="transfer">
    /// When true (the default) the <c>ArrayBuffer</c> moves to the receiver rather than being
    /// copied. Across documents that is the difference between one allocation and two.
    /// </param>
    /// <returns>False when the target window is not there.</returns>
    public ValueTask<bool> PostBytes(byte[] data, string targetOrigin, bool transfer = true)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetOrigin);
        return _js.Invoke<bool>("BitButil.windowMessaging.postBytes",
            _kind, _element, _windowId, targetOrigin, data, transfer);
    }

    /// <summary>
    /// Posts a message that hands <see cref="MessagePortHandle"/>s to the other document.
    /// </summary>
    /// <returns>False when the target window is not there, or a port has already been released.</returns>
    /// <remarks>
    /// This is the handshake that turns cross-document messaging into a conversation: post once with
    /// a port, and everything after that goes over the port instead - no origin argument on every
    /// message, and nobody else listening in. The ports are transferred, so the handles passed here
    /// stop working.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostWithPorts<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, string targetOrigin, MessagePortHandle[] ports, JsonSerializerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetOrigin);
        ArgumentNullException.ThrowIfNull(ports);
        return _js.Invoke<bool>("BitButil.windowMessaging.postWithPorts",
            _kind, _element, _windowId, targetOrigin, JsonSerializer.Serialize(value, options),
            Array.ConvertAll(ports, p => p.Id));
    }
}
