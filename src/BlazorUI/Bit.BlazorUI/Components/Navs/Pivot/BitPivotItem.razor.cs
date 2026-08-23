namespace Bit.BlazorUI;

/// <summary>
/// A single tab of a <see cref="BitPivot"/>: its header, which is rendered into the tablist, and the
/// content of the tabpanel that the header selects.
/// </summary>
public partial class BitPivotItem : BitComponentBase
{
    private (bool, BitVisibility, string?, string?, string?, int?, bool?) _lastHeaderState;



    [CascadingParameter] private BitPivot? Parent { get; set; }



    /// <summary>
    /// The content of the pivot item, It can be Any custom tag or a text (alias of ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The custom css class of the content of the pivot item.
    /// </summary>
    [Parameter] public string? BodyClass { get; set; }

    /// <summary>
    /// The custom css style of the content of the pivot item.
    /// </summary>
    [Parameter] public string? BodyStyle { get; set; }

    /// <summary>
    /// The content of the pivot item, It can be Any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Overrides the Dismissible of the parent pivot for this item alone, so a single tab can gain or
    /// lose its dismiss button independently of the rest.
    /// </summary>
    [Parameter] public bool? Dismissible { get; set; }

    /// <summary>
    /// The content of the pivot item header, It can be Any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the pivot item header, The text displayed of each pivot link.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// Gets or sets the icon to display next to the pivot link using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display next to the pivot link from the built-in Fluent UI icons.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Defines an optional item count displayed in parentheses just after the link text.
    /// </summary>
    [Parameter] public int? ItemCount { get; set; }

    /// <summary>
    /// Whether or not the item is selected.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsSelected { get; set; }

    /// <summary>
    /// A required key to uniquely identify a pivot item.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Callback for when this pivot item header is clicked or activated from the keyboard.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback for when the dismiss button of this pivot item is clicked, or the Delete key is
    /// pressed while it holds the focus.
    /// </summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// The title (tooltip) of the pivot item header, which is the usual place for the full text of a
    /// header that is too long to be shown in one.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    internal void SetIsSelected(bool value)
    {
        _ = AssignIsSelected(value);

        StateHasChanged();
    }

    // Part of what this item renders - its tabindex, its aria-controls - is state the pivot owns
    // rather than a parameter of its own, and Blazor skips re-rendering a child whose parameters
    // have not changed, so the pivot asks the item to render itself when that state moves.
    internal void Refresh()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-pvti";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Parent?.Classes?.HeaderItem);

        ClassBuilder.Register(() => IsSelected ? $"bit-pvti-sel {Parent?.Classes?.SelectedItem}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Parent?.Styles?.HeaderItem);

        StyleBuilder.Register(() => IsSelected ? Parent?.Styles?.SelectedItem : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        Parent?.RegisterItem(this);

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Everything the parent draws on its own behalf out of this item - the overflow menu copy of
        // the header, the roving tabindex, the dismiss button - is stale as soon as any of these
        // change, and the parent has no other way of hearing about it.
        var state = (IsEnabled, Visibility, HeaderText, IconName, Key, ItemCount, Dismissible);

        if (state == _lastHeaderState) return;

        _lastHeaderState = state;

        Parent?.Refresh();
    }

    protected override void OnVisibilityChanged(BitVisibility visibility)
    {
        Parent?.Refresh();
    }



    private async Task HandleOnClick()
    {
        if (Parent is null) return;

        await Parent.HandleItemClick(this);
    }

    private async Task HandleOnDismiss()
    {
        if (Parent is null) return;

        await Parent.HandleItemDismiss(this);
    }

    private void HandleOnFocusIn()
    {
        Parent?.HandleItemFocusIn(this);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        Parent?.UnregisterItem(this);

        await base.DisposeAsync(disposing);
    }
}
