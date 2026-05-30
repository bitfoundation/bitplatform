namespace Bit.Brouter;

// Parses route templates and extracts parameters from them.
// Supports:
//   - Literal segments (e.g. /Path/To/Some/Page)
//   - Parameter segments (e.g. /Customer/{Id}/Orders/{OrderId})
//   - Parameter constraints (e.g. {id:int} or {id:int:long})
//   - Optional parameters (must be trailing): {id?}
//   - Catch-all parameters (must be the very last segment): {**path}
//   - Literal wildcards: '*' for a single segment, '**' for catch-all
internal static class BrouterTemplateParser
{
    private static readonly char[] _invalidParameterNameCharacters = ['*', '?', '{', '}', '=', '.', ':'];

    /// <summary>Read-only view of the characters that aren't allowed inside a parameter name.</summary>
    public static ReadOnlySpan<char> InvalidParameterNameCharacters => _invalidParameterNameCharacters;

    internal static BrouterRouteTemplate ParseTemplate(string template)
    {
        if (string.IsNullOrEmpty(template)) return new BrouterRouteTemplate("", []);

        var originalTemplate = template;
        template = template.Trim('/');

        // Special case "/".
        if (template == "") return new BrouterRouteTemplate("/", []);

        var segments = template.Split('/');
        var templateSegments = new BrouterTemplateSegment[segments.Length];

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrEmpty(segment))
                throw new InvalidOperationException($"Invalid path '{template}'. Empty segments are not allowed.");

            if (segment[0] != '{')
            {
                if (segment[^1] == '}')
                    throw new InvalidOperationException($"Invalid path '{template}'. Missing '{{' in parameter segment '{segment}'.");

                templateSegments[i] = new BrouterTemplateSegment(originalTemplate, segment, isParameter: false);
            }
            else
            {
                if (segment[^1] != '}')
                    throw new InvalidOperationException($"Invalid path '{template}'. Missing '}}' in parameter segment '{segment}'.");

                if (segment.Length < 3)
                    throw new InvalidOperationException($"Invalid path '{template}'. Empty parameter name in segment '{segment}' is not allowed.");

                var inner = segment[1..^1];

                // Validate parameter name characters: skip '*' (catch-all prefix), ':' (constraint separator), '?' (optional suffix).
                ValidateParameterName(originalTemplate, segment, inner);

                templateSegments[i] = new BrouterTemplateSegment(originalTemplate, inner, isParameter: true);
            }
        }

        // Validate placement rules:
        //   - Catch-all (literal '**' or '{**x}') must be the last segment.
        //   - All segments after the first optional parameter must also be optional (trailing optionals only).
        bool sawOptional = false;
        for (int i = 0; i < templateSegments.Length; i++)
        {
            var s = templateSegments[i];

            if (s.IsCatchAll && i != templateSegments.Length - 1)
                throw new InvalidOperationException($"Invalid path '{template}'. Catch-all segments must appear last.");

            if (s.IsOptional)
            {
                sawOptional = true;
            }
            else if (sawOptional)
            {
                throw new InvalidOperationException(
                    $"Invalid path '{template}'. Optional parameters can only appear at the end of the template.");
            }
        }

        // Detect duplicate parameter names.
        for (int i = 0; i < templateSegments.Length; i++)
        {
            var s = templateSegments[i];
            if (s.IsParameter is false) continue;

            for (int j = i + 1; j < templateSegments.Length; j++)
            {
                var n = templateSegments[j];
                if (n.IsParameter is false) continue;

                if (string.Equals(s.Value, n.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Invalid path '{template}'. The parameter '{s.Value}' appears multiple times.");
            }
        }

        return new BrouterRouteTemplate(template, templateSegments);
    }

    private static void ValidateParameterName(string template, string segment, string inner)
    {
        // Strip leading '**', then trailing '?', then the constraint suffix ':...' before checking the name.
        // Order matters: the optional marker '?' must be checked before stripping constraints
        // so that malformed tokens like "id?:int" are caught (the '?' would remain in the name
        // after colon-stripping and fail the invalid-character check).
        var name = inner;
        if (name.StartsWith("**", StringComparison.Ordinal)) name = name[2..];
        if (name.EndsWith('?')) name = name[..^1];
        var colon = name.IndexOf(':');
        if (colon >= 0) name = name[..colon];

        if (name.Length == 0)
            throw new InvalidOperationException($"Invalid path '{template}'. Empty parameter name in segment '{segment}' is not allowed.");

        var invalidIdx = name.AsSpan().IndexOfAny(InvalidParameterNameCharacters);
        if (invalidIdx != -1)
            throw new InvalidOperationException(
                $"Invalid path '{template}'. The character '{name[invalidIdx]}' in parameter segment '{segment}' is not allowed.");
    }
}
