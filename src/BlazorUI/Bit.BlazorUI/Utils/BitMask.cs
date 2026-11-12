using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Lays a value out over a mask, and reads it back out of a text the mask has been laid over: the engine behind the
/// formatting of BitPhoneInput, kept apart from it so any input can format as the user types.
/// </summary>
/// <remarks>
/// A mask is a run of slots and literals. A slot is a character of the mask that names a token - '#', say - and takes
/// one character of the raw value that the token accepts (a digit, for '#'); every other character of the mask is a
/// literal, inserted for the user. The raw value holds only what went into the slots; the formatted text, the raw value
/// with the literals laid around it.
/// </remarks>
internal static class BitMask
{
    /// <summary>The tokens of a mask whose only slot is '#', a digit: the masks of BitPhoneInput.</summary>
    public static readonly IReadOnlyDictionary<char, Func<char, bool>> DigitTokens = new Dictionary<char, Func<char, bool>>
    {
        ['#'] = char.IsAsciiDigit,
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
    public static string Apply(string raw, string mask, IReadOnlyDictionary<char, Func<char, bool>> tokens, bool appendOverflow)
    {
        if (raw.Length == 0) return raw;

        var builder = new StringBuilder(mask.Length + raw.Length);
        var next = 0;

        foreach (var m in mask)
        {
            if (next >= raw.Length) break;

            if (tokens.TryGetValue(m, out var accepts))
            {
                // A character the slot does not take has no place in the value, so it is passed over for the next one.
                while (next < raw.Length && accepts(raw[next]) is false) next++;

                if (next >= raw.Length) break;

                builder.Append(raw[next++]);
            }
            else
            {
                builder.Append(m);
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
    public static string Strip(string text, string mask, IReadOnlyDictionary<char, Func<char, bool>> tokens)
    {
        var builder = new StringBuilder(text.Length);
        var maskIndex = 0;
        var textIndex = 0;

        while (textIndex < text.Length)
        {
            var c = text[textIndex];

            // Past the end of the mask there is nothing left but what did not fit in it.
            if (maskIndex >= mask.Length)
            {
                if (AcceptedByAny(c, tokens))
                {
                    builder.Append(c);
                }

                textIndex++;
            }
            else if (tokens.TryGetValue(mask[maskIndex], out var accepts))
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
            else if (c == mask[maskIndex])
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

    private static bool AcceptedByAny(char c, IReadOnlyDictionary<char, Func<char, bool>> tokens)
    {
        foreach (var accepts in tokens.Values)
        {
            if (accepts(c)) return true;
        }

        return false;
    }
}
