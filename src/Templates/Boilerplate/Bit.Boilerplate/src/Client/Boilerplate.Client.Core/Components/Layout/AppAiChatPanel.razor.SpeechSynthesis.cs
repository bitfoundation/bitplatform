using System.Text.RegularExpressions;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout;

// The speaker half of the panel: speech out. Its counterpart is AppAiChatPanel.razor.SpeechRecognition.cs, and the
// two are deliberately not independent - only one of them may be live at a time, or the engine's own voice ends up in
// the transcript. Every crossing between them goes through StopDictation / PauseReadAloud.
public partial class AppAiChatPanel
{
    /// <summary>
    /// Shorter runs are held back and merged with whatever arrives next, so an answer that streams in a token at a
    /// time is not read out as a string of clipped fragments with the engine's gap between every one of them.
    /// </summary>
    private const int ReadAloudMinChunkLength = 40;


    [AutoInject] private SpeechSynthesis speechSynthesis = default!;


    private bool isReadAloudSupported;

    // Read aloud follows the conversation once it is switched on: the answer it was started on, and then every answer
    // that arrives after it, until the user presses stop. readAloudOffset is how much of that answer has already been
    // handed over; the engine queues what it is given and reads at its own pace, so the rate the server streams at and
    // the rate the voice speaks at never have to match.
    private bool readAloudEnabled;
    private bool readAloudPaused;
    private bool readAloudFirstRunPending;
    private int readAloudOffset;
    private string? readAloudLastLine;
    private AiChatMessage? readAloudMessage;


    /// <summary>
    /// Switches read aloud on, or off again when pressed a second time. It is a mode rather than a one-shot: from here
    /// the answer to every following prompt is read out as it streams in, so a user who is listening instead of reading
    /// does not have to reach for the button again on each turn.
    /// </summary>
    private async Task ToggleReadAloud(AiChatMessage message)
    {
        if (ReferenceEquals(readAloudMessage, message))
        {
            await StopReadAloud();
            return;
        }

        // Asking to be read to while the microphone is open is a change of mind about which of the two is wanted -
        // and the engine would be left with a run it is not allowed to speak, so a second press would read as "stop".
        await StopDictation();

        // The engine has a single queue, so a press on another message replaces what is being read rather than
        // overlapping two answers. A press is answered at once, unlike the switch to an answer that has not arrived
        // yet, where waiting is what keeps the silence short.
        await speechSynthesis.Cancel();

        readAloudEnabled = true;

        FollowReadAloud(message);

        readAloudFirstRunPending = false; // The queue was just emptied, so this message's first run has nothing to drop.

        // An answer still arriving is read as far as it has got and picked up again from ReadAloudArrivedContent;
        // one that is already complete has nothing more coming, so all of it goes to the engine now.
        await ReadAloudArrivedContent(final: ReferenceEquals(message, lastAssistantMessage) is false || isLoading is false);
    }

    /// <summary>Points read aloud at <paramref name="message"/>, with none of it read yet.</summary>
    private void FollowReadAloud(AiChatMessage message)
    {
        readAloudPaused = false;
        readAloudFirstRunPending = true;
        readAloudOffset = 0;
        readAloudLastLine = null;
        readAloudMessage = message;
    }

    /// <summary>
    /// Hands the engine whatever of the followed answer has arrived and is safe to read. Markdown only says what it
    /// means once its construct is closed - the closing fence of a code block decides whether the lines above it are
    /// read at all - so the text is cut at a boundary the clean-up in <see cref="ToSpeakableText"/> can be trusted
    /// across. <paramref name="final"/> takes everything that is left, the answer being known to be complete by then.
    /// </summary>
    private async Task ReadAloudArrivedContent(bool final)
    {
        // isListening covers what readAloudPaused cannot: it is the microphone being open right now, whatever the mode
        // was doing when it opened. Nothing is said while the user is being recorded - the engine's own voice is what
        // the recognizer would hear, and it would end up in the message the user is dictating.
        if (readAloudEnabled is false || readAloudPaused || isListening || readAloudMessage is null) return;

        var content = readAloudMessage.Content ?? string.Empty;

        if (content.Length <= readAloudOffset) return;

        var end = final ? content.Length : EndOfSpeakableContent(content, readAloudOffset);
        var length = end - readAloudOffset;

        if (length <= 0 || (final is false && length < ReadAloudMinChunkLength)) return;

        var chunk = content.Substring(readAloudOffset, length);
        readAloudOffset = end;

        if (readAloudFirstRunPending)
        {
            readAloudFirstRunPending = false;

            // The user has moved on and the answer to what they asked next is ready, so the rest of the previous one
            // is dropped instead of being played out first - by the time it finished, nobody would still want it.
            await speechSynthesis.Cancel();
        }

        var text = ToSpeakableText(chunk, readAloudLastLine);

        if (text.Length is 0) return; // Nothing but markup arrived, which is silence rather than an empty utterance.

        // The repeated-line check spans the whole answer, not just this run of it: a product arrives titled and then
        // linked with the same words, and the two land in separate runs when they arrive a moment apart.
        readAloudLastLine = text[(text.LastIndexOf('\n') + 1)..];

        await speechSynthesis.Speak(new SpeechUtterance
        {
            Text = text,
            VoiceName = GetVoiceName(),
            Lang = CultureInfoManager.InvariantGlobalization is false ? CultureInfoManager.DefaultCulture.Name : null
        });
    }

