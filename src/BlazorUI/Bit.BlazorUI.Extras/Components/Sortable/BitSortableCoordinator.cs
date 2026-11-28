using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The per-scope coordinator that holds the active drag, so that several <see cref="BitSortable{TItem}"/> lists in the
/// same group can exchange items. One per scope - per circuit on Blazor Server, per application on WebAssembly, per
/// renderer scope anywhere - so two users never share a drag, and nothing has to be registered for it: the lists find
/// their scope's coordinator themselves (see <see cref="Of"/>).
/// </summary>
internal sealed class BitSortableCoordinator
{
    // Keyed weakly by the scope's IServiceProvider, which every component rendered in a scope is handed the same instance
    // of, so a scope that ends takes its coordinator with it. The coordinator holds nothing the browser has to release:
    // the script it drives is the package's own, loaded once for the page.
    private static readonly ConditionalWeakTable<IServiceProvider, BitSortableCoordinator> Coordinators = new();

    /// <summary>The coordinator of the scope <paramref name="services"/> belong to, created the first time it is asked for.</summary>
    /// <param name="services">The services of the list asking, as injected through <see cref="IServiceProvider"/>.</param>
    internal static BitSortableCoordinator Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return Coordinators.GetValue(services, static _ => new BitSortableCoordinator());
    }



    private long _lastKey;
    private int _lastDrag;

    // Tells this scope's keys from another's on the same page (a Server island next to a WebAssembly one): both count
    // from 1, and the browser layer looks an item up by its key across every list it knows.
    private readonly string _keyPrefix = BitShortId.NewId() + "-";



    internal BitSortableDragContext? Context { get; set; }

    /// <summary>
    /// How many finished drags are waiting for a destination's <c>ConfirmDrop</c>. Until they are answered no other
    /// drag starts: one that changed the same lists would be undone along with the refused one.
    /// </summary>
    internal int Confirming { get; set; }

    /// <summary>
    /// Serializes the drag handlers of all lists, so a drop position, the end of a drag and a list leaving the
    /// page mid-drag never act on each other's half-made changes. Never held while the app's own callbacks run.
    /// </summary>
    internal SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>
    /// Every list of the scope that is on the page. When a drag starts, each of them is asked whether it takes the
    /// drag, and the browser layer routes the pointer to those that do.
    /// </summary>
    internal HashSet<IBitSortableZone> Zones { get; } = new(ReferenceEqualityComparer.Instance);



    /// <summary>
    /// A key for an item, unique across every list of the scope: the browser layer names an item by it, so it never
    /// depends on an index the page may have moved on from.
    /// </summary>
    internal string NextKey() => _keyPrefix + (++_lastKey).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The id of the next drag (see <see cref="BitSortableDragContext.Id"/>); never 0, which means "the drag in progress".</summary>
    internal int NextDragId() => ++_lastDrag;

    /// <summary>
    /// The drag in progress, provided it is the one <paramref name="drag"/> names (0: whichever is in progress): a call
    /// about a drag that has since ended finds none.
    /// </summary>
    internal BitSortableDragContext? ContextOf(int drag)
        => Context is { Finished: false } ctx && (drag == 0 || ctx.Id == drag) ? ctx : null;
}
