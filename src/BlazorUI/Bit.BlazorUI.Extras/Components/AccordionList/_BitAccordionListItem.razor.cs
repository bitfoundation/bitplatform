namespace Bit.BlazorUI;

public partial class _BitAccordionListItem<TItem> : ComponentBase, IDisposable where TItem : class
{
    private bool _skipRender;
    private TItem? _registeredItem;
    private BitAccordion? _accordion;

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitAccordionList<TItem> AccordionList { get; set; } = default!;



    internal ValueTask FocusAsync() => _accordion?.FocusAsync() ?? ValueTask.CompletedTask;

    // The root element of the accordion the item renders, which is the box the list scrolls into view: the
    // wrapper around it generates none of its own (display: contents) and could not be scrolled to.
    internal ElementReference? GetElement() => _accordion?.RootElement;



    protected override void OnParametersSet()
    {
        // A render the list asks for is never the one the keyboard bookkeeping below is trying to skip.
        _skipRender = false;

        base.OnParametersSet();
    }

    // The keydown handler on the wrapper is bookkeeping for the list rather than a state change of this item,
    // so the render Blazor runs after every one of its event handlers is skipped: without this, every key
    // pressed on a header - Tab included - would re-render the whole item and its panel for nothing.
    protected override bool ShouldRender()
    {
        if (_skipRender is false) return true;

        _skipRender = false;

        return false;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // The list keeps a reference of every item it renders so that it can move the focus to it. The item
        // itself can be swapped for another one on a later render, so the old registration is dropped first.
        if (ReferenceEquals(_registeredItem, Item) is false)
        {
            if (_registeredItem is not null)
            {
                AccordionList.UnregisterItem(_registeredItem);
            }

            _registeredItem = Item;

            AccordionList.RegisterItem(Item, this);
        }

        base.OnAfterRender(firstRender);
    }

    private RenderFragment? BuildActions()
    {
        var actions = AccordionList.GetItemActions(Item);

        if (actions is null) return null;

        // The actions sit outside of the panel, so they need a stop of their own to keep the keys pressed on
        // whatever they hold - a menu, a switch - from reaching the navigation handler on the wrapper.
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "bit-acl-act");
            builder.AddEventStopPropagationAttribute(2, "onkeydown", true);
            builder.AddContent(3, actions);
            builder.CloseElement();
        };
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        _skipRender = true;

        // A panel that scrolls (MaxHeight) is a tab stop of its own, and the arrow keys pressed on it are its
        // own scroll. The stop inside it never sees them - they are fired on the box that holds the content
        // rather than inside it - so the navigation steps aside for as long as the panel holds the focus,
        // rather than moving the reader twice: once down the list and once down the panel.
        if (_accordion?.IsContentFocused is true) return;

        await AccordionList.HandleOnItemKeyDown(e, Item);
    }

    public void Dispose()
    {
        if (_registeredItem is not null)
        {
            AccordionList?.UnregisterItem(_registeredItem);
            _registeredItem = null;
        }

        GC.SuppressFinalize(this);
    }
}