    // Sample for customizing the voice name based on the platform and language. The default voice is used if this returns null.
    private string? GetVoiceName()
    {
        if (CultureInfoManager.InvariantGlobalization is true || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is not "en")
            return null;

        if (TelemetryContext.Platform?.Contains("Windows", StringComparison.OrdinalIgnoreCase) is true)
            return "Microsoft Ava Online (Natural) - English (United States)";

        return null;
    }

    /// <summary>
    /// Silences the engine but leaves the mode on, so the answer to what the user is about to say is read out without
    /// them having to ask for it again. The rest of the answer that was cut off is not read: they talked over it.
    /// </summary>
    private async Task PauseReadAloud()
    {
        if (readAloudEnabled is false || readAloudPaused) return;

        readAloudPaused = true;

        await speechSynthesis.Cancel();
    }

    private async Task StopReadAloud()
    {
        if (readAloudEnabled is false) return;

        readAloudEnabled = false;
        readAloudPaused = false;
        readAloudFirstRunPending = false;
        readAloudOffset = 0;
        readAloudLastLine = null;
        readAloudMessage = null;

        await speechSynthesis.Cancel();
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

    /// <summary>
    /// Answers are markdown and a synthesizer reads the syntax out loud - "star star bit platform star star" - so the
    /// markers go and links are reduced to their label. What carries nothing once heard is dropped rather than spelled
    /// out: code blocks, image markup, urls, and emoji - a five star rating is five spoken words in front of the number
    /// that already says it, and a check mark becomes "check mark button" in front of every line it decorates.
    /// </summary>
    /// <param name="markdown">The answer, or one run of it when it is being read while it streams in.</param>
    /// <param name="previousLine">
    /// The last line already read out, when <paramref name="markdown"/> is a run of an answer whose earlier runs have
    /// been read: the repeated-line check below has to see across the seam, or the pair it exists for is split by it.
    /// </param>
    private static string ToSpeakableText(string markdown, string? previousLine = null)
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

            var preceding = lines.Count > 0 ? lines[^1] : previousLine;

            if (string.Equals(preceding, spoken, StringComparison.OrdinalIgnoreCase)) continue;

            lines.Add(spoken);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// How far into <paramref name="content"/>, from <paramref name="from"/>, the text can be cut without cutting a
    /// markdown construct in half. A whole line is the unit that survives the clean-up intact, so the last one that
    /// has arrived ends the run; a paragraph that arrives as one long line would stay silent until it ended, so a
    /// finished sentence - a terminator with something after it, which "3.5" and "e.g." never have - ends one too.
    /// Returns <paramref name="from"/> when nothing can be cut yet.
    /// </summary>
    internal static int EndOfSpeakableContent(string content, int from)
    {
        // Everything under an unclosed fence is code that may never be read out, and whether it is read is decided by
        // a fence that has not arrived, so the answer is only settled up to where that block starts.
        var limit = StartOfUnclosedCodeFence(content) ?? content.Length;

        if (limit <= from) return from;

        var lineEnd = content.LastIndexOf('\n', limit - 1, limit - from);

        if (lineEnd >= 0) return lineEnd + 1;

        for (var i = limit - 1; i > from; i--)
        {
            if (content[i] is ' ' or '\t' && content[i - 1] is '.' or '!' or '?' or '؟' or '۔' or '。')
                return i;
        }

        return from;
    }

    /// <summary>Where the code block that is still open begins, or null when none is.</summary>
    private static int? StartOfUnclosedCodeFence(string content)
    {
        int? start = null;

        for (var index = 0; index < content.Length;)
        {
            var lineEnd = content.IndexOf('\n', index);
            var line = (lineEnd < 0 ? content[index..] : content[index..lineEnd]).TrimStart();

            if (line.StartsWith("```", StringComparison.Ordinal) || line.StartsWith("~~~", StringComparison.Ordinal))
            {
                start = start is null ? index : null;
            }

            if (lineEnd < 0) break;

            index = lineEnd + 1;
        }

        return start;
    }
}
