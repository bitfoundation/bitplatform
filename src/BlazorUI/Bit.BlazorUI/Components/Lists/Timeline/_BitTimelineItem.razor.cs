namespace Bit.BlazorUI;

public partial class _BitTimelineItem<TItem> where TItem : class
{
    private ElementReference _buttonElement = default!;
    private string? _buttonKeysElementId;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitTimeline<TItem> Timeline { get; set; } = default!;



    // The clickable item is not a real button, so the keys a button is activated by (Enter as it goes down,
    // Space as it comes back up, neither one scrolling the page) are wired up in the browser: there a key
    // pressed on the item itself can be told apart from one typed into a control of a custom template,
    // which a Blazor keydown handler cannot see. The button is a new element each time the item becomes
    // clickable again, so it is registered again whenever the element it holds changes.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Timeline.IsItemInteractive(Item) is false)
        {
            _buttonKeysElementId = null;
            return;
        }

        if (_buttonElement.Id is { } id && id != _buttonKeysElementId)
        {
            _buttonKeysElementId = id;

            await _js.BitUtilsRegisterButtonKeys(_buttonElement);
        }
    }
}
