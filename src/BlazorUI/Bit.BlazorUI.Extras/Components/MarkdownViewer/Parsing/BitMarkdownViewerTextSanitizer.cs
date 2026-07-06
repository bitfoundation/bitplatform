namespace Bit.BlazorUI;

/// <summary>
/// Pre-processing helpers applied to the raw Markdown source before parsing.
/// </summary>
internal static class BitMarkdownViewerTextSanitizer
{
    /// <summary>
    /// Removes Unicode bidirectional formatting characters that can be abused to make
    /// the rendered text (links, code, ...) display in a different order than it is
    /// logically encoded - the "Trojan Source" class of spoofing attacks (CVE-2021-42574).
    /// </summary>
    /// <remarks>
    /// Only the bidi reordering control characters are stripped. Characters needed by
    /// legitimate scripts and emoji - notably the zero-width joiner (U+200D) and
    /// non-joiner (U+200C) - are deliberately preserved. Text direction for genuine
    /// right-to-left content is still handled by the host via the <c>dir</c> attribute
    /// and the browser's bidi algorithm, so removing the explicit overrides is safe.
    /// </remarks>
    public static string StripBidiControlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Quick scan: avoid allocating when there is nothing to strip (the common case).
        bool hasAny = false;
        foreach (char c in text)
        {
            if (IsBidiControl(c)) { hasAny = true; break; }
        }
        if (!hasAny) return text;

        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!IsBidiControl(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    // The Unicode bidi formatting / override / isolate controls plus the directional
    // marks, per UAX #9 and the Trojan Source research.
    private static bool IsBidiControl(char c) =>
        c is '\u202A' or '\u202B' or '\u202C' or '\u202D' or '\u202E' // LRE RLE PDF LRO RLO
          or '\u2066' or '\u2067' or '\u2068' or '\u2069'             // LRI RLI FSI PDI
          or '\u200E' or '\u200F'                                     // LRM RLM
          or '\u061C';                                                // ALM (Arabic letter mark)
}
