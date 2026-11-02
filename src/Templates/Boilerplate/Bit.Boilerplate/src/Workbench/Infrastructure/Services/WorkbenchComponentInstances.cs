namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The bit BlazorUI components alive in the Workbench's circuits, by the id of their root element, so Inspect mode can find
/// the component behind a clicked element. Components are held weakly; the ones that are gone are dropped now and then.
/// </summary>
public sealed class WorkbenchComponentInstances
{
    private readonly ConcurrentDictionary<string, WeakReference<BitComponentBase>> components = new(StringComparer.Ordinal);
    private int addedSincePrune;

    public void Add(BitComponentBase component)
    {
        components[component.UniqueId] = new(component);

        if (Interlocked.Increment(ref addedSincePrune) % 1000 is 0)
        {
            Prune();
        }
    }

    /// <summary>The component whose root element has the id: its unique id, or the Id parameter that replaces it.</summary>
    public BitComponentBase? Find(string elementId)
    {
        if (components.TryGetValue(elementId, out var reference) && reference.TryGetTarget(out var component))
            return component;

        return components.Values.Select(r => r.TryGetTarget(out var c) ? c : null).FirstOrDefault(c => c?.Id == elementId);
    }

    private void Prune()
    {
        foreach (var (id, reference) in components)
        {
            if (reference.TryGetTarget(out _) is false)
            {
                components.TryRemove(id, out _);
            }
        }
    }
}
