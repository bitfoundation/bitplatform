using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BbScanner;

/// <summary>
/// A GS1 element string - the Application Identifiers and data fields of a GS1-128, GS1 DataMatrix, GS1 QR Code,
/// GS1 DataBar or GS1 Aztec symbol - split into its elements and checked against the GS1 General Specifications.
/// </summary>
/// <remarks>
/// <para>
/// A decoder reports a GS1 symbol's content as <see cref="BbBarcodeResult.Text"/>, the element string as the symbol
/// encodes it: the AIs and their values run together, a variable length value ended by the group separator
/// (<c>U+001D</c>) that stands for FNC1. <see cref="BbBarcodeResult.TryGetGs1(out BbGs1ElementString?)"/> parses that
/// text; <see cref="TryParse(string?, out BbGs1ElementString?)"/> parses one from anywhere else - a keyboard wedge,
/// a database - in the same form, with or without a leading symbology identifier such as <c>]C1</c>, or in the
/// bracketed form printed under a symbol, <c>(01)09521234543213(10)ABC123</c>.
/// </para>
/// <para>
/// Parsing is strict. Each AI must be one of the GS1 AIs this library knows; each value must have the length, the
/// characters and the check digit its AI calls for, and dates must be real dates. An AI may repeat only with the
/// same value. Whatever does not parse is rejected as a whole, never reported in part: an element string read
/// wrongly would put a batch number where an expiry date belongs. The checks that associate one AI with another -
/// that an expiry date comes with a GTIN, say - are the application's to make.
/// </para>
/// </remarks>
public sealed class BbGs1ElementString
{
    private const char GroupSeparator = '\u001D';

    private BbGs1ElementString(BbGs1Element[] elements) => Elements = elements;

    /// <summary>The elements, in the order the symbol carries them.</summary>
    public IReadOnlyList<BbGs1Element> Elements { get; }

    /// <summary>The value of the element with the given AI, or <see langword="null"/> when there is none.</summary>
    /// <param name="applicationIdentifier">The AI, for example <c>01</c> for the GTIN.</param>
    public string? this[string applicationIdentifier] => Find(applicationIdentifier)?.Value;

