using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Gamepad_API">Gamepad API</see>:
/// reads connected controllers and, where the hardware allows it, plays haptic effects.
/// </summary>
/// <remarks>
/// Gamepads have no input events - the platform only offers a polled snapshot - so button and axis
/// values come from <see cref="GetGamepads"/> or from <see cref="SubscribeChanges"/>, which polls in
/// the browser's frame loop and only crosses into .NET when something actually moved.
/// <br/>
/// For privacy, browsers hide connected pads until the user has interacted with one: until then
/// <see cref="GetGamepads"/> returns an empty array even though a controller is plugged in. Ask the
/// user to press a button.
/// </remarks>
public class Gamepad(IJSRuntime js) : IAsyncDisposable
{
    internal const string ConnectedMethodName = nameof(InvokeGamepadConnected);
    internal const string DisconnectedMethodName = nameof(InvokeGamepadDisconnected);
    internal const string ChangedMethodName = nameof(InvokeGamepadChanged);

    private readonly ConcurrentDictionary<Guid, (Action<GamepadState[]>? OnConnected, Action<GamepadState[]>? OnDisconnected)> _connectionHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<GamepadState[]>> _changeHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Gamepad>? _dotNetRef;
    private DotNetObjectReference<Gamepad> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.getGamepads</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.gamepad.isSupported");

    /// <summary>
    /// A snapshot of every connected gamepad. Empty until the user has pressed a button on one -
    /// browsers withhold controllers from a page that hasn't been interacted with.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadButton))]
    public ValueTask<GamepadState[]> GetGamepads() => js.Invoke<GamepadState[]>("BitButil.gamepad.getGamepads");

    /// <summary>
    /// Invoked from JS on <c>gamepadconnected</c>. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ConnectedMethodName)]
    public void InvokeGamepadConnected(Guid id, GamepadState[] gamepads)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnConnected?.Invoke(gamepads);
    }

    /// <summary>
    /// Invoked from JS on <c>gamepaddisconnected</c>. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DisconnectedMethodName)]
    public void InvokeGamepadDisconnected(Guid id, GamepadState[] gamepads)
    {
        if (_connectionHandlers.TryGetValue(id, out var handlers)) handlers.OnDisconnected?.Invoke(gamepads);
    }

    /// <summary>
    /// Invoked from JS when a polled snapshot differs from the previous one. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChangedMethodName)]
    public void InvokeGamepadChanged(Guid id, GamepadState[] gamepads)
    {
        if (_changeHandlers.TryGetValue(id, out var handler)) handler.Invoke(gamepads);
    }

    /// <summary>
    /// Watches controllers being plugged in and unplugged. Both callbacks receive the full list of
    /// connected pads as it is after the change, not just the one that changed.
    /// </summary>
    /// <param name="onConnected">Called when a gamepad becomes available.</param>
    /// <param name="onDisconnected">Called when a gamepad goes away.</param>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    [DynamicDependency(nameof(InvokeGamepadConnected), typeof(Gamepad))]
    [DynamicDependency(nameof(InvokeGamepadDisconnected), typeof(Gamepad))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadButton))]
    public async ValueTask<ButilSubscription> SubscribeConnection(Action<GamepadState[]>? onConnected = null,
                                                                  Action<GamepadState[]>? onDisconnected = null)
    {
        var id = Guid.NewGuid();
        _connectionHandlers[id] = (onConnected, onDisconnected);
        await js.InvokeVoid("BitButil.gamepad.subscribeConnection", DotNetRef, id);

        return new ButilSubscription(id, async () =>
        {
            _connectionHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.gamepad.unsubscribeConnection", id);
        });
    }

    /// <summary>
    /// Polls every connected gamepad in the browser's animation frame loop and calls
    /// <paramref name="handler"/> whenever the state changes.
    /// </summary>
    /// <param name="handler">Called with the full list of connected pads on each change.</param>
    /// <param name="minIntervalMs">
    /// The floor between two callbacks. Analogue sticks jitter constantly, so polling at the full
    /// frame rate would call into .NET ~60 times a second for a controller sitting still; the
    /// default trades a little latency for a manageable message rate. Pass 0 for every frame.
    /// </param>
    /// <returns>A subscription - dispose it to stop polling.</returns>
    /// <remarks>
    /// Polling stops on its own while the tab is hidden, because that is what
    /// <c>requestAnimationFrame</c> does, and resumes when the tab is shown again.
    /// </remarks>
    [DynamicDependency(nameof(InvokeGamepadChanged), typeof(Gamepad))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GamepadButton))]
    public async ValueTask<ButilSubscription> SubscribeChanges(Action<GamepadState[]> handler, int minIntervalMs = 50)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _changeHandlers[id] = handler;
        await js.InvokeVoid("BitButil.gamepad.subscribePoll", DotNetRef, id, Math.Max(0, minIntervalMs));

        return new ButilSubscription(id, async () =>
        {
            _changeHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.gamepad.unsubscribePoll", id);
        });
    }

    /// <summary>
    /// Plays a <c>dual-rumble</c> haptic effect on the pad at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The gamepad's <see cref="GamepadState.Index"/>.</param>
    /// <param name="durationMs">How long the effect runs, in milliseconds.</param>
    /// <param name="strongMagnitude">Intensity of the low-frequency (heavy) motor, 0 to 1.</param>
    /// <param name="weakMagnitude">Intensity of the high-frequency (light) motor, 0 to 1.</param>
    /// <param name="startDelayMs">How long to wait before starting, in milliseconds.</param>
    /// <returns>False when the pad is gone or has no vibration actuator.</returns>
    /// <remarks>
    /// Magnitudes outside 0 to 1 are clamped here rather than being sent on, since the underlying
    /// API rejects the whole effect for an out-of-range value.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Vibrate(int index,
                                   int durationMs = 200,
                                   double strongMagnitude = 1,
                                   double weakMagnitude = 1,
                                   int startDelayMs = 0)
        => js.Invoke<bool>("BitButil.gamepad.vibrate", index, Math.Max(0, durationMs),
            Math.Clamp(strongMagnitude, 0, 1), Math.Clamp(weakMagnitude, 0, 1), Math.Max(0, startDelayMs));

    /// <summary>Stops any haptic effect currently playing on the pad at <paramref name="index"/>.</summary>
    /// <param name="index">The gamepad's <see cref="GamepadState.Index"/>.</param>
    public ValueTask ResetVibration(int index) => js.InvokeVoid("BitButil.gamepad.resetVibration", index);

    /// <summary>
    /// On scope/circuit teardown, detaches any listener whose <see cref="ButilSubscription"/> was
    /// never disposed so an abandoned poll loop can't keep running.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _connectionHandlers.Clear();
            _changeHandlers.Clear();
            await js.InvokeVoid("BitButil.gamepad.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
