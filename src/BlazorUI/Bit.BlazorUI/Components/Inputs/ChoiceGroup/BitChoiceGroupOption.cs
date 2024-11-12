namespace Bit.BlazorUI;

public partial class BitChoiceGroupOption<TValue> : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitChoiceGroup<BitChoiceGroupOption<TValue>, TValue> Parent { get; set; } = default!;

    /// <summary>
    /// AriaLabel attribute for the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// CSS class attribute for the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Id attribute of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Whether the BitChoiceGroup option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The icon to show as content of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The image address to show as the content of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? ImageSrc { get; set; }

    /// <summary>
    /// The alt attribute for the image of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// Provides Width and Height for the image of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public BitImageSize? ImageSize { get; set; }

    /// <summary>
    /// The text to show as a prefix for the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Provides a new image for the selected state of the image of the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? SelectedImageSrc { get; set; }

    /// <summary>
    /// CSS style attribute for the BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The custom template for the BitChoiceGroup option.
    /// </summary>
    [Parameter] public RenderFragment<BitChoiceGroupOption<TValue>>? Template { get; set; }

    /// <summary>
    /// Text to show as the content of BitChoiceGroup option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// This value is returned when BitChoiceGroup option is checked.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }



    /// <summary>
    /// Index of the BitChoiceGroup option. This property's value is set by the component at render.
    /// </summary>
    public int Index { get; internal set; }

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