    /// <summary>The element with the given AI, or <see langword="null"/> when there is none.</summary>
    /// <param name="applicationIdentifier">The AI, for example <c>17</c> for the expiry date.</param>
    public BbGs1Element? Find(string applicationIdentifier)
    {
        ArgumentNullException.ThrowIfNull(applicationIdentifier);
        foreach (var element in Elements)
        {
            if (string.Equals(element.ApplicationIdentifier, applicationIdentifier, StringComparison.Ordinal))
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>
    /// The element string in the bracketed form GS1 prints under a symbol, <c>(01)09521234543213(10)ABC123</c>; an
    /// opening parenthesis inside a value is escaped with a backslash, which character set 82 has no use for, so the text parses back to the same elements.
    /// </summary>
    public string ToBracketedString()
    {
        var builder = new StringBuilder();
        foreach (var element in Elements)
        {
            builder.Append('(').Append(element.ApplicationIdentifier).Append(')');
            foreach (var character in element.Value)
            {
                if (character == '(')
                {
                    builder.Append('\\');
                }

                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// The element string as a symbol encodes it: values run together, each variable length one that is not last
    /// ended by a group separator (<c>U+001D</c>).
    /// </summary>
    public string ToElementString()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < Elements.Count; i++)
        {
            var element = Elements[i];
            builder.Append(element.ApplicationIdentifier).Append(element.Value);
            if (i < Elements.Count - 1 && !HasPredefinedLength(element.ApplicationIdentifier))
            {
                builder.Append(GroupSeparator);
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc cref="ToBracketedString" />
    public override string ToString() => ToBracketedString();

    /// <summary>Parses an element string, in its encoded or its bracketed form.</summary>
    /// <param name="text">The element string.</param>
    /// <returns>The elements.</returns>
    /// <exception cref="FormatException">The text is not a valid GS1 element string; the message says where and why.</exception>
    public static BbGs1ElementString Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TryParseCore(text, out var result, out var error) ? result : throw new FormatException(error);
    }

    /// <summary>Parses an element string, in its encoded or its bracketed form.</summary>
    /// <param name="text">The element string.</param>
    /// <param name="result">The elements, or <see langword="null"/> when the text is not a valid GS1 element string.</param>
    /// <returns>Whether the text parsed.</returns>
    public static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out BbGs1ElementString? result)
    {
        if (text is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(text, out result, out _);
    }

    private static bool HasPredefinedLength(string ai) => BbGs1Dictionary.PredefinedLengths.ContainsKey(ai[..2]);

    private static bool TryParseCore(string text, [NotNullWhen(true)] out BbGs1ElementString? result, [NotNullWhen(false)] out string? error)
    {
        result = null;
        var input = StripSymbologyIdentifier(text.AsSpan());

        var elements = new List<BbGs1Element>();
        if (input.Length > 0 && input[0] == '('
            ? !ParseBracketed(input, elements, out error)
            : !ParseEncoded(input, elements, out error))
        {
            return false;
        }

        if (elements.Count == 0)
        {
            error = "The element string is empty.";
            return false;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                if (elements[i].ApplicationIdentifier == elements[j].ApplicationIdentifier &&
                    elements[i].Value != elements[j].Value)
                {
                    error = $"AI ({elements[i].ApplicationIdentifier}) appears twice with different values.";
                    return false;
                }
            }
        }

        result = new BbGs1ElementString([.. elements]);
        return true;
    }

    // A scanner that reports symbology identifiers puts one ahead of the data: ]C1 for GS1-128, ]e0 for GS1 DataBar,
    // ]d2 for GS1 DataMatrix, ]Q3 for GS1 QR Code, ]z1 for GS1 Aztec, ]J1 for GS1 DotCode. Any other identifier says
    // the data is not GS1 - ]C0 is plain Code 128, ]Q1 a plain QR code - and is left in place to fail the parse. FNC1 in
    // first position may also come through as a leading separator.
    private static ReadOnlySpan<char> StripSymbologyIdentifier(ReadOnlySpan<char> input)
    {
        if (input.Length >= 3 && input[0] == ']' && input[1..3] is "C1" or "e0" or "d2" or "Q3" or "z1" or "J1")
        {
            input = input[3..];
        }

        while (input.Length > 0 && input[0] == GroupSeparator)
        {
            input = input[1..];
        }

        return input;
    }

    private static bool ParseEncoded(ReadOnlySpan<char> input, List<BbGs1Element> elements, [NotNullWhen(false)] out string? error)
    {
        var position = 0;
        while (position < input.Length)
        {
            var definition = BbGs1Dictionary.Match(input[position..]);
            if (definition is null)
            {
                error = $"No known GS1 Application Identifier starts at position {position}.";
                return false;
            }

            var valueStart = position + definition.Ai.Length;
            int valueEnd;
            if (BbGs1Dictionary.PredefinedLengths.TryGetValue(definition.Ai[..2], out var elementLength))
            {
                // A predefined length element needs no separator after it; one is tolerated.
                valueEnd = position + elementLength;
                if (valueEnd > input.Length)
                {
                    error = $"AI ({definition.Ai}) needs {elementLength - definition.Ai.Length} characters; the element string ends first.";
                    return false;
                }

                if (input[valueStart..valueEnd].Contains(GroupSeparator))
                {
                    error = $"AI ({definition.Ai}) has a fixed length; a separator cuts it short.";
                    return false;
                }

                position = valueEnd < input.Length && input[valueEnd] == GroupSeparator ? valueEnd + 1 : valueEnd;
            }
            else
            {
                var separator = input[valueStart..].IndexOf(GroupSeparator);
                valueEnd = separator < 0 ? input.Length : valueStart + separator;
                position = separator < 0 ? input.Length : valueEnd + 1;
            }

            if (!TryCreate(definition, input[valueStart..valueEnd].ToString(), elements, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool ParseBracketed(ReadOnlySpan<char> input, List<BbGs1Element> elements, [NotNullWhen(false)] out string? error)
    {
        var position = 0;
        var value = new StringBuilder();
        while (position < input.Length)
        {
            if (input[position] != '(')
            {
                error = $"An AI in parentheses was expected at position {position}.";
                return false;
            }

            var close = input[position..].IndexOf(')');
            if (close < 0)
            {
                error = $"The AI at position {position} has no closing parenthesis.";
                return false;
            }

            var ai = input.Slice(position + 1, close - 1);
            var definition = BbGs1Dictionary.Match(ai);
            if (definition is null || definition.Ai.Length != ai.Length)
            {
                error = $"({ai}) is not a known GS1 Application Identifier.";
                return false;
            }

            position += close + 1;
            value.Clear();
            while (position < input.Length && input[position] != '(')
            {
                if (input[position] == '\\' && position + 1 < input.Length && input[position + 1] == '(')
                {
                    position++;
                }

                value.Append(input[position++]);
            }

            if (!TryCreate(definition, value.ToString(), elements, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryCreate(BbGs1AiDefinition definition, string value, List<BbGs1Element> elements, [NotNullWhen(false)] out string? error)
    {
        if (!BbGs1Validator.Validate(definition, value, out error))
        {
            error = $"AI ({definition.Ai}): {error}";
            return false;
        }

        elements.Add(new BbGs1Element(definition, value));
        return true;
    }
}
