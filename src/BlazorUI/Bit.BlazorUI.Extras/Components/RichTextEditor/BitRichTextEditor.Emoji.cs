namespace Bit.BlazorUI;

// Emoji / special-character picker.
public partial class BitRichTextEditor
{
    private bool _showEmoji;
    private string _emojiSearch = "";

    private readonly record struct EmojiEntry(string Char, string Name, string Keywords);

    private static readonly EmojiEntry[] Emoji =
    [
        new("😀", "grinning", "smile happy face"),
        new("😉", "wink", "smile face"),
        new("😍", "heart eyes", "love smile face"),
        new("👍", "thumbs up", "yes approve like"),
        new("👎", "thumbs down", "no disapprove"),
        new("🙏", "pray", "thanks please"),
        new("🎉", "party", "celebrate tada"),
        new("🔥", "fire", "hot lit"),
        new("✅", "check", "done yes ok"),
        new("❌", "cross", "no error wrong"),
        new("⭐", "star", "favorite"),
        new("❤️", "heart", "love red"),
        new("💡", "bulb", "idea light"),
        new("⚠️", "warning", "caution alert"),
        new("📌", "pin", "note important"),
        new("🚀", "rocket", "launch ship fast"),
        new("©", "copyright", "symbol"),
        new("®", "registered", "symbol trademark"),
        new("™", "trademark", "symbol"),
        new("€", "euro", "currency money"),
        new("£", "pound", "currency money"),
        new("→", "arrow right", "symbol"),
        new("←", "arrow left", "symbol"),
        new("•", "bullet", "dot symbol"),
        new("…", "ellipsis", "dots symbol"),
    ];

    private void ToggleEmoji()
    {
        _showEmoji = !_showEmoji;
        _emojiSearch = "";
    }

    private IEnumerable<EmojiEntry> FilteredEmoji()
    {
        var term = _emojiSearch?.Trim();
        if (string.IsNullOrEmpty(term)) return Emoji;
        if (term.Length > 50) term = term[..50];
        return Emoji.Where(e =>
            e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || e.Keywords.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task InsertEmojiAsync(string ch)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorInsertText(_editorRef, ch);
    }
}
