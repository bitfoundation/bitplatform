
namespace Bit.BlazorUI;

public partial class BitPivotItem : IDisposable
{
    private bool isSelected;
    private bool _disposed;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (value == isSelected) return;
            isSelected = value;
            ClassBuilder.Reset();
        }
    }


    /// <summary>
    /// The content of the pivot item can be Any custom tag or a text, If HeaderContent provided value of this parameter show, otherwise use ChildContent
    /// </summary>
    [Parameter] public RenderFragment? BodyTemplate { get; set; }

    /// <summary>
    /// The content of the pivot item, It can be Any custom tag or a text 
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content of the pivot item header, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The text of the pivot item header, The text displayed of each pivot link
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// The icon name for the icon shown next to the pivot link
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Defines an optional item count displayed in parentheses just after the linkText
    /// </summary>
    [Parameter] public int? ItemCount { get; set; }

    /// <summary>
    /// The parent BitPivot component instance
    /// </summary>
    [CascadingParameter] public BitPivot? Pivot { get; set; }

    /// <summary>
    /// A required key to uniquely identify a pivot item
    /// </summary>
    [Parameter] public string? Key { get; set; }

    protected override string RootElementClass => "bit-pvt-itm";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsSelected ? $"{RootElementClass}-selcted-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (Pivot is not null)
        {
            Pivot.RegisterItem(this);
        }

        await base.OnInitializedAsync();
    }

    internal void SetState(bool status)
    {
        IsSelected = status;
        StateHasChanged();
    }

    private void HandleButtonClick()
    {
        if (IsEnabled is false) return;

        Pivot?.SelectItem(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (Pivot is not null)
        {
            Pivot.UnregisterItem(this);
        }

        _disposed = true;
    }
}
