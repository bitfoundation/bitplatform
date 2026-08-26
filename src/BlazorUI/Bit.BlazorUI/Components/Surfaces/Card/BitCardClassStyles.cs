namespace Bit.BlazorUI;

/// <summary>
/// The custom CSS classes or styles of the parts of the <see cref="BitCard"/> component.
/// </summary>
public class BitCardClassStyles
{
    /// <summary>
    /// The custom CSS class/style for the root element of the BitCard.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// The custom CSS class/style for the stretched link that covers a card with an Href.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// The custom CSS class/style for the full-bleed media area at the head of the card.
    /// </summary>
    public string? Cover { get; set; }

    /// <summary>
    /// The custom CSS class/style for the image rendered from the ImageUrl of the card.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// The custom CSS class/style for the element that holds the header, the body and the footer of the card.
    /// </summary>
    public string? Main { get; set; }

    /// <summary>
    /// The custom CSS class/style for the header of the card.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// The custom CSS class/style for the leading icon of the header of the card.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The custom CSS class/style for the element that holds the title and the subtitle of the card.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// The custom CSS class/style for the title of the card.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The custom CSS class/style for the subtitle of the card.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// The custom CSS class/style for the actions rendered at the trailing edge of the header of the card.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// The custom CSS class/style for the body of the card, which is what the ChildContent renders into.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// The custom CSS class/style for the footer of the card.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// The custom CSS class/style for the root element of the card while it is selected.
    /// </summary>
    public string? Selected { get; set; }
}
