namespace Bit.BlazorUI;

public class BitPersonaClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitPersona.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the coin container of the BitPersona.
    /// </summary>
    public string? CoinContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the presentation icon of the BitPersona.
    /// </summary>
    public string? PresentationIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the presence dot of the BitPersona at <see cref="BitPersonaSize.Size8"/>.
    /// </summary>
    /// <remarks>
    /// Kept for backward compatibility. <see cref="Presence"/> is applied to the presence dot at every size,
    /// including <see cref="BitPersonaSize.Size8"/>, and is what new code should use.
    /// </remarks>
    public string? Presentation { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image container of the BitPersona.
    /// </summary>
    public string? ImageContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the unknown icon of the BitPersona.
    /// </summary>
    public string? UnknownIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the coin icon of the BitPersona, which is the icon shown inside the
    /// coin in place of the initials.
    /// </summary>
    public string? CoinIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image overlay of the BitPersona.
    /// </summary>
    public string? ImageOverlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image overlay text of the BitPersona.
    /// </summary>
    public string? ImageOverlayText { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the initials of the BitPersona.
    /// </summary>
    public string? Initials { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image of the BitPersona.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the action button of the BitPersona.
    /// </summary>
    public string? ActionButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the action button icon of the BitPersona.
    /// </summary>
    public string? ActionButtonIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the presence dot of the BitPersona.
    /// </summary>
    public string? Presence { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the details container of the BitPersona.
    /// </summary>
    public string? DetailsContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the primary text container of the BitPersona.
    /// </summary>
    public string? PrimaryTextContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the secondary text container of the BitPersona.
    /// </summary>
    public string? SecondaryTextContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the tertiary text container of the BitPersona.
    /// </summary>
    public string? TertiaryTextContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the optional text container of the BitPersona.
    /// </summary>
    public string? OptionalTextContainer { get; set; }
}
