using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdleDetector">IdleDetector API</see>.
/// Requires the <c>idle-detection</c> permission, which the browser will prompt for on first
/// <see cref="Start"/>.
/// </summary>
public class IdleDetector(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeIdleDetector);

    private readonly ConcurrentDictionary<Guid, Action<IdleState>> _handlers = new();

    // Per-instance callback reference (see Keyboard): watches are isolated per circuit / WASM app
    // and released on disposal — no static state, no cross-circuit leak.
    private DotNetObjectReference<IdleDetector>? _dotNetRef;
    private DotNetObjectReference<IdleDetector> DotNetRef => _dotNetRef ??= DotNetObjectReference.Create(this);

    /// <summary>True when the runtime exposes <c>IdleDetector</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.idleDetector.isSupported");

    /// <summary>
    /// Asks the browser for the <c>idle-detection</c> permission.
    /// </summary>
    /// <returns>The new permission state.</returns>
    public ValueTask<PermissionState> RequestPermission()
        => RequestPermissionInternal();

    private async ValueTask<PermissionState> RequestPermissionInternal()
    {
        var raw = await js.Invoke<string>("BitButil.idleDetector.requestPermission");
        return raw switch
        {
            "granted" => PermissionState.Granted,
            "denied" => PermissionState.Denied,
            "prompt" => PermissionState.Prompt,
            _ => PermissionState.Unknown,
        };
    }

    /// <summary>
    /// Invoked from JS on each idle state change. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeIdleDetector(Guid id, IdleState state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(state);
    }

    /// <summary>
    /// Starts watching for idle changes. The handler fires whenever user/screen state changes.
    /// </summary>
    /// <param name="threshold">Idle threshold in seconds. Spec minimum is 60.</param>
    [DynamicDependency(nameof(InvokeIdleDetector), typeof(IdleDetector))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IdleState))]
    public async Task<ButilSubscription> Start(int threshold, Action<IdleState> handler)
    {
        if (threshold < 60) threshold = 60;

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.InvokeVoid("BitButil.idleDetector.start", DotNetRef, id, threshold);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.idleDetector.stop", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.idleDetector.stop", id);
            }
        }
        catch (JSDisconnectedException) { }
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
