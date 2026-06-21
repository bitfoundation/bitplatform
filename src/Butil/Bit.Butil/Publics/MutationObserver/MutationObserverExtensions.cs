using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Extension methods that wire <c>MutationObserver</c> onto an <see cref="ElementReference"/>.
/// </summary>
public static class MutationObserverExtensions
{
    /// <summary>
    /// Observes mutations on the given element. The handler receives every record batch.
    /// Use the returned <see cref="ButilSubscription"/> to stop observing.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MutationRecord))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MutationObserverOptions))]
    public static async Task<ButilSubscription> ObserveMutations(
        this ElementReference element,
        IJSRuntime js,
        Action<MutationRecord[]> handler,
        MutationObserverOptions? options = null)
    {
        // Default to a "watch everything" config matching the most common Blazor use case
        // (watching for nodes being added/removed inside a region).
        options ??= new MutationObserverOptions { ChildList = true, Subtree = true };

        var host = new MutationObserverInterop(handler);
        var listenerId = Guid.NewGuid();

        await js.InvokeVoid("BitButil.mutationObserver.observe",
            host.DotNetRef,
            listenerId,
            element,
            options);

        return new ButilSubscription(listenerId, async () =>
        {
            try { await js.InvokeVoid("BitButil.mutationObserver.unobserve", listenerId); }
            finally { host.Dispose(); }
        });
    }
}
