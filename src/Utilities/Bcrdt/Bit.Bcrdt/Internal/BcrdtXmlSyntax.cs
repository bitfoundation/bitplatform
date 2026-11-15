using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.Bcrdt;

/// <summary>
/// The XML rules the tree types share: what a name may be, and how text and values are written out.
/// </summary>
/// <remarks>
/// Written by hand rather than borrowed from <c>System.Xml</c> on purpose: the engine is trimmed into
/// WebAssembly apps, and pulling in the whole XML stack to check a tag name would cost every one of them
/// far more than these few rules do.
/// </remarks>
internal static class BcrdtXmlSyntax
{
    /// <summary>
    /// Whether <paramref name="name"/> is an XML 1.0 <c>Name</c> no longer than
    /// <see cref="BcrdtXmlElement.MaxNameLength"/>.
    /// </summary>
    /// <remarks>
    /// The same check runs on a name created locally and on one decoded from a peer, so every replica
    /// agrees on which names exist. A lone surrogate is never part of a name.
    /// </remarks>
    public static bool IsValidName([NotNullWhen(true)] string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > BcrdtXmlElement.MaxNameLength) return false;

        for (int i = 0; i < name.Length; i++)
        {
            bool first = i == 0;
            int codePoint = name[i];

            if (char.IsHighSurrogate(name[i]))
            {
                if (i + 1 >= name.Length || !char.IsLowSurrogate(name[i + 1])) return false;
                codePoint = char.ConvertToUtf32(name[i], name[i + 1]);
                i++;
            }
            else if (char.IsLowSurrogate(name[i]))
            {
                return false;
            }

            if (first ? !IsNameStartChar(codePoint) : !IsNameChar(codePoint)) return false;
        }

        return true;
    }

    /// <summary>Throws unless <paramref name="name"/> is a valid element or attribute name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid XML name.</exception>
    public static void ValidateName(string name, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(name, parameterName);
        if (!IsValidName(name))
        {
            throw new ArgumentException(
                $"'{name}' is not a valid XML name: it must be a non-empty XML 1.0 Name of at most " +
                $"{BcrdtXmlElement.MaxNameLength} characters.",
                parameterName);
        }
    }

    // NameStartChar and NameChar from the XML 1.0 (Fifth Edition) specification, section 2.3.
    private static bool IsNameStartChar(int c) =>
        c is ':' or '_' or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or
            (>= 0xC0 and <= 0xD6) or (>= 0xD8 and <= 0xF6) or (>= 0xF8 and <= 0x2FF) or
            (>= 0x370 and <= 0x37D) or (>= 0x37F and <= 0x1FFF) or (>= 0x200C and <= 0x200D) or
            (>= 0x2070 and <= 0x218F) or (>= 0x2C00 and <= 0x2FEF) or (>= 0x3001 and <= 0xD7FF) or
            (>= 0xF900 and <= 0xFDCF) or (>= 0xFDF0 and <= 0xFFFD) or (>= 0x10000 and <= 0xEFFFF);

    private static bool IsNameChar(int c) =>
        IsNameStartChar(c) || c is '-' or '.' or (>= '0' and <= '9') or 0xB7 or
            (>= 0x300 and <= 0x36F) or (>= 0x203F and <= 0x2040);

    /// <summary>
    /// Appends <paramref name="text"/> escaped for character data, or for a double-quoted attribute value.
    /// </summary>
    /// <remarks>
    /// Markup characters become entity references. In an attribute, tab, line feed and carriage return
    /// become character references too, because a parser would otherwise normalize them to spaces; in
    /// text only the carriage return does, which a parser would otherwise fold into a line feed. A
    /// character XML cannot carry at all - a control character, a lone surrogate, U+FFFE or U+FFFF - is
    /// written as U+FFFD, the replacement character, since not even a character reference may name it.
    /// </remarks>
    public static void AppendEscaped(StringBuilder builder, string text, bool attribute)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"' when attribute: builder.Append("&quot;"); break;
                case '\t' when attribute: builder.Append("&#9;"); break;
                case '\n' when attribute: builder.Append("&#10;"); break;
                case '\r': builder.Append("&#13;"); break;
                case '\t' or '\n': builder.Append(c); break;
                case < ' ' or '\uFFFE' or '\uFFFF': builder.Append('\uFFFD'); break;
                default:
                    if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        builder.Append(c).Append(text[++i]);
                    }
                    else if (char.IsSurrogate(c))
                    {
                        builder.Append('\uFFFD');
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }
    }

    /// <summary>Renders an attribute value from the collaborative value model as attribute text.</summary>
    /// <remarks>
    /// A string is written as it is. Every other scalar takes its invariant-culture form - so a replica's
    /// locale never changes the markup - a byte array is Base64, and a list or a map is JSON with its keys
    /// in ordinal order, so converged replicas render identical markup.
    /// </remarks>
    public static string FormatValue(object value) => value switch
    {
        string text => text,
        bool flag => flag ? "true" : "false",
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTimeOffset moment => moment.ToString("O", CultureInfo.InvariantCulture),
        DateTime moment => moment.ToString("O", CultureInfo.InvariantCulture),
        Guid id => id.ToString("D"),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => ToJson(value),
    };

    private static string ToJson(object value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteJson(writer, value);
        }
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }

    private static void WriteJson(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case long integer:
                writer.WriteNumberValue(integer);
                break;
            case double number when double.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case float number when float.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            case IReadOnlyDictionary<string, object?> map:
                writer.WriteStartObject();
                foreach (string key in map.Keys.Order(StringComparer.Ordinal))
                {
                    writer.WritePropertyName(WellFormed(key));
                    WriteJson(writer, map[key]);
                }
                writer.WriteEndObject();
                break;
            case List<object?> list:
                writer.WriteStartArray();
                foreach (object? item in list) WriteJson(writer, item);
                writer.WriteEndArray();
                break;
            default:
                // Strings, and the scalars JSON has no literal for - an infinite double, a date, a
                // Base64 byte array - as the same text they render as on their own.
                writer.WriteStringValue(WellFormed(FormatValue(value)));
                break;
        }
    }

    /// <summary>
    /// The text with every lone surrogate replaced by U+FFFD, which JSON - unlike the document - cannot carry.
    /// </summary>
    private static string WellFormed(string text)
    {
        int i = 0;
        for (; i < text.Length; i++)
        {
            if (!char.IsSurrogate(text[i])) continue;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
            else break;
        }
        if (i >= text.Length) return text;

        var builder = new StringBuilder(text.Length);
        for (int j = 0; j < text.Length; j++)
        {
            char c = text[j];
            if (char.IsHighSurrogate(c) && j + 1 < text.Length && char.IsLowSurrogate(text[j + 1])) builder.Append(c).Append(text[++j]);
            else builder.Append(char.IsSurrogate(c) ? '\uFFFD' : c);
        }
        return builder.ToString();
    }
}
