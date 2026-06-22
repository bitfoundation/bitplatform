using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Markdown.Parsing;
using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI.Markdown.Extensions;

/// <summary>
/// Replaces <c>:shortcode:</c> emoji in text with the corresponding Unicode glyph.
/// Unknown shortcodes are left untouched. The map can be extended via <see cref="Emoji"/>.
/// </summary>
public sealed partial class EmojiAstProcessor : AstProcessor
{
    [GeneratedRegex(@":([a-z0-9_+\-]+):", RegexOptions.IgnoreCase)]
    private static partial Regex Shortcode();

    /// <summary>The shortcode-to-glyph map. Thread-safe so callers can add their own.</summary>
    public static readonly ConcurrentDictionary<string, string> Emoji = new(StringComparer.OrdinalIgnoreCase)
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

    public override void Process(DocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var text in AstHelper.Descendants(document).OfType<TextNode>())
        {
            if (text.Text.IndexOf(':') < 0) continue;
            text.Text = Shortcode().Replace(text.Text, m =>
                Emoji.TryGetValue(m.Groups[1].Value, out var glyph) ? glyph : m.Value);
        }
    }
}

/// <summary>Enables <c>:shortcode:</c> emoji replacement.</summary>
public sealed class EmojiExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new EmojiAstProcessor());
}
