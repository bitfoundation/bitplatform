using System.Diagnostics.CodeAnalysis;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

/// <summary>
/// An <see cref="IJSRuntime"/> that answers every call with <c>default</c>, recording what was invoked
/// and with which arguments.
/// </summary>
/// <remarks>
/// This harness runs outside a browser, so there is no JS to call. Most of it only needs the services to
/// be constructible and callable - the E2E test project covers actual browser behaviour - but the
/// argument <em>shape</em> a JavaScript module depends on is checkable here, without a browser, which is
/// what <see cref="CancellationContract"/> uses the recording for.
/// <br/>
/// <see cref="Answer"/> and <see cref="OnInvoke"/> are what make it more than a sink: the first lets a
/// check hand back a specific value for one identifier, and the second runs as a call is dispatched, so
/// a token can be cancelled while a call is in flight.
/// </remarks>
internal sealed class StubJSRuntime : IJSRuntime
{
    private readonly List<Call> _calls = [];

    /// <summary>One recorded call: its position in the sequence, what was invoked, and with what.</summary>
    internal sealed record Call(int Order, string Identifier, object?[] Args);

    /// <summary>Runs as a call is dispatched, before it is answered.</summary>
    internal Action<string>? OnInvoke { get; set; }

    /// <summary>Answers one identifier with a specific value instead of <c>default</c>.</summary>
    internal Func<string, object?>? Answer { get; set; }

    /// <summary>Everything invoked so far, in order. A snapshot, taken under the lock.</summary>
    internal Call[] Calls
    {
        get { lock (_calls) return [.. _calls]; }
    }

    internal Call[] All(string identifier) => [.. Calls.Where(call => call.Identifier == identifier)];

    internal Call? First(string identifier) => All(identifier).FirstOrDefault();

    // The DynamicallyAccessedMembers annotations have to mirror IJSRuntime's exactly, or the trim
    // analyzer reports IL2095 on the override.
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        lock (_calls) _calls.Add(new Call(_calls.Count, identifier, args ?? []));

        OnInvoke?.Invoke(identifier);

        return Answer?.Invoke(identifier) is TValue answer ? new(answer) : new(default(TValue)!);
    }
}
