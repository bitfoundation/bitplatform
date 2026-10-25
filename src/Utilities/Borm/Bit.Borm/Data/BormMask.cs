using System.Text;

namespace Bit.Borm;

/// <summary>
/// The shape a value is written in — a phone number <c>(000) 000-0000</c>, an IBAN
/// <c>AA00 0000 0000 0000 0000 00</c>, a postcode <c>A0A 0A0</c> — and the means to put what the user typed
/// into it.
/// </summary>
/// <remarks>
/// <para>
/// <c>0</c> is a digit, <c>A</c> a letter (written in capitals), <c>a</c> a letter as typed and <c>*</c> a
/// letter or a digit; a backslash makes the next character literal (<c>\0</c>, <c>\A</c>), and every other
/// character is literal already. What the user types is read for the characters that fill the slots: the
/// literals they type are taken as such, and any other separator - a space, a dash, a dot - is passed over,
/// so <c>5551234567</c>, <c>555-123-4567</c> and <c>(555) 123-4567</c> are the same answer.
/// </para>
/// <para>
/// A mask formats a value once it is entered; it does not rewrite the box under the caret while the user
/// types. Doing that needs script to put the caret back, and a box that changes what is being typed into
/// it is one assistive technology describes badly - which is why the advice of GOV.UK and others is not to
/// use one at all.
/// </para>
/// </remarks>
public sealed class BormMask
{
    private enum Slot { Literal, Digit, UpperLetter, Letter, LetterOrDigit }

    private readonly (Slot Kind, char Literal)[] _tokens;

    /// <summary>Parses <paramref name="pattern"/>.</summary>
    /// <exception cref="ArgumentException">The pattern is empty, has no slot to fill, or ends in a lone backslash.</exception>
    public BormMask(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var tokens = new List<(Slot, char)>(pattern.Length);
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\')
            {
                if (++i == pattern.Length) throw new ArgumentException($"The mask '{pattern}' ends in a backslash with nothing to escape.", nameof(pattern));
                tokens.Add((Slot.Literal, pattern[i]));
                continue;
            }

            tokens.Add(c switch
            {
                '0' => (Slot.Digit, c),
                'A' => (Slot.UpperLetter, c),
                'a' => (Slot.Letter, c),
                '*' => (Slot.LetterOrDigit, c),
                _ => (Slot.Literal, c)
            });
        }

        _tokens = [.. tokens];
        SlotCount = _tokens.Count(t => t.Kind != Slot.Literal);
        if (SlotCount == 0) throw new ArgumentException($"The mask '{pattern}' has no slot for the user to fill (0, A, a or *).", nameof(pattern));

        Pattern = pattern;
        OnlyDigits = _tokens.All(t => t.Kind is Slot.Literal or Slot.Digit);
        Display = string.Concat(_tokens.Select(t => t.Kind switch
        {
            Slot.Digit => '#',
            Slot.UpperLetter or Slot.Letter => 'A',
            Slot.LetterOrDigit => '*',
            _ => t.Literal
        }));
    }

    /// <summary>The pattern as written.</summary>
    public string Pattern { get; }

    /// <summary>How many characters the user has to give.</summary>
    public int SlotCount { get; }

    /// <summary>Whether every slot is a digit — a box a phone's keyboard can offer numbers for.</summary>
    public bool OnlyDigits { get; }

    /// <summary>
    /// The shape for a person to read: <c>#</c> for a digit, <c>A</c> for a letter, <c>*</c> for either -
    /// <c>(###) ###-####</c>. What the message for a value that does not fit shows.
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// Puts <paramref name="text"/> into the mask. True with the whole formatted value when every slot was
    /// filled and nothing was left over; false when a character fits no slot, or there are too few or too many.
    /// </summary>
    public bool TryFormat(string? text, out string formatted)
    {
        formatted = string.Empty;
        if (string.IsNullOrEmpty(text)) return false;

        var output = new StringBuilder(_tokens.Length);
        var i = 0;
        foreach (var (kind, literal) in _tokens)
        {
            if (kind == Slot.Literal)
            {
                output.Append(literal);
                // The user typed it: it stands for itself, not for the next slot.
                if (i < text.Length && char.ToUpperInvariant(text[i]) == char.ToUpperInvariant(literal)) i++;
                continue;
            }

            while (i < text.Length && !char.IsLetterOrDigit(text[i])) i++;
            if (i == text.Length) return false;

            var c = text[i++];
            switch (kind)
            {
                case Slot.Digit when char.IsAsciiDigit(c):
                    output.Append(c);
                    break;
                case Slot.UpperLetter when char.IsLetter(c):
                    output.Append(char.ToUpperInvariant(c));
                    break;
                case Slot.Letter when char.IsLetter(c):
                case Slot.LetterOrDigit when char.IsLetter(c) || char.IsAsciiDigit(c):
                    output.Append(c);
                    break;
                default:
                    return false;
            }
        }

        for (; i < text.Length; i++)
            if (char.IsLetterOrDigit(text[i])) return false;

        formatted = output.ToString();
        return true;
    }

    /// <summary>
    /// The characters that filled the slots, without the literals: <c>5551234567</c> for
    /// <c>(555) 123-4567</c>. False when <paramref name="text"/> does not fit the mask.
    /// </summary>
    public bool TryUnmask(string? text, out string raw)
    {
        raw = string.Empty;
        if (!TryFormat(text, out var formatted)) return false;

        var output = new StringBuilder(SlotCount);
        for (var t = 0; t < _tokens.Length; t++)
            if (_tokens[t].Kind != Slot.Literal) output.Append(formatted[t]);
        raw = output.ToString();
        return true;
    }

    public override string ToString() => Pattern;
}
