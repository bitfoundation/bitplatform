using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Parses JSON that is still being written.
/// </summary>
/// <remarks>
/// <para>
/// Models emit JSON a token at a time, so for most of a generation the payload is syntactically
/// invalid. Waiting for the closing brace means a table or a form appears all at once at the end.
/// This class repairs a truncated document into the closest valid one, which is what lets the UI
/// fill in fields as they arrive.
/// </para>
/// <para>
/// The repair closes open containers, drops an incomplete trailing key or literal, and keeps a
/// partially written string value so that text fields grow character by character rather than
/// appearing whole. It never invents data: every value in the result was present in the input.
/// </para>
/// </remarks>
public static class BitAiPartialJson
{
    /// <summary>
    /// Parses complete JSON, returning <see langword="null"/> instead of throwing on invalid input.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed value, or <see langword="null"/> when the text is not valid JSON.</returns>
    public static JsonElement? TryParseComplete(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            // Text cut between the halves of a surrogate pair cannot be transcoded to UTF-8.
            return null;
        }
    }

    /// <summary>
    /// Parses JSON that may be truncated, repairing it first when needed.
    /// </summary>
    /// <param name="json">The JSON text received so far.</param>
    /// <returns>
    /// The parsed value, or <see langword="null"/> when nothing usable has arrived yet.
    /// </returns>
    public static JsonElement? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return TryParseComplete(json) ?? TryParseComplete(Repair(json));
    }

    /// <summary>
    /// Repairs truncated JSON into the closest valid document.
    /// </summary>
    /// <param name="json">The JSON text received so far.</param>
    /// <returns>Valid JSON text, or <see langword="null"/> when nothing usable has arrived yet.</returns>
    public static string? Repair(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var frames = new List<Frame>(8);
        var lastGood = -1;
        var lastGoodDepth = 0;
        var inString = false;
        var stringIsKey = false;
        var index = 0;

        void MarkComplete(int position, int depth)
        {
            lastGood = position;
            lastGoodDepth = depth;
        }

        while (index < json.Length)
        {
            var c = json[index];

            if (inString)
            {
                if (c == '\\')
                {
                    index += 2;
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    index++;
                    if (!stringIsKey) MarkComplete(index, frames.Count);
                    continue;
                }

                index++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    stringIsKey = frames.Count > 0 && frames[^1].IsObject && !frames[^1].AfterColon;
                    index++;
                    break;

                case '{':
                case '[':
                    frames.Add(new Frame(c == '{', false));
                    index++;
                    MarkComplete(index, frames.Count);
                    break;

                case '}':
                case ']':
                    if (frames.Count > 0) frames.RemoveAt(frames.Count - 1);
                    index++;
                    MarkComplete(index, frames.Count);
                    break;

                case ':':
                    if (frames.Count > 0) frames[^1] = frames[^1] with { AfterColon = true };
                    index++;
                    break;

                case ',':
                    if (frames.Count > 0) frames[^1] = frames[^1] with { AfterColon = false };
                    index++;
                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    index++;
                    break;

                default:
                {
                    var start = index;
                    while (index < json.Length && !IsDelimiter(json[index])) index++;
                    var token = json.AsSpan(start, index - start);
                    if (index < json.Length || IsCompleteScalar(token)) MarkComplete(index, frames.Count);
                    break;
                }
            }
        }

        if (inString && !stringIsKey)
        {
            var text = TrimDanglingEscape(json.AsSpan());
            return Close(text, frames, frames.Count, closeString: true);
        }

        if (lastGood < 0) return null;
        return Close(json.AsSpan(0, lastGood), frames, lastGoodDepth, closeString: false);
    }

    private static string Close(ReadOnlySpan<char> text, List<Frame> frames, int depth, bool closeString)
    {
        var builder = new StringBuilder(text.Length + depth + 1);
        builder.Append(text);
        if (closeString) builder.Append('"');
        for (var i = depth - 1; i >= 0; i--) builder.Append(frames[i].IsObject ? '}' : ']');
        return builder.ToString();
    }

    private static ReadOnlySpan<char> TrimDanglingEscape(ReadOnlySpan<char> text)
    {
        if (text.Length > 0 && IsEscapeStart(text, text.Length - 1)) text = text[..^1];

        // A \u escape still missing some of its four hex digits. Only a backslash that starts an
        // escape counts: in "\\u12" the backslash before the u is itself escaped.
        for (var length = 2; length <= 5 && length <= text.Length; length++)
        {
            var start = text.Length - length;
            if (text[start + 1] == 'u' && IsEscapeStart(text, start))
            {
                text = text[..start];
                break;
            }
        }

        // A high surrogate whose low half has not arrived, escaped or raw, cannot be read as a string.
        if (text.Length >= 6
            && text[^5] == 'u'
            && IsEscapeStart(text, text.Length - 6)
            && ushort.TryParse(text[^4..], System.Globalization.NumberStyles.AllowHexSpecifier,
                System.Globalization.CultureInfo.InvariantCulture, out var unit)
            && char.IsHighSurrogate((char)unit))
        {
            text = text[..^6];
        }
        else if (text.Length > 0 && char.IsHighSurrogate(text[^1]))
        {
            text = text[..^1];
        }

        return text;
    }

    private static bool IsEscapeStart(ReadOnlySpan<char> text, int index)
    {
        if (text[index] != '\\') return false;
        var preceding = 0;
        for (var i = index - 1; i >= 0 && text[i] == '\\'; i--) preceding++;
        return preceding % 2 == 0;
    }

    private static bool IsDelimiter(char c) =>
        c is ',' or '}' or ']' or ':' or ' ' or '\t' or '\r' or '\n' or '"';

    private static bool IsCompleteScalar(ReadOnlySpan<char> token)
    {
        if (token.IsEmpty) return false;
        if (token.SequenceEqual("true") || token.SequenceEqual("false") || token.SequenceEqual("null")) return true;
        return IsJsonNumber(token);
    }

    /// <remarks>
    /// Stricter than <see cref="double.TryParse(ReadOnlySpan{char}, out double)"/>, which accepts
    /// <c>1.</c>, <c>NaN</c>, and <c>Infinity</c>. Keeping any of those makes the repaired document
    /// invalid, and the whole value would disappear until the next digit arrives.
    /// </remarks>
    private static bool IsJsonNumber(ReadOnlySpan<char> token)
    {
        var i = 0;
        if (token[i] == '-') i++;
        if (i >= token.Length || !char.IsAsciiDigit(token[i])) return false;
        if (token[i] == '0') i++;
        else while (i < token.Length && char.IsAsciiDigit(token[i])) i++;

        if (i < token.Length && token[i] == '.')
        {
            var start = ++i;
            while (i < token.Length && char.IsAsciiDigit(token[i])) i++;
            if (i == start) return false;
        }

        if (i < token.Length && token[i] is 'e' or 'E')
        {
            i++;
            if (i < token.Length && token[i] is '+' or '-') i++;
            var start = i;
            while (i < token.Length && char.IsAsciiDigit(token[i])) i++;
            if (i == start) return false;
        }

        return i == token.Length;
    }

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly record struct Frame(bool IsObject, bool AfterColon);
}
