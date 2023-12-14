namespace Bit.BlazorUI;

public class BitSearchBoxItem
{
    /// <summary>
    /// The aria label attribute for the suggested item.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the suggested item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The id for the suggested item.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Determines if the suggested item is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }

    /// <summary>
    /// Custom CSS style for the suggested item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The text to render for the suggested item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title attribute for the suggested item.
    /// </summary>
    public string? Title { get; set; }
}
