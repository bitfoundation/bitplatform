using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/BroadcastChannel">BroadcastChannel</see>
/// API for cross-tab pub/sub on the same origin.
/// </summary>
/// <remarks>
/// Each <see cref="BroadcastChannel"/> instance can host any number of named channels — a
/// new JS-side channel object is created on first <see cref="Subscribe"/> per name and torn
/// down only when every subscription on that name has been disposed.
/// </remarks>
public class BroadcastChannel(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, string> _subscriptions = new();

    /// <summary>True when the runtime exposes <c>BroadcastChannel</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.broadcastChannel.isSupported");

    /// <summary>
    /// Sends <paramref name="message"/> to every other listener on <paramref name="channelName"/>
    /// in the same origin (the sender does not receive its own message — that's the spec).
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask Post<[DynamicallyAccessedMembers(JsonSerialized)] T>(string channelName, T message)
        => js.InvokeVoid("BitButil.broadcastChannel.post", channelName, message);

    /// <summary>
    /// Subscribes to <paramref name="channelName"/>. The handler receives every message as a
    /// <see cref="JsonElement"/> so callers can deserialize into whatever shape they expect.
    /// Use the returned <see cref="ButilSubscription"/> to detach.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BroadcastChannelListenersManager))]
    public async Task<ButilSubscription> Subscribe(string channelName,
        Action<JsonElement>? onMessage,
        Action? onError = null)
    {
        if (onMessage is null && onError is null)
            throw new ArgumentException("At least one of onMessage or onError must be provided.");

        var id = BroadcastChannelListenersManager.AddListener(onMessage, onError);
        _subscriptions.TryAdd(id, channelName);

        await js.InvokeVoid("BitButil.broadcastChannel.subscribe",
            BroadcastChannelListenersManager.MessageMethodName,
            BroadcastChannelListenersManager.ErrorMethodName,
            id,
            channelName);

        return new ButilSubscription(id, async () =>
        {
            BroadcastChannelListenersManager.RemoveListener(id);
            _subscriptions.TryRemove(id, out _);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.broadcastChannel.unsubscribe", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_subscriptions.IsEmpty is false)
            {
                var ids = _subscriptions.Keys.ToArray();
                _subscriptions.Clear();
                foreach (var id in ids)
                {
                    BroadcastChannelListenersManager.RemoveListener(id);
                    if (OperatingSystem.IsBrowser())
                    {
                        await js.InvokeVoid("BitButil.broadcastChannel.unsubscribe", id);
                    }
                }
            }
        }
        catch (JSDisconnectedException) { }
        GC.SuppressFinalize(this);
    }
}
