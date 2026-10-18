namespace Bit.Butil.Demo.Client.Services;

/// <summary>
/// Backs the side rail. Sections are the pages' own content, so they cannot be enumerated up
/// front: every <see cref="Shared.DemoSection"/> registers itself here as it initializes, and
/// the rail re-renders when they do.
/// </summary>
/// <remarks>
/// The registration carries the path it belongs to and the list resets when a new one arrives,
/// rather than being cleared by a LocationChanged handler. Both the router and the rail listen to
/// that event, and nothing orders their handlers relative to each other - a clear that ran after
/// the incoming page's sections had registered would wipe exactly the entries it was meant to
/// collect. Keying on the path cannot lose that race.
/// </remarks>
public class PageOutline
{
    private readonly List<PageOutlineItem> _items = [];

    /// <summary>The path the current <see cref="Items"/> were registered for.</summary>
    public string? Path { get; private set; }

    /// <summary>
    /// A snapshot: <see cref="Register"/> runs while sections initialize, i.e. in the middle of a
    /// render the rail may already be walking this list, and handing out the live one would let it
    /// grow underneath the enumerator.
    /// </summary>
    public IReadOnlyList<PageOutlineItem> Items => [.. _items];

    public event Action? Changed;

    public void Register(string path, PageOutlineItem item)
    {
        if (path != Path)
        {
            Path = path;
            _items.Clear();
        }

        // Re-renders re-run OnInitialized for a section that was disposed and recreated in place;
        // the rail must not grow a second copy of it.
        if (_items.Any(i => i.Slug == item.Slug)) return;

        _items.Add(item);

        Changed?.Invoke();
    }
}
