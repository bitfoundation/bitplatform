namespace Bit.BlazorUI;

/// <summary>
/// One person of a <see cref="BitAvatarGroup"/>.
/// </summary>
public class BitAvatarGroupItem
{
    /// <summary>
    /// The custom CSS classes of the avatar of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The color of the coin of the item, which takes the place of the one picked for it from its name.
    /// </summary>
    public BitColor? CoinColor { get; set; }

    /// <summary>
    /// Any data the app wants to keep with the item.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// The URL the avatar of the item links to, which makes it a link.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// The URL of the picture of the item. Without one, or while it loads, the coin shows the initials of the
    /// <see cref="Text"/>.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The initials the coin shows in place of the ones derived from the <see cref="Text"/>.
    /// </summary>
    public string? Initials { get; set; }

    /// <summary>
    /// Whether the avatar of the item is disabled, which is never clickable nor a link.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// A unique value to use as the key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The presence status of the item, drawn as a dot on the corner of its coin.
    /// </summary>
    public BitPersonaPresence Presence { get; set; }

    /// <summary>
    /// The custom CSS styles of the avatar of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The target of the link of the item (_blank, _self, ...), for an item with an <see cref="Href"/>.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The name of the item, which names its avatar to the screen readers and gives its coin its initials.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The tooltip of the avatar of the item. Default: the <see cref="Text"/>.
    /// </summary>
    public string? Title { get; set; }
}
