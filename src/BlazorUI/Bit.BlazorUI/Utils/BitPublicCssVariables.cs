using System.Text;

namespace Bit.BlazorUI;

// A popup relocated to the body inherits what its root declares through the chain Callouts.ts moves it into, but a
// part rendered beside the root that is never relocated - the calendar of a standalone date picker, the dial of a
// standalone circular time picker - is never given that chain, and so none of what the Style of its component
// declares reaches it. The public --bit-<Component>-* declarations of that Style are copied onto such a part by
// hand, and this is the one place that picks them out of a style string.
//
// One instance belongs to one component and remembers what it last picked out: the parts it is copied onto are
// re-rendered on every arrow key and on every hover while a range is being picked, and the result only changes
// when the Style of the instance or Styles.Root does.
internal sealed class BitPublicCssVariables(string prefix)
{
    private string? _style;
    private string? _stylesRoot;
    private string? _variables;

    // The public declarations of the Style of the component and of its Styles.Root, ahead of the part's own
    // style, which therefore still wins.
    internal string? Prepend(string? style, string? stylesRoot, string? partStyle)
    {
        if (string.Equals(style, _style, StringComparison.Ordinal) is false ||
            string.Equals(stylesRoot, _stylesRoot, StringComparison.Ordinal) is false)
        {
            _style = style;
            _stylesRoot = stylesRoot;

            StringBuilder? builder = null;

            Append(ref builder, style, prefix);
            Append(ref builder, stylesRoot, prefix);

            _variables = builder?.ToString();
        }

        if (_variables is null) return partStyle;

        return partStyle.HasNoValue() ? _variables : _variables + partStyle;
    }

    // Appends every declaration of the given style whose property starts with the prefix, each ended with a
    // semicolon. A semicolon only ends a declaration outside of quotes and brackets, since a value can carry one
    // of its own - the url(data:image/png;base64,...) of an image - and cutting it there would copy a declaration
    // whose unclosed quote or bracket swallows everything written after it on the element it is copied onto.
    private static void Append(ref StringBuilder? builder, string? style, string prefix)
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
