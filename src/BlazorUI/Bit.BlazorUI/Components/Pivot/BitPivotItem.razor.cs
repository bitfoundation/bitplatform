namespace Bit.BlazorUI;

public partial class BitPivotItem : IDisposable
{
    protected override bool UseVisual => false;

    private bool IsSelectedHasBeenSet;
    private bool isSelected;

    private bool _disposed;
    private bool _isEnabled;

    /// <summary>
    /// The content of the pivot item, It can be Any custom tag or a text (alias of ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

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
    /// The icon name for the icon shown next to the pivot link.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Defines an optional item count displayed in parentheses just after the link text.
    /// </summary>
    [Parameter] public int? ItemCount { get; set; }

    /// <summary>
    /// Whether or not the item is selected.
    /// </summary>
    [Parameter]
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (value == isSelected) return;

            isSelected = value;
            _ = IsSelectedChanged.InvokeAsync(value);
            ClassBuilder.Reset();
        }
    }
    [Parameter] public EventCallback<bool> IsSelectedChanged { get; set; }

    /// <summary>
    /// A required key to uniquely identify a pivot item.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    
    [CascadingParameter] private BitPivot? Parent { get; set; }


    protected override string RootElementClass => "bit-pvi";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsSelected ? "selected" : string.Empty);
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

    internal void SetIsSelected(bool value)
    {
        IsSelected = value;
        StateHasChanged();
    }

    private async Task HandleOnClick()
    {
        if (IsEnabled is false || Parent is null || Parent.IsEnabled is false) return;

        Parent.SelectItem(this);
        await Parent.OnItemClick.InvokeAsync(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        Parent?.UnregisterItem(this);

        _disposed = true;
    }
}
