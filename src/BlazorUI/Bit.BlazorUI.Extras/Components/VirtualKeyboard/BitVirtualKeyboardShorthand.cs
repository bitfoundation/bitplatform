using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// The shorthand rows of a <see cref="BitVirtualKeyboardLayout"/> are written in: one string per row, its keys
/// separated by spaces.
/// </summary>
/// <remarks>
/// Each token is one key, read in this order:
/// <list type="number">
/// <item>A width: a token longer than one character that does not start with <c>{</c> and ends in <c>*</c> and a number
/// is a key of that width (<c>0*2</c> is a "0" key two units wide, <c>**2</c> a "*" key, <c>\\*1.5</c> a "\" key).</item>
/// <item>An escape: <c>\\</c> types a backslash, <c>\s</c> a space, <c>\u{HEX}</c> that code point, and a backslash before
/// anything else types that thing (<c>\{shift}</c> types "{shift}").</item>
/// <item>A named key: <c>{name}</c>, <c>{name:argument}</c>, <c>{name*width}</c> - a key of the layout's bag or a built-in
/// key (<c>{bksp*1.5}</c>, <c>{layer:symbols}</c>, <c>{dead:acute}</c>, <c>{cmd:qty}</c>). A token of three or more
/// characters that starts with <c>{</c> or ends with <c>}</c> and is not a well-formed name is an error.</item>
/// <item>Anything else types itself (<c>.com</c>, <c>لا</c>, a lone <c>{</c>); when the layout's bag has a key of that
/// text, the row takes that key.</item>
/// </list>
/// </remarks>
public static partial class BitVirtualKeyboardShorthand
{
    [GeneratedRegex(@"^(.+)\*([0-9]+(?:\.[0-9]+)?)$", RegexOptions.CultureInvariant)]
    private static partial Regex WidthSuffixRegex();

    [GeneratedRegex(@"^\{([a-z][a-z0-9-]*)(?::([^*{}\s]+))?(?:\*([0-9]+(?:\.[0-9]+)?))?\}$", RegexOptions.CultureInvariant)]
    private static partial Regex NamedKeyRegex();



    /// <summary>
    /// Parses one row.
    /// </summary>
    /// <exception cref="FormatException">A token is a malformed named key or escape.</exception>
    public static BitVirtualKeyboardRow ParseRow(string shorthand)
    {
        ArgumentNullException.ThrowIfNull(shorthand);

        var keys = new List<BitVirtualKeyboardKey>();

        foreach (var token in shorthand.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            keys.Add(ParseToken(token, shorthand));
        }

        return new BitVirtualKeyboardRow { Keys = keys };
    }

    /// <summary>
    /// Parses several rows.
    /// </summary>
    public static List<BitVirtualKeyboardRow> ParseRows(params IEnumerable<string> shorthands)
    {
        ArgumentNullException.ThrowIfNull(shorthands);

        return [.. shorthands.Select(ParseRow)];
    }



    private static BitVirtualKeyboardKey ParseToken(string token, string row)
    {
        double? width = null;
        var head = token;

        // 1. a width on anything but a named key, which carries its width inside its braces
        if (token.Length > 1 && token[0] != '{')
        {
            var match = WidthSuffixRegex().Match(token);
            if (match.Success)
            {
                head = match.Groups[1].Value;
                width = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            }
        }

        // 2. an escape
        if (head[0] == '\\')
        {
            return new BitVirtualKeyboardKey { Output = Unescape(head, token, row), Width = width };
        }

        // 3. a named key
        if (head.Length >= 3 && (head[0] == '{' || head[^1] == '}'))
        {
            var match = NamedKeyRegex().Match(head);
            if (match.Success is false || width.HasValue)
            {
                throw new FormatException($"The token \"{token}\" of the row \"{row}\" is not a well-formed named key: write {{name}}, {{name:argument}} or {{name*width}} with a lowercase name, or escape it (\\{token}).");
            }

            var name = match.Groups[1].Value;
            var argument = match.Groups[2].Success ? match.Groups[2].Value : null;

            return new BitVirtualKeyboardKey
            {
                Id = argument is null ? name : $"{name}:{argument}",
                Layer = name == "layer" ? argument : null,
                DeadKey = name == "dead" ? argument : null,
                Command = name == "cmd" ? argument : null,
                Width = match.Groups[3].Success ? double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : null,
            };
        }

        // 4. a literal
        return new BitVirtualKeyboardKey { Output = head, Width = width };
    }

    private static string Unescape(string text, string token, string row)
    {
        if (text.Length == 1) return "\\";

        var sb = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c != '\\' || i == text.Length - 1)
            {
                sb.Append(c);
                continue;
            }

            var next = text[i + 1];

            if (next == 's')
            {
                sb.Append(' ');
                i++;
            }
            else if (next == 'u' && i + 2 < text.Length && text[i + 2] == '{')
            {
                var end = text.IndexOf('}', i + 3);
                var hex = end < 0 ? "" : text[(i + 3)..end];

                if (hex.Length is < 1 or > 6 || hex.All(Uri.IsHexDigit) is false
                    || int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var codePoint) is false
                    || Rune.IsValid(codePoint) is false)
                {
                    throw new FormatException($"The escape of the token \"{token}\" of the row \"{row}\" is not a code point: write \\u{{HEX}} with one to six hex digits.");
                }

                sb.Append(char.ConvertFromUtf32(codePoint));
                i = end;
            }
            else
            {
                // \\ types a backslash, and a backslash before anything else types that thing
                sb.Append(next);
                i++;
            }
        }

        return sb.ToString();
    }
}