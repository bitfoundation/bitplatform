using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class _BitNavBarItem<TItem> : IDisposable where TItem : class
{
    private bool _disposed;
    private bool _replaced;
    private TItem? _registeredItem;
    private _BitNavBarChild? _child;
    private ElementReference _registeredElement;



    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitNavBar<TItem> NavBar { get; set; } = default!;



    protected override void OnAfterRender(bool firstRender)
    {
        // An item rendered by a template of its own in the Replace mode has no anchor or button of the
        // navbar's to hand over, so the registration it used to hold (while it was rendered normally) is
        // dropped instead of being left pointing at an element that is gone.
        if (_replaced)
        {
            DropItemElement();
        }
        // The navbar moves the focus between its items from the keyboard, so every item hands over the
        // anchor or the button it rendered.
        else if (_child is not null)
        {
            RegisterItemElement(_child.Element);
        }

        base.OnAfterRender(firstRender);
    }

    // The default key of an item carries its index, so a reordered collection re-keys the very same
    // component instance onto another item: the registration of the item this instance used to render is
    // dropped first, otherwise the navbar would keep focusing an element that now belongs to another item.
    // The drop only goes through while the registration still points at this instance's own element, so
    // two items swapping places do not unregister each other's freshly registered element.
    private void RegisterItemElement(ElementReference element)
    {
        if (NavBar is null) return;

        if (_registeredItem is not null && ReferenceEquals(_registeredItem, Item) is false)
        {
            NavBar.UnregisterItemElement(_registeredItem, _registeredElement);
        }

        _registeredItem = Item;
        _registeredElement = element;

        NavBar.RegisterItemElement(Item, element);
    }

    private void DropItemElement()
    {
        if (_registeredItem is null) return;

        NavBar?.UnregisterItemElement(_registeredItem, _registeredElement);

        _registeredItem = null;
        _registeredElement = default;
    }



    // Method group (not a closure) so the EventCallback stays delegate-equal across renders and the DOM
    // click handler id survives re-renders; a per-render lambda gets disposed/recreated on each render,
    // making clicks that race a re-render dispatch a stale handler id and silently fail on WebAssembly.
    private Task HandleOnClick()
    {
        return NavBar is null ? Task.CompletedTask : NavBar.HandleOnClick(Item);
    }

    private Task HandleOnFocusIn()
    {
        return NavBar is null ? Task.CompletedTask : NavBar.HandleOnFocusIn(Item);
    }

    private Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        return NavBar is null ? Task.CompletedTask : NavBar.HandleOnKeyDown(Item, e);
    }



    // rel="noopener noreferrer" only protects against a cross-origin target, so it is only added to the
    // links that actually leave the app: the ones carrying a scheme (https://, mailto:, ...) or a
    // protocol-relative host (//host/path).
    private static readonly Regex _AbsoluteUrlRegex = new("^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|//)", RegexOptions.Compiled);

    private static bool IsRelativeUrl(string? url) => url.HasNoValue() || _AbsoluteUrlRegex.IsMatch(url!) is false;



    // A custom item type reads its aria-current through a selector, which can hand over a value that is not
    // one of the enum's own, so an unknown one falls back to the default rather than tearing the item down.
    private static string GetAriaCurrent(BitNavAriaCurrent ariaCurrent)
    {
        return _AriaCurrentMap.TryGetValue(ariaCurrent, out var value) ? value : _AriaCurrentMap[BitNavAriaCurrent.Page];
    }

    private static readonly Dictionary<BitNavAriaCurrent, string> _AriaCurrentMap = new()
    {
        [BitNavAriaCurrent.Page] = "page",
        [BitNavAriaCurrent.Step] = "step",
        [BitNavAriaCurrent.Location] = "location",
        [BitNavAriaCurrent.Time] = "time",
        [BitNavAriaCurrent.Date] = "date",
        [BitNavAriaCurrent.True] = "true"
    };



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_registeredItem is not null)
        {
            NavBar?.UnregisterItemElement(_registeredItem, _registeredElement);
        }

        _disposed = true;
    }
}
