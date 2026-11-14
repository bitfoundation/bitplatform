using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Lays a value out over a mask, and reads it back out of a text the mask has been laid over: the engine behind the
/// formatting of BitPhoneInput and of the Mask of BitTextField.
/// </summary>
/// <remarks>
/// A mask is a run of slots and literals. A slot is a character of the mask that names a token - '#', say - and takes
/// one character of the raw value that the token accepts (a digit, for '#'); every other character of the mask is a
/// literal, inserted for the user. With an escape character, the character after it is a literal even when it names a
/// token (<c>\A</c> is the letter A itself), and the escape character is not written. The raw value holds only what went
/// into the slots; the formatted text, the raw value with the literals laid around it.
/// </remarks>
internal static class BitMask
{
    /// <summary>The character that makes the next character of a mask of BitTextField a literal.</summary>
    public const char Escape = '\\';

    /// <summary>The tokens of a mask whose only slot is '#', a digit: the masks of BitPhoneInput.</summary>
    public static readonly IReadOnlyDictionary<char, Func<char, bool>> DigitTokens = new Dictionary<char, Func<char, bool>>
    {
        ['#'] = char.IsAsciiDigit,
    };

    /// <summary>
    /// The tokens of the masks of BitTextField: '#' is a digit, 'A' a letter and '*' a letter or a digit. Its script
    /// knows the same three, which is what lets it keep the caret in place as the text is laid out again.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, Func<char, bool>> TextTokens = new Dictionary<char, Func<char, bool>>
    {
        ['#'] = char.IsAsciiDigit,
        ['A'] = char.IsLetter,
        ['*'] = char.IsLetterOrDigit,
    };

    /// <summary>
    /// Lays the raw value out over the mask: every slot takes the next character of the raw value its token accepts,
    /// skipping one it does not, and every literal is copied as it is - but only while there is raw value left to place,
    /// so a half typed value never grows a trailing literal.
    /// </summary>
    /// <param name="raw">The value that goes into the slots.</param>
    /// <param name="mask">The mask to lay it out over.</param>
    /// <param name="tokens">The slot characters of the mask, each with what it accepts.</param>
    /// <param name="appendOverflow">
    /// Whether what is left of the raw value once the mask runs out is appended as it is, so a value longer than the mask
    /// is formatted as far as the mask goes and kept whole beyond it; otherwise it is dropped.
    /// </param>
    /// <param name="escape">The character that makes the next character of the mask a literal, if the mask has one.</param>
    public static string Apply(string raw, string mask, IReadOnlyDictionary<char, Func<char, bool>> tokens, bool appendOverflow, char? escape = null)
    {
        if (raw.Length == 0) return raw;

        var units = Parse(mask, tokens, escape);
        var builder = new StringBuilder(units.Length + raw.Length);
        var next = 0;

        foreach (var (c, accepts) in units)
        {
            if (next >= raw.Length) break;

            if (accepts is not null)
            {
                // A character the slot does not take has no place in the value, so it is passed over for the next one.
                while (next < raw.Length && accepts(raw[next]) is false) next++;

                if (next >= raw.Length) break;

                builder.Append(raw[next++]);
            }
            else
            {
                builder.Append(c);
            }
        }

        if (appendOverflow && next < raw.Length)
        {
            builder.Append(raw, next, raw.Length - next);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reads the raw value back out of a text the mask has already been laid over.
    /// </summary>
    /// <remarks>
    /// Walking the text and the mask side by side is what tells a character the mask inserted apart from one the user
    /// typed, which the characters alone cannot say for a mask holding a literal a slot would also take ("0## ### ####"):
    /// laying such a mask over its own result would otherwise keep the '0' the last formatting inserted and grow the value
    /// by a character per keystroke.
    /// </remarks>
    /// <param name="text">The formatted text.</param>
    /// <param name="mask">The mask it was laid out over.</param>
    /// <param name="tokens">The slot characters of the mask, each with what it accepts.</param>
    /// <param name="escape">The character that makes the next character of the mask a literal, if the mask has one.</param>
    public static string Strip(string text, string mask, IReadOnlyDictionary<char, Func<char, bool>> tokens, char? escape = null)
    {
        var units = Parse(mask, tokens, escape);
        var builder = new StringBuilder(text.Length);
        var maskIndex = 0;
        var textIndex = 0;

        while (textIndex < text.Length)
        {
            var c = text[textIndex];

            // Past the end of the mask there is nothing left but what did not fit in it.
            if (maskIndex >= units.Length)
            {
                if (AcceptedByAny(c, tokens))
                {
                    builder.Append(c);
                }

                textIndex++;
            }
            else if (units[maskIndex].Accepts is { } accepts)
            {
                // A character the slot does not take is one the text carries of its own, so it is dropped without
                // spending the slot it was found on.
                if (accepts(c))
                {
                    builder.Append(c);
                    maskIndex++;
                }

                textIndex++;
            }
            else if (c == units[maskIndex].Char)
            {
                // A literal of the mask, in the very place the mask puts it: it belongs to the layout, not the value.
                textIndex++;
                maskIndex++;
            }
            else if (AcceptedByAny(c, tokens))
            {
                // The text has not been laid out this far yet, so the literal it is missing is stepped over and the
                // same character is measured against what the mask has after it.
                maskIndex++;
            }
            else
            {
                textIndex++;
            }
        }

        return builder.ToString();
    }

    // The mask as the run of characters it writes, each a slot (with what its token accepts) or a literal.
    private static (char Char, Func<char, bool>? Accepts)[] Parse(string mask, IReadOnlyDictionary<char, Func<char, bool>> tokens, char? escape)
    {
        var units = new List<(char, Func<char, bool>?)>(mask.Length);

        for (var i = 0; i < mask.Length; i++)
        {
            var c = mask[i];

            // An escape at the very end has nothing to escape, so it is the literal it is.
            if (escape is { } e && c == e && i + 1 < mask.Length)
            {
                units.Add((mask[++i], null));
            }
            else
            {
                units.Add((c, tokens.TryGetValue(c, out var accepts) ? accepts : null));
            }
        }

        return [.. units];
    }

    private static bool AcceptedByAny(char c, IReadOnlyDictionary<char, Func<char, bool>> tokens)
    {
        foreach (var accepts in tokens.Values)
        {
            if (accepts(c)) return true;
        }

        return false;
    }
}
