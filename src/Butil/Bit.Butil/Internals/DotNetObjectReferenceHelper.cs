using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Thread-safe lazy creation of a per-instance <see cref="DotNetObjectReference{TValue}"/>.
/// </summary>
/// <remarks>
/// The previous <c>_dotNetRef ??= DotNetObjectReference.Create(this)</c> idiom is a non-atomic
/// read-modify-write. Under the classic single-threaded Blazor circuit / sync-context model that is
/// fine, but multithreaded WebAssembly runtimes can run two callers concurrently, in which case two
/// references would be created and one would leak (never disposed). This helper publishes exactly one
/// reference via <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/> and disposes the redundant
/// one created by any racing loser, so the field always holds a single, correctly-tracked reference.
/// </remarks>
internal static class DotNetObjectReferenceHelper
{
    internal static DotNetObjectReference<TValue> GetOrCreate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TValue>(ref DotNetObjectReference<TValue>? field, TValue value)
        where TValue : class
    {
        var existing = Volatile.Read(ref field);
        if (existing is not null) return existing;

        var created = DotNetObjectReference.Create(value);

        // Publish only if the field is still null; otherwise another thread won the race.
        var winner = Interlocked.CompareExchange(ref field, created, null);
        if (winner is null) return created;

        // Lost the race: drop our redundant reference so it doesn't leak, and use the winner's.
        created.Dispose();
        return winner;
    }
}
