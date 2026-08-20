using System.Text.RegularExpressions;

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>
/// Turns the markdown an answer is written in into the words a synthesizer should say, and cuts the result into
/// pieces a provider will take. It lives on this side rather than in the AI chat panel because it belongs to whoever
/// is about to speak the text: the panel sends what the model wrote, and <c>ChatbotController.SynthesizeSpeech</c>
/// decides what of it is worth hearing and how much of it to say at a time.
/// </summary>
public static partial class SpeakableText
{
    /// <summary>
    /// The longest run a synthesizer takes in one request: OpenAI's speech endpoint refuses more than 4096
    /// characters and the providers copying its shape copy that too. It bounds the spoken words, not the markdown
    /// they came from - the syntax, code blocks and urls are gone by the time it is measured - so an answer several
    /// times this long usually still fits in one request.
    /// </summary>
    private const int MaxProviderInputLength = 4096;

    /// <summary>
    /// Answers are markdown and a synthesizer reads the syntax out loud - "star star bit platform star star" - so the
    /// markers go and links are reduced to their label. What carries nothing once heard is dropped rather than spelled
    /// out: code blocks, image markup, urls, and emoji - a five star rating is five spoken words in front of the number
    /// that already says it, and a check mark becomes "check mark button" in front of every line it decorates.
    /// <para>
    /// The result is empty when the markdown held nothing but the above, which is silence rather than an empty
    /// utterance - the caller answers with no content instead of paying for it.
    /// </para>
    /// </summary>
    public static string FromMarkdown(string markdown)
    {
        var text = FencedCodeRegex().Replace(markdown.ReplaceLineEndings("\n"), " ");
        text = MarkdownImageRegex().Replace(text, string.Empty);
        text = MarkdownLinkRegex().Replace(text, "$1");
        text = UrlRegex().Replace(text, string.Empty);
        text = EmojiRegex().Replace(text, string.Empty);
        // An underscore is emphasis around a word and the joint inside an identifier both, and a space serves either:
        // dropping it outright would hand the synthesizer "snakecasename" as one unpronounceable word.
        text = text.Replace('_', ' ');
        text = MarkdownMarkerRegex().Replace(text, string.Empty);
        text = MarkdownRuleRegex().Replace(text, string.Empty);
        text = TableCellSeparatorRegex().Replace(text, ", ");
        text = ExtraSpaceRegex().Replace(text, " ");

        // A product arrives titled, pictured and linked with the same words, so its name would be read three times over
        // - the picture is gone by now and the two that are left are neighbouring lines. Lines emptied by the markup
        // that was all they held go too, so the reading is not paced by pauses around nothing.
        List<string> lines = [];
        foreach (var line in text.Split('\n'))
        {
            var spoken = line.Trim(' ', '\t', ',');

            if (spoken.Length is 0) continue;

            if (lines.Count > 0 && string.Equals(lines[^1], spoken, StringComparison.OrdinalIgnoreCase)) continue;

            lines.Add(spoken);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Cuts speakable text into pieces a synthesizer will accept, at the last line break - or failing that the last
    /// space - that fits, so a piece never ends mid-word. A line break is preferred because it is where a paragraph
    /// ended, and a voice that pauses there pauses where the writing did.
    /// <para>
    /// Nearly every answer comes out as a single piece. The splitting is here rather than in a client because which
    /// limit applies is the provider's business.
    /// </para>
    /// </summary>
    public static IEnumerable<string> Segment(string speakable)
    {
        var text = speakable.Trim();

        while (text.Length > MaxProviderInputLength)
        {
            var window = text[..MaxProviderInputLength];

            var cut = window.LastIndexOf('\n');

            if (cut <= 0)
            {
                cut = window.LastIndexOf(' ');
            }

            // A single unbroken run longer than the limit - one enormous "word" - has no boundary to respect, so it
            // is cut where the limit falls rather than being dropped.
            if (cut <= 0)
            {
                cut = MaxProviderInputLength;
            }

            var segment = text[..cut].Trim();

            // A piece of nothing but whitespace is silence, and the provider rejects an empty input.
            if (segment.Length > 0)
            {
                yield return segment;
            }

            text = text[cut..].TrimStart();
        }

        if (text.Length > 0)
        {
            yield return text;
        }
    }


    [GeneratedRegex(@"(?:```|~~~)[\s\S]*?(?:```|~~~)")]
    private static partial Regex FencedCodeRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"<[a-z][a-z0-9+.-]*://[^>]*>|(?:https?://|www\.)\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^[ \t]{0,3}(?:>+[ \t]*|(?:#{1,6}|[-*+\u2022]|\d+[.)])[ \t]+)|[*~`]", RegexOptions.Multiline)]
    private static partial Regex MarkdownMarkerRegex();

    /// <summary>Horizontal rules and the dashes under a table header, which are a line of punctuation and nothing else.</summary>
    [GeneratedRegex(@"^[-=:| \t]{3,}$", RegexOptions.Multiline)]
    private static partial Regex MarkdownRuleRegex();

    [GeneratedRegex(@"(?:[ \t]*\|[ \t]*)+")]
    private static partial Regex TableCellSeparatorRegex();

    /// <summary>
    /// The dingbat and arrow blocks (U+2600-27BF, U+2B00-2BFF and friends) plus U+1F000-1FBFF as surrogate pairs,
    /// since .NET matches one utf-16 unit at a time and \p{So} would never see an astral character. FE0F, 200D and
    /// 20E3 are the joiners emoji sequences are built from, and are left behind when their pictograph goes.
    /// </summary>
    [GeneratedRegex(@"[\u2190-\u21FF\u231A-\u231B\u23E9-\u23FA\u24C2\u25AA-\u25FE\u2600-\u27BF\u2B00-\u2BFF\uFE0F\u200D\u20E3]|[\uD83C-\uD83E][\uDC00-\uDFFF]")]
    private static partial Regex EmojiRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex ExtraSpaceRegex();
}
