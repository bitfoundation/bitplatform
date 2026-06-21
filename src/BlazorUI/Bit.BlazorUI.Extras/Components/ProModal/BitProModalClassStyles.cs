namespace Bit.BlazorUI;

public class BitProModalClassStyles : BitModalClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the header container of the BitProModal.
    /// </summary>
    public string? HeaderContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header of the BitProModal.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the close button of the BitProModal.
    /// </summary>
    public string? CloseButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the close icon of the BitProModal.
    /// </summary>
    public string? CloseIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitProModal.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitProModal.
    /// </summary>
    public string? Footer { get; set; }


    public static BitProModalClassStyles? Merge(BitProModalClassStyles? classStyles1, BitProModalClassStyles? classStyles2)
    {
        if (classStyles1 is null && classStyles2 is null) return null;

        return new BitProModalClassStyles
        {
            Root = classStyles1?.Root ?? classStyles2?.Root,
            Overlay = classStyles1?.Overlay ?? classStyles2?.Overlay,
            Content = classStyles1?.Content ?? classStyles2?.Content,
            HeaderContainer = classStyles1?.HeaderContainer ?? classStyles2?.HeaderContainer,
            Header = classStyles1?.Header ?? classStyles2?.Header,
            CloseButton = classStyles1?.CloseButton ?? classStyles2?.CloseButton,
            CloseIcon = classStyles1?.CloseIcon ?? classStyles2?.CloseIcon,
            Body = classStyles1?.Body ?? classStyles2?.Body,
            Footer = classStyles1?.Footer ?? classStyles2?.Footer,
        };
    }
}
