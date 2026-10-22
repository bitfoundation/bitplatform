using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// A transport that replays a scripted event list, which is all a transport has to be.
/// </summary>
/// <remarks>
/// This class is also the shortest demonstration of the framework's central claim: nothing
/// downstream of <see cref="IBitAiTransport"/> knows or cares what produced the events.
/// </remarks>
internal sealed class BitAiFakeTransport : IBitAiTransport
{
    private readonly Func<BitAiRequest, IEnumerable<BitAiEvent>> _script;

    public BitAiFakeTransport(params BitAiEvent[] events) => _script = _ => events;

    public BitAiFakeTransport(Func<BitAiRequest, IEnumerable<BitAiEvent>> script) => _script = script;

    /// <summary>Requests the transport has been given, in order.</summary>
    public List<BitAiRequest> Requests { get; } = [];

    /// <summary>A delay inserted before each event, used to test cancellation.</summary>
    public TimeSpan Delay { get; set; }

    /// <summary>Thrown instead of streaming, used to test failure handling.</summary>
    public Exception? Throw { get; set; }

    public async IAsyncEnumerable<BitAiEvent> StreamAsync(
        BitAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        if (Throw is not null) throw Throw;

        foreach (var evt in _script(request))
        {
            if (Delay > TimeSpan.Zero) await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield return evt;
        }
    }
}
