using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class _BitNavChild<TItem> : IDisposable where TItem : class
{
    private bool _disposed;
    private TItem? _registeredItem;
    private ElementReference _headerElement;
    private ElementReference _registeredElement;
    private _BitNavItemContainer? _container;



    [CascadingParameter] protected BitNav<TItem> Nav { get; set; } = default!;

    [CascadingParameter] protected _BitNavChild<TItem>? Parent { get; set; }



    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public string? Key { get; set; }

    [Parameter] public int Depth { get; set; }

    [Parameter] public RenderFragment? ChildrenFragment { get; set; }



    protected override void OnAfterRender(bool firstRender)
    {
        // The nav moves the focus between its items from the keyboard, so every item hands over the
        // element it rendered: the item's own anchor/button, or the group header button in Grouped mode.
        if (_container is not null)
        {
            RegisterItemElement(_container.Element);
        }
        else if (_headerElement.Id is not null)
        {
            RegisterItemElement(_headerElement);
        }

        base.OnAfterRender(firstRender);
    }

    // The default key of an item carries its index, so a reordered collection re-keys the very same
    // component instance onto another item: the registration of the item this instance used to render is
    // dropped first, otherwise the nav would keep focusing an element that now belongs to another item.
    // The drop only goes through while the registration still points at this instance's own element, so
    // two items swapping places do not unregister each other's freshly registered element.
    private void RegisterItemElement(ElementReference element)
    {
        if (Nav is null) return;

        if (_registeredItem is not null && ReferenceEquals(_registeredItem, Item) is false)
        {
            Nav.UnregisterItemElement(_registeredItem, _registeredElement);
        }

        _registeredItem = Item;
        _registeredElement = element;

        Nav.RegisterItemElement(Item, element);
    }



    // Method group (not a closure) so the EventCallback stays delegate-equal across renders and the DOM
    // click handler id survives re-renders; a per-render lambda gets disposed/recreated on each render,
    // making clicks that race a re-render (e.g. the swipe-trap pointerup re-render in BitNavPanel)
    // dispatch a stale handler id and silently fail on WebAssembly.
    private async Task HandleOnClick()
    {
        if (Nav is null) return;
        if (Nav.IsEnabled is false) return;
        if (Nav.GetIsEnabled(Item) is false) return;

        // The selection is read before the click is handled: the manual mode selects the clicked item right
        // here, and asking afterwards would report every freshly clicked item as the already-selected one
        // and swallow the click callback.
        var wasSelected = Nav.IsSelected(Item);

        if (Nav.GetChildItems(Item).Any() && Nav.GetUrl(Item).HasNoValue())
        {
            await ToggleItem();
        }
        else if (Nav.Mode == BitNavMode.Manual)
        {
            await Nav.SetSelectedItem(Item);
        }

        if (wasSelected is false || Nav.Reselectable)
        {
            if (Nav.GetUrl(Item).HasValue() || Nav.GetForceAnchor(Item))
            {
                await Task.Yield(); // wait for the link to navigate first
            }
            await Nav.OnItemClick.InvokeAsync(Item);
        }
    }

    private void HandleOnFocusIn()
    {
        Nav?.SetFocusedItem(Item);
    }

    // The keyboard navigation is wired to the item's own element rather than to the root of the nav, so a
    // focusable element rendered by a template (an input, a checkbox, ...) keeps its own key handling
    // instead of being read as a move through the tree.
    private Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        return Nav is null ? Task.CompletedTask : Nav.HandleOnKeyDown(Item, e);
    }

    // The chevron is a div with a button role, so Enter and Space have to be wired up by hand, as the
    // WAI-ARIA button pattern describes. The page-scrolling default of Space is cancelled by the
    // capture-phase guard in BitNav.ts, which decides on the key actually pressed instead of the
    // render-time @onkeydown:preventDefault, which lags a keystroke behind and would swallow the Tab
    // that follows an activation.
    private async Task HandleToggleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not ("Enter" or " " or "Spacebar")) return;

        await ToggleItem();
    }

    private async Task ToggleItem()
    {
        if (Nav is null) return;
        if (Nav.NoCollapse) return;
        if (Nav.IsEnabled is false) return;

        if (Nav.GetIsEnabled(Item) is false || Nav.GetChildItems(Item).Count is 0) return;

        await Nav.ToggleItem(Item);
    }

    private string GetItemContainerClasses()
    {
        var classes = new List<string>();

        if (Nav.IsEnabled is false || Nav.GetIsEnabled(Item) is false)
        {
            classes.Add("bit-nav-dis");
        }

        if (Nav.IsSelected(Item))
        {
            classes.Add("bit-nav-sel");
            if (Nav.Classes?.SelectedItemContainer is not null)
            {
                classes.Add(Nav.Classes?.SelectedItemContainer!);
            }
        }

        if (Nav.Classes?.ItemContainer is not null)
        {
            classes.Add(Nav.Classes?.ItemContainer!);
        }

        return string.Join(" ", classes);
    }

    private string GetItemContainerStyles()
    {
        var styles = new List<string>();

        if (Nav.FitWidth && Nav.IconOnly is false)
        {
            styles.Add($"padding-inline-end:{Nav.IndentPadding}px");
        }

        if (Nav.Styles?.ItemContainer is not null)
        {
            styles.Add(Nav.Styles.ItemContainer);
        }

        if (Nav.IsSelected(Item) && Nav.Styles?.SelectedItemContainer is not null)
        {
            styles.Add(Nav.Styles?.SelectedItemContainer!);
        }

        // The fragments are declarations, so they are joined with a semicolon: a space would run two of
        // them together into a single malformed declaration.
        return string.Join(";", styles);
    }

    private string GetItemClasses()
    {
        var classes = new List<string>();
        if (Nav.Classes?.Item is not null)
        {
            classes.Add(Nav.Classes?.Item!);
        }

        if (Nav.IsSelected(Item) && Nav.Classes?.SelectedItem is not null)
        {
            classes.Add(Nav.Classes?.SelectedItem!);
        }

        return string.Join(" ", classes);
    }

    private string GetItemStyles()
    {
        var styles = new List<string>();
        if (Nav.Styles?.Item is not null)
        {
            styles.Add(Nav.Styles?.Item!);
        }

        if (Nav.IsSelected(Item) && Nav.Styles?.SelectedItem is not null)
        {
            styles.Add(Nav.Styles?.SelectedItem!);
        }

        return string.Join(";", styles);
    }

    // rel="noopener noreferrer" only protects against a cross-origin target, so it is only added to the
    // links that actually leave the app: the ones carrying a scheme (https://, mailto:, ...) or a
    // protocol-relative host (//host/path).
    private static readonly Regex _AbsoluteUrlRegex = new("^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|//)", RegexOptions.Compiled);

    private static bool IsRelativeUrl(string? url) => url.HasNoValue() || _AbsoluteUrlRegex.IsMatch(url!) is false;



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
            Nav?.UnregisterItemElement(_registeredItem, _registeredElement);
        }

        _disposed = true;
    }
}
