using System.Linq;
using Bit.Blex;

namespace Bit.Blex.Samples.Wasm.Stores;

/// <summary>
/// A todo store demonstrating normalized collection state via <see cref="BlexEntityAdapter{TEntity, TKey}"/>
/// (the Redux Toolkit <c>createEntityAdapter</c> pattern), computed counts and an <c>[BlexEffectAttribute]</c>
/// with a generated loading/error lifecycle (<c>SeedIsLoading</c>/<c>SeedError</c>).
/// </summary>
[BlexStoreAttribute(Name = "todos")]
public partial class TodoStore
{
    private static readonly BlexEntityAdapter<TodoItem, Guid> Adapter = new(t => t.Id);

    [BlexStateAttribute] private BlexEntityState<TodoItem, Guid> _items = Adapter.GetInitialState();

    [BlexComputedAttribute]
    private int ComputeRemaining() => Items.All.Count(i => !i.Done);

    [BlexComputedAttribute]
    private int ComputeTotal() => Items.Count;

    [BlexActionAttribute]
    private void OnAdd(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            Items = Adapter.AddOne(Items, new TodoItem(Guid.NewGuid(), text.Trim(), false));
    }

    [BlexActionAttribute]
    private void OnToggle(Guid id)
        => Items = Adapter.UpdateOne(Items, id, i => i with { Done = !i.Done });

    [BlexActionAttribute]
    private void OnRemove(Guid id)
        => Items = Adapter.RemoveOne(Items, id);

    [BlexActionAttribute]
    private void OnClearCompleted()
        => Items = Adapter.RemoveMany(Items, Items.All.Where(i => i.Done).Select(i => i.Id).ToList());

    [BlexActionAttribute]
    private void OnCompleteAll()
        => Items = Adapter.Map(Items, i => i with { Done = true });

    /// <summary>
    /// An effect: the generator emits <c>SeedSampleData()</c>, plus reactive
    /// <c>SeedSampleDataIsLoading</c> and <c>SeedSampleDataError</c> properties -- no hand-rolled
    /// loading flag needed. The whole body records as one time-travelable action.
    /// </summary>
    [BlexEffectAttribute(Name = "SeedSampleData")]
    private async Task OnSeedSampleData()
    {
        await Task.Delay(400); // pretend to call an API
        Items = Adapter.SetAll(Items,
        [
            new TodoItem(Guid.NewGuid(), "Try Blex time-travel in DevTools", false),
            new TodoItem(Guid.NewGuid(), "Star the repo", false),
            new TodoItem(Guid.NewGuid(), "Delete Fluxor boilerplate", true),
        ]);
    }
}

/// <summary>An immutable todo item. Records serialize cleanly for snapshots and DevTools.</summary>
public sealed record TodoItem(Guid Id, string Text, bool Done);
