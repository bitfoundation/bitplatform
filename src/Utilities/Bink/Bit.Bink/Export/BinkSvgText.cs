using System.Globalization;

namespace Bit.Bink;

/// <summary>XML escaping helpers shared by the SVG writers.</summary>
internal static class BinkSvgText
{
    /// <summary>Writes text with XML special characters escaped.</summary>
    public static void WriteEscaped(TextWriter writer, ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '<': writer.Write("&lt;"); break;
                case '>': writer.Write("&gt;"); break;
                case '&': writer.Write("&amp;"); break;
                case '"': writer.Write("&quot;"); break;
                case '\'': writer.Write("&#39;"); break;
                default:
                    // Control characters are not legal in XML 1.0 and would make the output unparseable.
                    if (c < 0x20 && c is not ('\t' or '\n' or '\r')) writer.Write(' ');
                    // Neither are the noncharacters U+FFFE and U+FFFF, nor a surrogate that is not part of a pair.
                    else if (c is '￾' or '￿') writer.Write(' ');
                    else if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { writer.Write(c); writer.Write(text[++i]); }
                    else if (char.IsSurrogate(c)) writer.Write('�');
                    else writer.Write(c);
                    break;
            }
        }
    }

    /// <summary>Returns the text with XML special characters escaped.</summary>
    public static string Escape(string text)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        WriteEscaped(writer, text);
        return writer.ToString();
    }
}
