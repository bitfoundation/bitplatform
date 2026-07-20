namespace Bit.Brouter;

// Parses route templates and extracts parameters from them.
// Supports:
//   - Literal segments (e.g. /Path/To/Some/Page)
//   - Parameter segments (e.g. /Customer/{Id}/Orders/{OrderId})
//   - Parameter constraints (e.g. {id:int}, {id:int:min(0)}, {slug:regex(^[a-z]+$)})
//   - Optional parameters: {id?} - allowed anywhere; a non-trailing optional parses but matches
//     as required, mirroring the built-in Blazor router
//   - Default values: {action=Index} (bound when the URL omits the segment)
//   - Catch-all parameters (must be the very last segment): {*path} / {**path}, constraints allowed
//   - Complex multi-part segments: {name}.{ext}, v{major}-{minor}, {file}.{ext?}
//   - Literal wildcards: '*' for a single segment, '**' for catch-all
internal static class BrouterTemplateParser
{
    private static readonly char[] _invalidParameterNameCharacters = ['*', '?', '{', '}', '=', '.', ':'];

    /// <summary>Read-only view of the characters that aren't allowed inside a parameter name.</summary>
    public static ReadOnlySpan<char> InvalidParameterNameCharacters => _invalidParameterNameCharacters;

    internal static BrouterRouteTemplate ParseTemplate(string template, BrouterConstraintRegistry? constraints = null)
    {
        if (string.IsNullOrEmpty(template)) return new BrouterRouteTemplate("", []);

        var originalTemplate = template;

        // Framework parity: a leading "~/" is equivalent to an app-relative "/".
        if (template.StartsWith("~/", StringComparison.Ordinal)) template = template[2..];
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

            templateSegments[i] = BrouterTemplateSegment.Parse(originalTemplate, segment, constraints);
        }

        // Catch-all (literal '**' or '{**x}' / '{*x}') must be the last segment. Optional parameters
        // may appear anywhere: like the built-in Blazor router, a middle optional is accepted at
        // parse time and simply behaves as required at match time (only the trailing run of
        // optional / default-valued segments can actually be omitted by a shorter URL).
        for (int i = 0; i < templateSegments.Length - 1; i++)
        {
            if (templateSegments[i].IsCatchAll)
                throw new InvalidOperationException($"Invalid path '{template}'. Catch-all segments must appear last.");
        }

        // Detect duplicate parameter names (including parameters inside complex segments).
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var templateSegment in templateSegments)
        {
            foreach (var name in templateSegment.ParameterNames)
            {
                if (seenNames.Add(name) is false)
                    throw new InvalidOperationException($"Invalid path '{template}'. The parameter '{name}' appears multiple times.");
            }
        }

        return new BrouterRouteTemplate(template, templateSegments);
    }
}
