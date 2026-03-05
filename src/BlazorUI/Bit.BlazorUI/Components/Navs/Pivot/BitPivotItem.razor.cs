namespace Bit.BlazorUI;

public partial class BitPivotItem : BitComponentBase
{
    private bool _isEnabled;



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



    internal void SetIsSelected(bool value)
    {
        _ = AssignIsSelected(value);

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

        if (IsEnabled == _isEnabled) return;

        _isEnabled = IsEnabled;
        Parent?.Refresh();
    }

    protected override void OnVisibilityChanged(BitVisibility visibility)
    {
        Parent?.Refresh();
    }



    private async Task HandleOnClick()
    {
        if (IsEnabled is false || Parent is null || Parent.IsEnabled is false) return;

        Parent.SelectItem(this);
        await Parent.OnItemClick.InvokeAsync(this);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        Parent?.UnregisterItem(this);

        await base.DisposeAsync(disposing);
    }
}
