namespace Bit.BlazorUI;

// Character/word count and MaxLength enforcement. The count values come from the content facts
// reported by the bridge; enforcement happens in the bridge on input/paste.
public partial class BitRichTextEditor
{
    /// <summary>Show the character/word count footer.</summary>
    [Parameter] public bool ShowCount { get; set; }

    /// <summary>Maximum plain-text character count. Null means unlimited.</summary>
    [Parameter] public int? MaxLength { get; set; }
}
