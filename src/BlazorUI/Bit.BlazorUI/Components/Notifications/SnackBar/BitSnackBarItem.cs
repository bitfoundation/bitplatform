namespace Bit.BlazorUI;

/// <summary>
/// A class to represent each snack bar item.
/// </summary>
public class BitSnackBarItem
{
    /// <summary>
    /// The unique identifier of the snack bar item.
    /// </summary>
    public readonly Guid Id = Guid.NewGuid();

    /// <summary>
    /// The title text of the snack bar item.
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// The body text of the snack bar item.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// The color theme of the snack bar item.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Custom CSS class to apply to the snack bar item.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Custom CSS style to apply to the snack bar item.
    /// </summary>
    public string? CssStyle { get; set; }

    /// <summary>
    /// Makes this specific snack bar item non-dismissible and removes its dismiss button.
    /// </summary>
    public bool Persistent { get; set; }
}
