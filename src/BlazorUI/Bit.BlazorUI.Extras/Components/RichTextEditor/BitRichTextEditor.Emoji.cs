namespace Bit.BlazorUI;

// Emoji / special-character picker.
public partial class BitRichTextEditor
{
    // A search term longer than this cannot match any entry name or keyword, so it is clamped
    // rather than being run over the whole table.
    private const int MaxEmojiSearchLength = 50;

    private bool _showEmoji;
    private string _emojiSearch = "";
    private ElementReference _emojiSearchRef = default!;

    private readonly record struct EmojiEntry(string Char, string Name, string Keywords, string Group);

    // Group keys are localization keys as well, so a Localizer can translate the headings.
    private const string EmojiGroupSmileys = "emoji-group-smileys";
    private const string EmojiGroupGestures = "emoji-group-gestures";
    private const string EmojiGroupNature = "emoji-group-nature";
    private const string EmojiGroupObjects = "emoji-group-objects";
    private const string EmojiGroupSymbols = "emoji-group-symbols";

    private static readonly EmojiEntry[] Emoji =
    [
        // Smileys & people
        new("😀", "grinning", "smile happy face", EmojiGroupSmileys),
        new("😃", "smiley", "smile happy joy face", EmojiGroupSmileys),
        new("😄", "smile", "happy laugh joy face", EmojiGroupSmileys),
        new("😁", "beaming", "grin smile happy face", EmojiGroupSmileys),
        new("😂", "tears of joy", "laugh lol funny cry", EmojiGroupSmileys),
        new("🙂", "slight smile", "happy face", EmojiGroupSmileys),
        new("😉", "wink", "smile face flirt", EmojiGroupSmileys),
        new("😊", "blush", "smile happy shy face", EmojiGroupSmileys),
        new("😍", "heart eyes", "love smile face", EmojiGroupSmileys),
        new("😘", "kiss", "love heart face", EmojiGroupSmileys),
        new("🤔", "thinking", "hmm consider face", EmojiGroupSmileys),
        new("🤗", "hug", "hands smile face", EmojiGroupSmileys),
        new("😐", "neutral", "meh face", EmojiGroupSmileys),
        new("😑", "expressionless", "blank meh face", EmojiGroupSmileys),
        new("🙄", "eye roll", "annoyed face", EmojiGroupSmileys),
        new("😴", "sleeping", "tired sleep zzz face", EmojiGroupSmileys),
        new("😅", "sweat smile", "nervous relief face", EmojiGroupSmileys),
        new("😎", "sunglasses", "cool face", EmojiGroupSmileys),
        new("🤩", "star struck", "wow excited face", EmojiGroupSmileys),
        new("😢", "cry", "sad tear face", EmojiGroupSmileys),
        new("😭", "sob", "cry sad tears face", EmojiGroupSmileys),
        new("😡", "angry", "mad rage face", EmojiGroupSmileys),
        new("😱", "scream", "shock fear face", EmojiGroupSmileys),
        new("🤯", "mind blown", "explode shock face", EmojiGroupSmileys),
        new("😬", "grimace", "awkward face", EmojiGroupSmileys),
        new("🥳", "partying", "celebrate party face", EmojiGroupSmileys),
        new("🤝", "handshake", "deal agree partner", EmojiGroupSmileys),
        new("👋", "wave", "hello hi bye hand", EmojiGroupSmileys),

        // Gestures & body
        new("👍", "thumbs up", "yes approve like ok", EmojiGroupGestures),
        new("👎", "thumbs down", "no disapprove dislike", EmojiGroupGestures),
        new("👏", "clap", "applause bravo hands", EmojiGroupGestures),
        new("🙌", "raised hands", "celebrate praise hooray", EmojiGroupGestures),
        new("🙏", "pray", "thanks please hands", EmojiGroupGestures),
        new("💪", "muscle", "strong flex arm", EmojiGroupGestures),
        new("👌", "ok hand", "perfect fine", EmojiGroupGestures),
        new("✌️", "victory", "peace fingers", EmojiGroupGestures),
        new("🤞", "fingers crossed", "luck hope", EmojiGroupGestures),
        new("👉", "point right", "this direction hand", EmojiGroupGestures),
        new("👈", "point left", "this direction hand", EmojiGroupGestures),
        new("☝️", "point up", "attention note hand", EmojiGroupGestures),
        new("🖐️", "open hand", "stop wave", EmojiGroupGestures),
        new("👀", "eyes", "look watch see", EmojiGroupGestures),

        // Nature & food
        new("🔥", "fire", "hot lit flame", EmojiGroupNature),
        new("⭐", "star", "favorite rate", EmojiGroupNature),
        new("🌟", "glowing star", "sparkle shine", EmojiGroupNature),
        new("✨", "sparkles", "shine magic new", EmojiGroupNature),
        new("⚡", "lightning", "fast bolt power", EmojiGroupNature),
        new("🌈", "rainbow", "color pride", EmojiGroupNature),
        new("☀️", "sun", "sunny weather", EmojiGroupNature),
        new("☁️", "cloud", "weather cloudy", EmojiGroupNature),
        new("❄️", "snowflake", "cold winter snow", EmojiGroupNature),
        new("🌱", "seedling", "grow plant new", EmojiGroupNature),
        new("🌍", "globe", "earth world planet", EmojiGroupNature),
        new("🐛", "bug", "insect defect issue", EmojiGroupNature),
        new("🚀", "rocket", "launch ship fast deploy", EmojiGroupNature),
        new("☕", "coffee", "drink cafe break", EmojiGroupNature),
        new("🍕", "pizza", "food slice", EmojiGroupNature),
        new("🎂", "cake", "birthday celebrate", EmojiGroupNature),

        // Objects
        new("💡", "bulb", "idea light tip", EmojiGroupObjects),
        new("📌", "pin", "note important", EmojiGroupObjects),
        new("📎", "paperclip", "attach file", EmojiGroupObjects),
        new("📝", "memo", "note write edit", EmojiGroupObjects),
        new("📁", "folder", "directory files", EmojiGroupObjects),
        new("📅", "calendar", "date schedule", EmojiGroupObjects),
        new("📊", "bar chart", "graph stats data", EmojiGroupObjects),
        new("📈", "chart up", "growth increase trend", EmojiGroupObjects),
        new("📉", "chart down", "decline decrease trend", EmojiGroupObjects),
        new("🔍", "magnifier", "search find zoom", EmojiGroupObjects),
        new("🔒", "lock", "secure private closed", EmojiGroupObjects),
        new("🔑", "key", "password access", EmojiGroupObjects),
        new("⏰", "alarm", "time clock reminder", EmojiGroupObjects),
        new("⏳", "hourglass", "wait time pending", EmojiGroupObjects),
        new("💻", "laptop", "computer code dev", EmojiGroupObjects),
        new("📱", "phone", "mobile device", EmojiGroupObjects),
        new("🔧", "wrench", "fix tool repair", EmojiGroupObjects),
        new("🛠️", "tools", "build fix maintenance", EmojiGroupObjects),
        new("🎯", "target", "goal aim bullseye", EmojiGroupObjects),
        new("🎉", "party popper", "celebrate tada launch", EmojiGroupObjects),
        new("🏆", "trophy", "win award prize", EmojiGroupObjects),
        new("🎁", "gift", "present box", EmojiGroupObjects),
        new("📚", "books", "docs read library", EmojiGroupObjects),
        new("🔗", "link", "url chain", EmojiGroupObjects),

        // Symbols & punctuation
        new("✅", "check", "done yes ok complete", EmojiGroupSymbols),
        new("☑️", "ballot check", "done task complete", EmojiGroupSymbols),
        new("❌", "cross", "no error wrong fail", EmojiGroupSymbols),
        new("⚠️", "warning", "caution alert danger", EmojiGroupSymbols),
        new("🚫", "prohibited", "no forbidden ban", EmojiGroupSymbols),
        new("❗", "exclamation", "important alert", EmojiGroupSymbols),
        new("❓", "question", "help ask unknown", EmojiGroupSymbols),
        new("💯", "hundred", "perfect score full", EmojiGroupSymbols),
        new("❤️", "heart", "love red like", EmojiGroupSymbols),
        new("💔", "broken heart", "sad breakup", EmojiGroupSymbols),
        new("♻️", "recycle", "reuse green", EmojiGroupSymbols),
        new("©", "copyright", "symbol legal", EmojiGroupSymbols),
        new("®", "registered", "symbol trademark legal", EmojiGroupSymbols),
        new("™", "trademark", "symbol legal", EmojiGroupSymbols),
        new("€", "euro", "currency money", EmojiGroupSymbols),
        new("£", "pound", "currency money", EmojiGroupSymbols),
        new("¥", "yen", "currency money", EmojiGroupSymbols),
        new("¢", "cent", "currency money", EmojiGroupSymbols),
        new("§", "section", "symbol legal clause", EmojiGroupSymbols),
        new("¶", "pilcrow", "paragraph symbol", EmojiGroupSymbols),
        new("†", "dagger", "footnote symbol", EmojiGroupSymbols),
        new("→", "arrow right", "symbol next", EmojiGroupSymbols),
        new("←", "arrow left", "symbol back previous", EmojiGroupSymbols),
        new("↑", "arrow up", "symbol above", EmojiGroupSymbols),
        new("↓", "arrow down", "symbol below", EmojiGroupSymbols),
        new("⇒", "double arrow", "implies symbol", EmojiGroupSymbols),
        new("•", "bullet", "dot symbol list", EmojiGroupSymbols),
        new("·", "middle dot", "separator symbol", EmojiGroupSymbols),
        new("…", "ellipsis", "dots symbol continue", EmojiGroupSymbols),
        new("–", "en dash", "hyphen range symbol", EmojiGroupSymbols),
        new("—", "em dash", "hyphen break symbol", EmojiGroupSymbols),
        new("×", "multiplication", "times symbol math", EmojiGroupSymbols),
        new("÷", "division", "divide symbol math", EmojiGroupSymbols),
        new("±", "plus minus", "tolerance symbol math", EmojiGroupSymbols),
        new("≈", "approximately", "about symbol math", EmojiGroupSymbols),
        new("≠", "not equal", "different symbol math", EmojiGroupSymbols),
        new("≤", "less or equal", "symbol math", EmojiGroupSymbols),
        new("≥", "greater or equal", "symbol math", EmojiGroupSymbols),
        new("°", "degree", "temperature angle symbol", EmojiGroupSymbols),
        new("½", "one half", "fraction symbol", EmojiGroupSymbols),
        new("¼", "one quarter", "fraction symbol", EmojiGroupSymbols),
        new("¾", "three quarters", "fraction symbol", EmojiGroupSymbols),
        new("α", "alpha", "greek symbol", EmojiGroupSymbols),
        new("β", "beta", "greek symbol", EmojiGroupSymbols),
        new("π", "pi", "greek symbol math", EmojiGroupSymbols),
        new("µ", "micro", "greek mu symbol", EmojiGroupSymbols),
        new("Ω", "omega", "greek ohm symbol", EmojiGroupSymbols),
        new("∞", "infinity", "endless symbol math", EmojiGroupSymbols),
        new("√", "square root", "radical symbol math", EmojiGroupSymbols),
        new("∑", "sum", "sigma total symbol math", EmojiGroupSymbols),
        new("«", "left guillemet", "quote symbol", EmojiGroupSymbols),
        new("»", "right guillemet", "quote symbol", EmojiGroupSymbols),
        new("“", "left quote", "curly quotation symbol", EmojiGroupSymbols),
        new("”", "right quote", "curly quotation symbol", EmojiGroupSymbols),
    ];

