namespace Bit.BlazorUI;

public class BitModalClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitModal.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay of the BitModal.
    /// </summary>
    public string? Overlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the modal content of the BitModal.
    /// </summary>
    public string? Content { get; set; }


    public static BitModalClassStyles Merge(BitModalClassStyles? classStyles1, BitModalClassStyles? classStyles2)
    {
        return new BitModalClassStyles
        {
            Root = classStyles1?.Root ?? classStyles2?.Root,
            Overlay = classStyles1?.Overlay ?? classStyles2?.Overlay,
            Content = classStyles1?.Content ?? classStyles2?.Content
        };
    }
}
