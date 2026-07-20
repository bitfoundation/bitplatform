using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Replaces <c>:shortcode:</c> emoji in text with the corresponding Unicode glyph.
/// Unknown shortcodes are left untouched. The built-in map can be extended per instance
/// by passing overrides to the constructor (see <see cref="BitMarkdownEmojiAstProcessor(IReadOnlyDictionary{string, string})"/>).
/// </summary>
public sealed partial class BitMarkdownEmojiAstProcessor : BitMarkdownAstProcessor
{
    [GeneratedRegex(@":([a-z0-9_+\-]+):", RegexOptions.IgnoreCase)]
    private static partial Regex Shortcode();

    /// <summary>The built-in, read-only shortcode-to-glyph defaults.</summary>
    private static readonly IReadOnlyDictionary<string, string> DefaultEmoji =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["smile"] = "😄", ["grin"] = "😁", ["laughing"] = "😆", ["wink"] = "😉",
            ["blush"] = "😊", ["heart"] = "❤️", ["thumbsup"] = "👍", ["+1"] = "👍",
            ["thumbsdown"] = "👎", ["-1"] = "👎", ["tada"] = "🎉", ["rocket"] = "🚀",
            ["fire"] = "🔥", ["star"] = "⭐", ["sparkles"] = "✨", ["zap"] = "⚡",
            ["bug"] = "🐛", ["bulb"] = "💡", ["books"] = "📚", ["memo"] = "📝",
            ["warning"] = "⚠️", ["white_check_mark"] = "✅", ["x"] = "❌",
            ["eyes"] = "👀", ["wave"] = "👋", ["clap"] = "👏", ["pray"] = "🙏",
            ["100"] = "💯", ["check"] = "✔️", ["question"] = "❓", ["exclamation"] = "❗",
            ["coffee"] = "☕", ["computer"] = "💻", ["package"] = "📦", ["lock"] = "🔒",
            ["key"] = "🔑", ["wrench"] = "🔧", ["hammer"] = "🔨", ["gear"] = "⚙️",
            ["snake"] = "🐍", ["cat"] = "🐱", ["dog"] = "🐶", ["sun"] = "☀️", ["moon"] = "🌙",
        };

    // An immutable per-instance snapshot. Because a pipeline is shared across concurrent
    // parses/circuits, the lookup must never be mutated after construction.
    private readonly IReadOnlyDictionary<string, string> _emoji;

    /// <summary>Creates a processor that uses the built-in emoji map.</summary>
    public BitMarkdownEmojiAstProcessor() => _emoji = DefaultEmoji;

    /// <summary>
    /// Creates a processor whose lookup is the built-in map plus the supplied
    /// <paramref name="overrides"/>. The overrides are snapshotted into an immutable
    /// dictionary, so later changes to the source collection do not affect this instance
    /// or leak across pipelines/circuits.
    /// </summary>
    public BitMarkdownEmojiAstProcessor(IReadOnlyDictionary<string, string> overrides)
    {
        var map = new Dictionary<string, string>(DefaultEmoji, StringComparer.OrdinalIgnoreCase);
        if (overrides is not null)
        {
            foreach (var kv in overrides) map[kv.Key] = kv.Value;
        }
        _emoji = map;
    }

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var text in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownTextNode>())
        {
            if (text.Text.IndexOf(':') < 0) continue;
            text.Text = Shortcode().Replace(text.Text, m =>
                _emoji.TryGetValue(m.Groups[1].Value, out var glyph) ? glyph : m.Value);
        }
    }
}
