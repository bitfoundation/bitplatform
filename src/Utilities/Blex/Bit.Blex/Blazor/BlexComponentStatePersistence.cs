using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Blazor;

/// <summary>
/// Bridges Blex stores to Blazor's <see cref="PersistentComponentState"/> so that state produced
/// during prerendering is handed to the interactive render without re-fetching, eliminating the
/// "double render" flicker. This is distinct from durable <see cref="IBlexStorage"/> persistence:
/// it only survives the prerender-to-interactive transition within a single page load.
/// </summary>
/// <remarks>
/// Applies to every registered store (the goal is render handoff, not selective durability). Stores
/// are restored eagerly in <see cref="TryRestore"/> and re-persisted via a registered callback.
/// </remarks>
public sealed class BlexComponentStatePersistence : IDisposable
{
    private const string KeyPrefix = "blex.cs:";

    private readonly PersistentComponentState _state;
    private readonly IReadOnlyList<IBlexStore> _stores;
    private readonly BlexManager? _manager;
    private readonly IComponentRenderMode? _renderMode;
    private PersistingComponentStateSubscription _subscription;
    private bool _subscribed;

    /// <summary>Creates the bridge over the framework state service and the registered stores.</summary>
    /// <param name="state">The framework's prerender state service.</param>
    /// <param name="stores">The stores to hand across the prerender boundary.</param>
    /// <param name="manager">
    /// The manager whose <see cref="BlexManager.OnError"/> receives per-store failures. Optional, but
    /// without it a failure can only go to stderr.
    /// </param>
    /// <param name="renderMode">
    /// The render mode the state is handed to - the provider's own. A Blazor Web App that registers more
    /// than one interactive render mode cannot infer it for a callback registered outside a component's own
    /// <c>RegisterOnPersisting</c> call, and fails the whole prerendered response when it has to; passing it
    /// makes the handoff work whichever modes the app registers.
    /// </param>
    public BlexComponentStatePersistence(
        PersistentComponentState state,
        IEnumerable<IBlexStore> stores,
        BlexManager? manager = null,
        IComponentRenderMode? renderMode = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        _stores = stores as IReadOnlyList<IBlexStore> ?? stores.ToList();
        _manager = manager;
        _renderMode = renderMode;
    }

    /// <summary>
    /// Restores any state captured during prerendering into the matching stores and registers the
    /// callback that re-persists them. Call once during the provider's initialization.
    /// </summary>
    /// <remarks>
    /// Failures are contained per store and reported through <see cref="BlexManager.OnError"/>, the
    /// way every other restore path in Blex behaves. This one runs inside the provider's
    /// initialization, so an escaping exception would take down the whole application at startup --
    /// over a render optimization whose worst-case fallback is simply rendering twice.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The persisted value is a JsonObject, which System.Text.Json reads with a built-in converter rather than reflection.")]
    public void TryRestore()
    {
        foreach (var store in _stores)
        {
            if (store is not BlexStoreBase sb)
                continue;

            try
            {
                if (_state.TryTakeFromJson<JsonObject>(KeyPrefix + store.Name, out var slice) && slice is not null)
                    sb.ApplyRestoredState(slice);
            }
            catch (Exception ex)
            {
                Report(ex, $"restoring prerendered '{store.Name}'");
            }
        }

        if (!_subscribed)
        {
            _subscription = _state.RegisterOnPersisting(PersistAll, _renderMode);
            _subscribed = true;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The persisted value is a JsonObject, which System.Text.Json writes with a built-in converter rather than reflection.")]
    private Task PersistAll()
    {
        foreach (var store in _stores)
        {
            try
            {
                _state.PersistAsJson(KeyPrefix + store.Name, store.SerializeState());
            }
            catch (Exception ex)
            {
                // This is a framework callback; throwing here would fail the whole persist pass and
                // strand every store behind this one.
                Report(ex, $"persisting prerendered '{store.Name}'");
            }
        }

        return Task.CompletedTask;
    }

    private void Report(Exception exception, string detail)
    {
        if (_manager is { } manager)
            manager.ReportError("prerender", exception, detail);
        else
            Console.Error.WriteLine($"[Blex] {detail}: {exception.Message}");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_subscribed)
        {
            _subscription.Dispose();
            _subscribed = false;
        }
    }
}