    private async Task ToggleEmoji()
    {
        _showEmoji = !_showEmoji;
        _emojiSearch = "";
        if (_showEmoji)
        {
            await CloseOtherPanels("emoji");
            RequestPanelFocus(() => _emojiSearchRef);
        }
        // Clear any stale inline validation message when opening or closing the picker, matching
        // the other inline tool toggles (e.g. ToggleFind) so old errors don't linger.
        ClearInlineError();
    }

    private IEnumerable<EmojiEntry> FilteredEmoji()
    {
        var term = _emojiSearch?.Trim();
        if (string.IsNullOrEmpty(term)) return Emoji;
        if (term.Length > MaxEmojiSearchLength) term = term[..MaxEmojiSearchLength];
        return Emoji.Where(e =>
            e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || e.Keywords.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // The picker renders one section per group so a long table stays scannable; the grouping is
    // computed from the filtered set, so searching collapses to just the groups that still match.
    private IEnumerable<IGrouping<string, EmojiEntry>> FilteredEmojiGroups()
        => FilteredEmoji().GroupBy(e => e.Group);

    private static string DefaultEmojiGroupLabel(string key) => key switch
    {
        EmojiGroupSmileys => "Smileys & people",
        EmojiGroupGestures => "Gestures",
        EmojiGroupNature => "Nature & food",
        EmojiGroupObjects => "Objects",
        _ => "Symbols"
    };

    private async Task InsertEmojiAsync(string ch)
    {
        // Block insertion whenever the toolbar controls are disabled (ReadOnly or source view
        // active), matching the find/replace guard, so emoji can't be written into the live
        // editor while the rendered DOM and raw source text are meant to stay in sync.
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorInsertText(_editorRef, ch);
    }
}
