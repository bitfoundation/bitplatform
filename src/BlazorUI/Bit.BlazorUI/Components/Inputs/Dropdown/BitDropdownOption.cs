namespace Bit.BlazorUI;

public partial class BitDropdownOption<TValue> : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitDropdown<BitDropdownOption<TValue>, TValue> Parent { get; set; } = default!;


    /// <summary>
    /// The aria label attribute for the dropdown option.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the dropdown option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The id for the dropdown option.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The custom data for the dropdown item to provide extra state for the template.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// Determines if the dropdown option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines if the dropdown option is hidden.
    /// </summary>
    [Parameter] public bool IsHidden { get; set; }

    /// <summary>
    /// The type of the dropdown option.
    /// </summary>
    [Parameter] public BitDropdownItemType ItemType { get; set; } = BitDropdownItemType.Normal;

    /// <summary>
    /// Custom CSS style for the dropdown option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The text to render for the dropdown option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title attribute for the dropdown option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The value of the dropdown option.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }



    /// <summary>
    /// Determines if the option is selected. This property's value is assigned by the component.
    /// </summary>
    public bool IsSelected { get; internal set; }



    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Parent.UnregisterOption(this);

        _disposed = true;
    }
}
