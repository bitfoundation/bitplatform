using System.Text;
using System.Globalization;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Text as this server hands it back: cut where a cut leaves text, and - for what a caller sent - with
/// nothing in it that could pass for something this server did not write.
/// <para>
/// Every tool quotes what it could not find, and an agent reads the answer as the server's word. A line
/// break in a quoted name forges a line the server never wrote, a bidi override or a zero-width character
/// makes what is shown differ from what is there, and a megabyte-long name floods the context window the
/// documents are capped to protect. So caller text is quoted only through <see cref="Quote"/>.
/// </para>
/// </summary>
internal static class BsqliteText
{
    /// <summary>The most of a caller's text a reply quotes - longer than any name this server knows.</summary>
    public const int MaxQuoteLength = 64;

    /// <summary>
    /// The first <paramref name="max"/> characters of <paramref name="text"/>, or one fewer when the cut would
    /// fall between the two halves of a surrogate pair: half a pair is not text, and a client that re-encodes
    /// the answer turns it into a replacement character or rejects it outright.
    /// </summary>
    public static string CutAt(string text, int max)
        => text.Length <= max ? text : text[..(char.IsHighSurrogate(text[max - 1]) ? max - 1 : max)];

    /// <summary>
    /// A character no name has and no reply should carry unseen: a control character, either of the two
    /// Unicode separators that are not control characters yet end a line all the same, or a format character -
    /// a bidi override or isolate, which reorders the text around it as it is shown (CVE-2021-42574), or a
    /// zero-width one, which is not shown at all.
    /// </summary>
    public static bool IsUnsafe(char c)
        => char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Format;

    public static bool HasUnsafeCharacter(string text) => text.Any(IsUnsafe);

    /// <summary>
    /// The text with every <see cref="IsUnsafe"/> character written as a \uXXXX escape - which reads the same in
    /// a log line or a message, and is the character itself inside a C# string literal.
    /// </summary>
    public static string Escape(string text)
    {
        if (HasUnsafeCharacter(text) is false) return text;

        var builder = new StringBuilder(text.Length + 16);

        foreach (var c in text)
        {
            if (IsUnsafe(c)) builder.Append($"\\u{(int)c:X4}");
            else builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// A caller's text as a reply quotes it: at most <paramref name="max"/> characters of it, ending in "..."
    /// when it was longer, with every <see cref="IsUnsafe"/> character escaped.
    /// </summary>
    public static string Quote(string text, int max = MaxQuoteLength)
    {
        var cut = CutAt(text, max);

        return $"{Escape(cut)}{(cut.Length < text.Length ? "..." : null)}";
    }
}
