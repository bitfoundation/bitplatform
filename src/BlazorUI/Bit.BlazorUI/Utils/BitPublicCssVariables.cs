using System.Text;

namespace Bit.BlazorUI;

// A component whose popup is rendered outside its root - and relocated to the body while it is open - hands that
// popup none of what its own Style declares, so the public --bit-<Component>-* declarations of it are copied onto
// the popup by hand. This is the one place that picks them out of a style string.
internal static class BitPublicCssVariables
{
    // Appends every declaration of the given style whose property starts with the prefix, each ended with a
    // semicolon. A semicolon only ends a declaration outside of quotes and brackets, since a value can carry one
    // of its own - the url(data:image/png;base64,...) of an image - and cutting it there would copy a declaration
    // whose unclosed quote or bracket swallows everything written after it on the element it is copied onto.
    internal static void Append(ref StringBuilder? builder, string? style, string prefix)
    {
        if (style.HasNoValue() || style!.Contains(prefix, StringComparison.Ordinal) is false) return;

        var start = 0;
        var depth = 0;
        var quote = '\0';

        for (var i = 0; i < style.Length; i++)
        {
            var c = style[i];

            if (c == '\\')
            {
                i++;
                continue;
            }

            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            switch (c)
            {
                case '"' or '\'':
                    quote = c;
                    break;
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}':
                    if (depth > 0) depth--;
                    break;
                case ';' when depth == 0:
                    AppendDeclaration(ref builder, style.AsSpan(start, i - start), prefix);
                    start = i + 1;
                    break;
            }
        }

        if (start < style.Length)
        {
            AppendDeclaration(ref builder, style.AsSpan(start), prefix);
        }
    }

    private static void AppendDeclaration(ref StringBuilder? builder, ReadOnlySpan<char> declaration, string prefix)
    {
        declaration = declaration.Trim();

        if (declaration.StartsWith(prefix, StringComparison.Ordinal) is false) return;

        (builder ??= new StringBuilder()).Append(declaration).Append(';');
    }
}
