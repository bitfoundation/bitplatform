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

    /// <summary>
    /// The footer's reading of the current content. Each case is a full localizable template, so a
    /// translator controls word order and pluralization instead of receiving "words" and "chars"
    /// as separate words to be glued onto a number - which is what made an editor holding one word
    /// read "1 words".
    /// </summary>
    private string CountLabel()
    {
        var words = _facts.WordCount == 1
            ? string.Format(Label("word-count", "{0} word"), _facts.WordCount)
            : string.Format(Label("words-count", "{0} words"), _facts.WordCount);

        var characters = MaxLength is int max
            ? string.Format(Label("chars-count-max", "{0}/{1} chars"), _facts.CharacterCount, max)
            : _facts.CharacterCount == 1
                ? string.Format(Label("char-count", "{0} char"), _facts.CharacterCount)
                : string.Format(Label("chars-count", "{0} chars"), _facts.CharacterCount);

        return $"{words} · {characters}";
    }

    /// <summary>
    /// The plain-text character count of the current content, as the footer and
    /// <see cref="MaxLength"/> count it. Zero before the editor has loaded.
    /// </summary>
    public int CharacterCount => _facts.CharacterCount;

    /// <summary>
    /// The word count of the current content. Zero before the editor has loaded.
    /// </summary>
    public int WordCount => _facts.WordCount;

    /// <summary>
    /// Whether the editor holds nothing a reader would see - no text and none of the elements that
    /// are content without carrying text (images, tables, rules, media). Reading this is cheaper
    /// and steadier than inspecting the bound HTML, which the browser pads with a stray line break
    /// while editing.
    /// </summary>
    public bool IsEmpty => _facts.IsEmpty;
}
