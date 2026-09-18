using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_MIDI_API">Web MIDI API</see>
/// (<c>navigator.requestMIDIAccess</c>): enumerate MIDI inputs and outputs, listen to the messages
/// a controller sends, and send messages to a synth.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. <see cref="RequestAccess"/> prompts the user; asking for
/// system-exclusive access is a separate, stricter prompt, so leave it off unless the app really
/// does need to reprogram a device.
/// <br/>
/// Everything else on this service returns nothing useful until <see cref="RequestAccess"/> has
/// resolved - the port list is part of the grant, not something readable ahead of it.
/// </remarks>
[ButilService(typeof(Midi))]
public class Midi(IJSRuntime js) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeMidiMessage);
    internal const string StateChangeMethodName = nameof(InvokeMidiStateChange);

    private readonly ConcurrentDictionary<Guid, Action<MidiMessage>> _messageHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<MidiPortInfo?>> _stateHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Midi>? _dotNetRef;
    private DotNetObjectReference<Midi> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.requestMIDIAccess</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.midi.isSupported");

    /// <summary>
    /// Asks the user for MIDI access and returns the ports it covers, or null when the prompt was
    /// refused. The resolved access is cached for the page, so calling this again with the same
    /// arguments is cheap and does not re-prompt.
    /// </summary>
    /// <remarks>
    /// Asking again with a different <paramref name="sysex"/> or <paramref name="software"/> is a
    /// different grant, so the browser replaces the access object and every port object under it.
    /// Subscriptions already handed out survive that: they are re-attached to the new ports, so a
    /// handler keeps firing and the <see cref="ButilSubscription"/> stays the way to detach it.
    /// A refusal leaves an access already granted untouched, subscriptions included.
    /// </remarks>
    /// <param name="sysex">
    /// Request system-exclusive messages as well. A separate and stricter permission - sysex can
    /// rewrite a device's firmware - so ask for it only when the app sends or reads sysex.
    /// </param>
    /// <param name="software">Include software synthesizers in the port list, not only hardware.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiAccessInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiPortInfo))]
    public ValueTask<MidiAccessInfo?> RequestAccess(bool sysex = false, bool software = false)
        => js.Invoke<MidiAccessInfo?>("BitButil.midi.requestAccess", sysex, software);

    /// <summary>
    /// The current port list. Null until <see cref="RequestAccess"/> has resolved - reading the
    /// ports is not itself a way to get permission.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiAccessInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiPortInfo))]
    public ValueTask<MidiAccessInfo?> GetPorts() => js.Invoke<MidiAccessInfo?>("BitButil.midi.getPorts");

    /// <summary>
    /// Sends raw MIDI bytes to an output port, opening it first if needed.
    /// </summary>
    /// <param name="outputId">The <see cref="MidiPortInfo.Id"/> of the output.</param>
    /// <param name="data">The message bytes - status byte first.</param>
    /// <param name="timestamp">
    /// When to send it, on the <c>performance.now()</c> clock. Null or 0 sends immediately;
    /// scheduling ahead is how a sequencer gets timing the main thread cannot deliver.
    /// </param>
    public ValueTask<bool> Send(string outputId, byte[] data, double? timestamp = null)
        => js.Invoke<bool>("BitButil.midi.send", outputId, data, timestamp);

    /// <summary>
    /// Sends a note-on message on <paramref name="channel"/> (0-15). A velocity of 0 is a note-off
    /// by convention, and is what most devices actually send when a key is released.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is above 15.</exception>
    public ValueTask<bool> SendNoteOn(string outputId, byte note, byte velocity = 100, byte channel = 0)
    {
        ThrowIfNotAChannel(channel);
        return Send(outputId, [(byte)(0x90 | channel), note, velocity]);
    }

    /// <summary>Sends a note-off message on <paramref name="channel"/> (0-15).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is above 15.</exception>
    public ValueTask<bool> SendNoteOff(string outputId, byte note, byte velocity = 0, byte channel = 0)
    {
        ThrowIfNotAChannel(channel);
        return Send(outputId, [(byte)(0x80 | channel), note, velocity]);
    }

    // Masking a bad channel down to 0-15 would send the message on some other channel and look like
    // the device ignored it; the caller's arithmetic is what is wrong, so it is raised as such.
    private static void ThrowIfNotAChannel(byte channel, [CallerArgumentExpression(nameof(channel))] string? paramName = null)
    {
        if (channel > 15) throw new ArgumentOutOfRangeException(paramName, channel, "A MIDI channel is 0-15.");
    }

    /// <summary>
    /// Drops every message queued for the port that has not been sent yet - the way out of a note
    /// left hanging by a cancelled sequence.
    /// </summary>
    public ValueTask<bool> Clear(string outputId) => js.Invoke<bool>("BitButil.midi.clear", outputId);

    /// <summary>
    /// Invoked from JS for each incoming MIDI message. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeMidiMessage(Guid id, MidiMessage message)
    {
        if (_messageHandlers.TryGetValue(id, out var handler)) handler.Invoke(message);
    }

    /// <summary>Invoked from JS on a port's <c>statechange</c>. See <see cref="InvokeMidiMessage"/>.</summary>
    [JSInvokable(StateChangeMethodName)]
    public void InvokeMidiStateChange(Guid id, MidiPortInfo? port)
    {
        if (_stateHandlers.TryGetValue(id, out var handler)) handler.Invoke(port);
    }

    /// <summary>
    /// Listens to the messages arriving on one input, or on every input at once.
    /// </summary>
    /// <param name="handler">Called for each message, with the port it came from.</param>
    /// <param name="inputId">
    /// The input to listen to, or null for all of them - which is usually what an app wants, since
    /// the user's controller is whichever one they touch.
    /// </param>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <remarks>
    /// Listening to all of them is a standing subscription, not a snapshot: a controller plugged in
    /// afterwards is picked up too, and subscribing before anything is plugged in is allowed. A
    /// named <paramref name="inputId"/>, by contrast, has to exist at the time of the call.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The listener was not attached - no MIDI access, or no such input.</exception>
    [DynamicDependency(nameof(InvokeMidiMessage), typeof(Midi))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiMessage))]
    public async ValueTask<ButilSubscription> SubscribeMessages(Action<MidiMessage> handler, string? inputId = null)
    {
        return await ButilSubscriptionHelper.Register(_messageHandlers, handler,
                                                      id => js.InvokeRegister("BitButil.midi.subscribeMessages", id, DotNetRef, inputId),
                                                      id => js.InvokeVoid("BitButil.midi.unsubscribeMessages", id),
                                                      "The MIDI message listener could not be attached - call RequestAccess() first, and make sure the named input exists.");
    }

    /// <summary>
    /// Watches ports being connected and disconnected. The handler gets the port that changed, so
    /// re-reading the whole list is only necessary when the app tracks more than one.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <exception cref="InvalidOperationException">The listener was not attached - no MIDI access.</exception>
    [DynamicDependency(nameof(InvokeMidiStateChange), typeof(Midi))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MidiPortInfo))]
    public async ValueTask<ButilSubscription> SubscribeStateChange(Action<MidiPortInfo?> handler)
    {
        return await ButilSubscriptionHelper.Register(_stateHandlers, handler,
                                                      id => js.InvokeRegister("BitButil.midi.subscribeStateChange", id, DotNetRef),
                                                      id => js.InvokeVoid("BitButil.midi.unsubscribeStateChange", id),
                                                      "The MIDI state-change listener could not be attached - call RequestAccess() first.");
    }

    /// <summary>Detaches every listener this instance attached and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var id in _messageHandlers.Keys.ToArray())
            {
                _messageHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.midi.unsubscribeMessages", id);
            }

            foreach (var id in _stateHandlers.Keys.ToArray())
            {
                _stateHandlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.midi.unsubscribeStateChange", id);
            }
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
