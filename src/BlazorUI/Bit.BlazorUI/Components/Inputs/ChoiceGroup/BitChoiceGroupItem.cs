namespace Bit.BlazorUI;

public class BitChoiceGroupItem<TValue>
{
    /// <summary>
    /// AriaLabel attribute for the BitChoiceGroup item.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// CSS class attribute for the BitChoiceGroup item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Id attribute of the BitChoiceGroup item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Whether the BitChoiceGroup item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The icon to show as content of the BitChoiceGroup item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The image address to show as the content of the BitChoiceGroup item.
    /// </summary>
    public string? ImageSrc { get; set; }

    /// <summary>
    /// The alt attribute for the image of the BitChoiceGroup item.
    /// </summary>
    public string? ImageAlt { get; set; }

    /// <summary>
    /// Provides Width and Height for the image of the BitChoiceGroup item.
    /// </summary>
    public BitImageSize? ImageSize { get; set; }

    /// <summary>
    /// The text to show as a prefix for the BitChoiceGroup item.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Provides a new image for the selected state of the image of the BitChoiceGroup item.
    /// </summary>
    public string? SelectedImageSrc { get; set; }

    /// <summary>
    /// CSS style attribute for the BitChoiceGroup item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template for the BitChoiceGroup item.
    /// </summary>
    public RenderFragment<BitChoiceGroupItem<TValue>>? Template { get; set; }

    /// <summary>
    /// Text to show as the content of BitChoiceGroup item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The value returned when BitChoiceGroup item is checked.
    /// </summary>
    public TValue? Value { get; set; }



    /// <summary>
    /// Index of the BitChoiceGroup item. This property's value is set by the component at render.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    /// Determines if the item is selected. This property's value is assigned by the component.
    /// </summary>
    public bool IsSelected { get; internal set; }
}
