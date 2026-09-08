using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Invoker_Commands_API">Invoker Commands API</see>:
/// a button says which element it acts on and what it does to it, and the browser wires the two
/// together - no click handler, no id plumbing, no focus management to get wrong.
/// </summary>
/// <remarks>
/// The built-in commands (<c>show-modal</c>, <c>close</c>, <c>toggle-popover</c>, <c>show-picker</c>,
/// the media ones) are handled by the browser itself, so a dialog can be opened and closed correctly -
/// including focus restoration and the top layer - without a line of C#. A command starting with
/// <c>--</c> is a custom one: the browser dispatches the event and does nothing else, which is what
/// <see cref="OnCommand"/> is for.
/// <br/>
/// The event fires on the <b>target</b>, not the button, so one handler serves however many invokers
/// point at it.
/// <br/>
/// Chromium and Safari. Where <see cref="IsSupported"/> is false the button does nothing, so keep an
/// ordinary click handler as the fallback.
/// </remarks>
[ButilService(typeof(InvokerCommands))]
public class InvokerCommands(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeCommand);

    private readonly ConcurrentDictionary<Guid, Action<CommandEventArgs>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<InvokerCommands>? _dotNetRef;
    private DotNetObjectReference<InvokerCommands> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime implements <c>command</c>/<c>commandfor</c> and <c>CommandEvent</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.invokerCommands.isSupported");

    /// <summary>
    /// Points an invoker at a target and says what it does.
    /// </summary>
    /// <param name="invoker">The button. Must be a <c>&lt;button&gt;</c> - the API ignores anything else.</param>
    /// <param name="target">The element the command is dispatched at.</param>
    /// <param name="command">
    /// A built-in command (<c>"show-modal"</c>, <c>"close"</c>, <c>"toggle-popover"</c>,
    /// <c>"request-close"</c>, <c>"show-picker"</c>, <c>"play-pause"</c>…) or a custom one starting
    /// with <c>--</c>.
    /// </param>
    /// <returns>False when the runtime has no invoker commands.</returns>
    /// <remarks>
    /// This sets the <c>commandForElement</c> property rather than the <c>commandfor</c> attribute,
    /// because the attribute takes an <c>id</c> and a Blazor-rendered target often has none. The
    /// effect is the same, and it survives without inventing ids.
    /// </remarks>
    public ValueTask<bool> SetCommandFor(ElementReference invoker, ElementReference target, string command)
        => js.Invoke<bool>("BitButil.invokerCommands.setCommandFor", invoker, target, command);

    /// <summary>Unpoints an invoker, so it stops acting on anything.</summary>
    public ValueTask<bool> ClearCommandFor(ElementReference invoker)
        => js.Invoke<bool>("BitButil.invokerCommands.clearCommandFor", invoker);

    /// <summary>The command an invoker currently carries, or an empty string.</summary>
    public ValueTask<string> GetCommand(ElementReference invoker)
        => js.Invoke<string>("BitButil.invokerCommands.getCommand", invoker);

    /// <summary>
    /// Invoked from JS for each command event. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeCommand(Guid id, CommandEventArgs args)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(args ?? new CommandEventArgs());
    }

    /// <summary>
    /// Listens for commands dispatched at an element.
    /// </summary>
    /// <param name="target">The element commands are aimed at - the same one passed to <see cref="SetCommandFor"/>.</param>
    /// <param name="handler">
    /// Called for every command, built-in or custom. Called on the interop dispatch, so a Blazor
    /// component has to <c>StateHasChanged</c> itself.
    /// </param>
    /// <returns>A subscription that detaches the listener on dispose.</returns>
    /// <remarks>
    /// Built-in commands have already been carried out by the time this runs - the event is a
    /// notification, not a request. Custom commands (<c>--something</c>) do nothing until this
    /// handler acts on them.
    /// </remarks>
    [DynamicDependency(nameof(InvokeCommand), typeof(InvokerCommands))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandEventArgs))]
    public async Task<ButilSubscription> OnCommand(ElementReference target, Action<CommandEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.Invoke<bool>("BitButil.invokerCommands.onCommand", target, id, DotNetRef, InvokeMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.invokerCommands.offCommand", id);
        });
    }

    /// <summary>Detaches every listener registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.invokerCommands.offCommand", id);
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
