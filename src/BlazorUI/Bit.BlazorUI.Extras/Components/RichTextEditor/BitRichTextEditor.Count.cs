namespace Bit.BlazorUI;

// Character/word count and MaxLength enforcement. The count values come from the content facts
// reported by the bridge; enforcement happens in the bridge on input/paste.
public partial class BitRichTextEditor
{
    /// <summary>Show the character/word count footer.</summary>
    [Parameter] public bool ShowCount { get; set; }

    private int? _maxLength;
    /// <summary>
    /// Maximum plain-text character count. Null means unlimited. Negative values are rejected
    /// and treated as null (unlimited) so the footer and bridge never receive an invalid limit.
    /// </summary>
    [Parameter]
    public int? MaxLength
    {
        get => _maxLength;
        set => _maxLength = value is < 0 ? null : value;
    }
}
