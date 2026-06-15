using System;
using System.Threading.Tasks;

namespace Bit.Butil;

/// <summary>
/// Lightweight token returned by Butil event/observer subscriptions. Disposing the token
/// detaches the underlying JS listener so consumers can <c>await using</c> a subscription
/// for the lifetime of a component without juggling Guids.
/// </summary>
/// <remarks>
/// Disposal is idempotent and safe to call during teardown - the underlying remover is wrapped to
/// swallow a <see cref="Microsoft.JSInterop.JSDisconnectedException"/>, a cancelled interop call, or
/// an <see cref="ObjectDisposedException"/> from an already-disposed runtime.
/// The exposed <see cref="Id"/> is still useful when callers want to compose multiple
/// subscriptions and remove them in bulk.
/// </remarks>
public sealed class ButilSubscription : IAsyncDisposable
{
    private Func<ValueTask>? _remover;

    internal ButilSubscription(Guid id, Func<ValueTask> remover)
    {
        Id = id;
        _remover = remover;
    }

    /// <summary>The internal listener id (also accepted by the matching <c>Remove(Guid)</c> API).</summary>
    public Guid Id { get; }

    /// <summary>Detaches the underlying listener. Calling more than once is a no-op.</summary>
    public async ValueTask DisposeAsync()
    {
        var remover = System.Threading.Interlocked.Exchange(ref _remover, null);
        if (remover is null) return;
        try
        {
            await remover();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException())
        {
            // Teardown - circuit gone, the call was cancelled, or the runtime was already disposed.
        }
    }
}
