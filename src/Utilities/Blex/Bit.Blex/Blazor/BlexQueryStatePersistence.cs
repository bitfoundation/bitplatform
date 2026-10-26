using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Bit.Blex.Query;
using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Blazor;

/// <summary>
/// Hands the queries a prerender fetched to the interactive render through Blazor's
/// <see cref="PersistentComponentState"/>, so it shows them at once instead of fetching them a second time.
/// </summary>
internal sealed class BlexQueryStatePersistence(
    PersistentComponentState state,
    BlexQueryClient client,
    BlexManager manager,
    IComponentRenderMode? renderMode) : IDisposable
{
    private const string Key = "blex.query";

    private PersistingComponentStateSubscription _subscription;
    private bool _subscribed;

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The persisted value is a JsonObject, which System.Text.Json reads with a built-in converter rather than reflection.")]
    public void TryRestore()
    {
        try
        {
            if (state.TryTakeFromJson<JsonObject>(Key, out var queries) && queries is not null)
                client.Hydrate(queries);
        }
        catch (Exception ex)
        {
            // A render optimization: the worst case without it is fetching again.
            manager.ReportError("prerender", ex, "restoring prerendered queries");
        }

        if (!_subscribed)
        {
            _subscription = state.RegisterOnPersisting(Persist, renderMode);
            _subscribed = true;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The persisted value is a JsonObject, which System.Text.Json writes with a built-in converter rather than reflection.")]
    private Task Persist()
    {
        try
        {
            var queries = client.Dehydrate();
            if (queries.Count > 0)
                state.PersistAsJson(Key, queries);
        }
        catch (Exception ex)
        {
            manager.ReportError("prerender", ex, "persisting prerendered queries");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_subscribed)
        {
            _subscription.Dispose();
            _subscribed = false;
        }
    }
}
