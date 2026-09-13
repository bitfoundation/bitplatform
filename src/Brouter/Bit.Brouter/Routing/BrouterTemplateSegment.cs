using System.Text;

namespace Bit.Brouter;

// One '/'-delimited chunk of a route template. Three shapes:
//   - Literal:              "users", plus the literal wildcards "*" (single segment) and "**" (catch-all)
//   - Simple parameter:     "{id}", "{id?}", "{id:int}", "{id=5}", "{*rest}", "{**rest}", "{*rest:nonfile}"
//   - Complex (multi-part): "{name}.{ext}", "v{major}-{minor}", "{file}.{ext?}"
internal class BrouterTemplateSegment
{
    /// <summary>
    /// The literal text for a literal segment, the parameter name for a parameter segment,
    /// or the raw segment text for a complex (multi-part) segment.
    /// </summary>
    public string Value { get; }

    public bool IsParameter { get; }

    /// <summary>True for a multi-part segment such as <c>{name}.{ext}</c> or <c>v{major}-{minor}</c>.</summary>
    public bool IsComplex => Parts is not null;

    /// <summary>The ordered parts of a complex segment; null for literal and simple parameter segments.</summary>
    public IReadOnlyList<BrouterTemplatePart>? Parts { get; }

    /// <summary>True for the literal-wildcard form <c>**</c> or the parameter catch-all forms <c>{*name}</c> / <c>{**name}</c>.</summary>
    public bool IsCatchAll { get; }

    /// <summary>True for the literal single-segment wildcard <c>*</c>.</summary>
    public bool IsSingleWildcard { get; }

    /// <summary>True for parameters declared with a trailing <c>?</c>, e.g. <c>{id?}</c>.</summary>
    public bool IsOptional { get; }

    /// <summary>The default declared with <c>=</c>, e.g. <c>{action=Index}</c>; null when absent.</summary>
    public string? DefaultValue { get; }

    public bool HasDefault => DefaultValue is not null;

    public BrouterRouteConstraintBinding[] Constraints { get; }

    /// <summary>
    /// The parameter names this segment binds: the name itself for a simple parameter, every
    /// parameter part's name for a complex segment, none for literals.
    /// </summary>
    public IEnumerable<string> ParameterNames
    {
        get
        {
            if (IsParameter)
            {
                yield return Value;
                yield break;
            }

            if (Parts is null) yield break;

            foreach (var part in Parts)
            {
                if (part.IsParameter) yield return part.Value;
            }
        }
    }

    /// <summary>Parses one raw template segment into a literal, simple-parameter, or complex segment.</summary>
    internal static BrouterTemplateSegment Parse(string template, string segment, BrouterConstraintRegistry? constraints = null)
    {
        // Fast path: no braces at all - a plain literal (or the literal wildcards '*' / '**').
        if (segment.IndexOf('{') < 0 && segment.IndexOf('}') < 0)
            return new BrouterTemplateSegment(segment);

        var parts = TokenizeParts(template, segment);

        if (parts.Count == 1)
        {
            return parts[0].IsParameter
                ? new BrouterTemplateSegment(template, segment, parts[0].Text, constraints)
                : new BrouterTemplateSegment(parts[0].Text); // literal containing escaped braces
        }

        return new BrouterTemplateSegment(template, segment, parts, constraints);
    }

    // Literal segment (also covers the literal wildcards '*' and '**').
    private BrouterTemplateSegment(string literal)
    {
        Value = literal;
        Constraints = [];
        IsSingleWildcard = literal == "*";
        IsCatchAll = literal == "**";
    }

    // Simple parameter segment: the whole segment is a single "{...}" token.
    private BrouterTemplateSegment(string template, string segmentText, string inner, BrouterConstraintRegistry? registry)
    {
        IsParameter = true;

        ParseParameterCore(template, segmentText, inner, registry, allowCatchAll: true,
            out var name, out var isCatchAll, out var isOptional, out var defaultValue, out var constraints);

        Value = name;
        IsCatchAll = isCatchAll;
        IsOptional = isOptional;
        DefaultValue = defaultValue;
        Constraints = constraints;
    }

    // Complex segment: multiple literal/parameter parts inside one segment.
    private BrouterTemplateSegment(string template, string segmentText, List<(bool IsParameter, string Text)> rawParts, BrouterConstraintRegistry? registry)
    {
        Value = segmentText;
        Constraints = [];

        var parts = new BrouterTemplatePart[rawParts.Count];
        for (var i = 0; i < rawParts.Count; i++)
        {
            var (isParam, text) = rawParts[i];

            if (isParam is false)
            {
                parts[i] = BrouterTemplatePart.Literal(text);
                continue;
            }

            if (i > 0 && rawParts[i - 1].IsParameter)
                throw new InvalidOperationException(
                    $"Invalid path '{template}'. In segment '{segmentText}' the parameter '{{{text}}}' directly follows another parameter; " +
                    "a literal separator is required between two parameters.");

            ParseParameterCore(template, segmentText, text, registry, allowCatchAll: false,
                out var name, out _, out var isOptional, out var defaultValue, out var constraints);

            if (isOptional)
            {
                if (i != rawParts.Count - 1)
                    throw new InvalidOperationException(
                        $"Invalid path '{template}'. In segment '{segmentText}' the optional parameter '{name}' must be the last part of the segment.");

                // Framework parity: an optional part must be preceded by a literal '.' exactly, which
                // becomes a separator dropped together with the omitted optional value.
                if (parts[i - 1].IsParameter || parts[i - 1].Value != ".")
                    throw new InvalidOperationException(
                        $"Invalid path '{template}'. In segment '{segmentText}' only a period (.) can precede the optional parameter '{name}'.");

                parts[i - 1] = BrouterTemplatePart.Separator();
            }

            parts[i] = BrouterTemplatePart.Parameter(name, isOptional, defaultValue, constraints);
        }

        Parts = Array.AsReadOnly(parts);
    }

    /// <summary>
    /// Splits a segment's raw text into literal and parameter parts. Handles the escaped braces
    /// <c>{{</c> / <c>}}</c> (which become literal single braces) and rejects unbalanced ones.
    /// </summary>
    private static List<(bool IsParameter, string Text)> TokenizeParts(string template, string segment)
    {
        var parts = new List<(bool, string)>();
        var literal = new StringBuilder();
        var i = 0;

        while (i < segment.Length)
        {
            var c = segment[i];

            if (c == '{')
            {
                if (i + 1 < segment.Length && segment[i + 1] == '{')
                {
                    literal.Append('{');
                    i += 2;
                    continue;
                }

                if (literal.Length > 0)
                {
                    parts.Add((false, literal.ToString()));
                    literal.Clear();
                }

                i++;
                var inner = new StringBuilder();
                var closed = false;
                while (i < segment.Length)
                {
                    c = segment[i];
                    if (c == '}')
                    {
                        if (i + 1 < segment.Length && segment[i + 1] == '}')
                        {
                            inner.Append('}');
                            i += 2;
                            continue;
                        }
                        closed = true;
                        i++;
                        break;
                    }
                    if (c == '{')
                    {
                        if (i + 1 < segment.Length && segment[i + 1] == '{')
                        {
                            inner.Append('{');
                            i += 2;
                            continue;
                        }
                        throw new InvalidOperationException(
                            $"Invalid path '{template}'. An unescaped '{{' inside parameter segment '{segment}' is not allowed.");
                    }
                    inner.Append(c);
                    i++;
                }

                if (closed is false)
                    throw new InvalidOperationException($"Invalid path '{template}'. Missing '}}' in parameter segment '{segment}'.");

                if (inner.Length == 0)
                    throw new InvalidOperationException($"Invalid path '{template}'. Empty parameter name in segment '{segment}' is not allowed.");

                parts.Add((true, inner.ToString()));
                continue;
            }

            if (c == '}')
            {
                if (i + 1 < segment.Length && segment[i + 1] == '}')
                {
                    literal.Append('}');
                    i += 2;
                    continue;
                }
                throw new InvalidOperationException($"Invalid path '{template}'. Missing '{{' in parameter segment '{segment}'.");
            }

            literal.Append(c);
            i++;
        }

        if (literal.Length > 0) parts.Add((false, literal.ToString()));
        return parts;
    }

    // Parses the text between a parameter's braces: catch-all stars, the optional '?', the name,
    // ':'-separated constraints (parenthesis-aware so "range(1,5)" and "regex(...)" survive ':' or
    // '=' inside their arguments), and the '=default' suffix.
    private static void ParseParameterCore(string template, string segmentText, string inner, BrouterConstraintRegistry? registry, bool allowCatchAll,
        out string name, out bool isCatchAll, out bool isOptional, out string? defaultValue, out BrouterRouteConstraintBinding[] constraints)
    {
        var s = inner;

        isCatchAll = false;
        if (s.StartsWith("**", StringComparison.Ordinal))
        {
            isCatchAll = true;
            s = s[2..];
        }
        else if (s.StartsWith('*'))
        {
            isCatchAll = true;
            s = s[1..];
        }

        if (isCatchAll && allowCatchAll is false)
            throw new InvalidOperationException(
                $"Invalid path '{template}'. A catch-all parameter is not allowed in the multi-part segment '{segmentText}'.");

        isOptional = s.EndsWith('?');
        if (isOptional) s = s[..^1];

        if (isCatchAll && isOptional)
            throw new ArgumentException($"Catch-all parameter '{{{inner}}}' in route '{template}' cannot be optional.");

        // The name runs to the first ':' (constraints) or '=' (default value).
        string suffix;
        var delim = s.AsSpan().IndexOfAny(':', '=');
        if (delim < 0)
        {
            name = s;
            suffix = string.Empty;
        }
        else
        {
            name = s[..delim];
            suffix = s[delim..];
        }

        if (name.Length == 0)
            throw new InvalidOperationException($"Invalid path '{template}'. Empty parameter name in segment '{segmentText}' is not allowed.");

        var invalidIdx = name.AsSpan().IndexOfAny(BrouterTemplateParser.InvalidParameterNameCharacters);
        if (invalidIdx != -1)
            throw new InvalidOperationException(
                $"Invalid path '{template}'. The character '{name[invalidIdx]}' in parameter segment '{segmentText}' is not allowed.");

        defaultValue = null;
        List<BrouterRouteConstraintBinding>? bindings = null;
        var i = 0;
        while (i < suffix.Length)
        {
            if (suffix[i] == '=')
            {
                defaultValue = suffix[(i + 1)..];
                break;
            }

            // suffix[i] == ':' - scan the constraint token, skipping ':'/'=' inside balanced parentheses.
            i++;
            var start = i;
            var depth = 0;
            while (i < suffix.Length)
            {
                var c = suffix[i];
                if (c == '(') depth++;
                else if (c == ')' && depth > 0) depth--;
                else if ((c == ':' || c == '=') && depth == 0) break;
                i++;
            }

            var token = suffix[start..i];
            (bindings ??= []).Add(new BrouterRouteConstraintBinding(token, BrouterRouteConstraint.Resolve(template, segmentText, token, registry)));
        }

        constraints = bindings?.ToArray() ?? [];

        if (isOptional && defaultValue is not null)
            throw new ArgumentException($"Optional parameter '{{{inner}}}' in route '{template}' cannot have a default value.");
    }

    public bool TryMatch(string segment, StringComparison literalComparison, out object? matchedParameterValue)
    {
        if (IsParameter)
        {
            matchedParameterValue = segment;

            foreach (var binding in Constraints)
            {
                if (binding.Constraint.TryMatch(segment, out var converted) is false)
                {
                    matchedParameterValue = null;
                    return false;
                }

                // Validation-only constraints (min, alpha, regex, ...) must not clobber a typed
                // conversion made earlier in the chain ({id:int:min(0)} still binds an int).
                if (binding.Constraint.ConvertsValue) matchedParameterValue = converted;
            }

            return true;
        }

        matchedParameterValue = null;
        return IsCatchAll || IsSingleWildcard || string.Equals(Value, segment, literalComparison);
    }

    /// <summary>
    /// Matches a complex (multi-part) segment against one URL segment and, on success, publishes
    /// every captured parameter into the supplied dictionaries. Port of ASP.NET Core's
    /// RoutePatternMatcher.MatchComplexSegment (the algorithm the built-in Blazor router runs).
    /// </summary>
    public bool TryMatchComplex(string segment, StringComparison literalComparison,
        Dictionary<string, object?> parameters, Dictionary<string, string[]> constraintsByParameter)
    {
        var parts = Parts!;
        var last = parts.Count - 1;

        // An optional last part ({name}.{ext?}) is tried with the optional value first, then without
        // it (dropping the '.' separator too) - unless the URL segment itself ends with the
        // separator, which must never bind ("doc." is not a match for "{name}.{ext?}").
        if (parts[last].IsParameter && parts[last].IsOptional && parts[last - 1].IsSeparator)
        {
            if (TryMatchComplexCore(segment, literalComparison, last, parameters, constraintsByParameter)) return true;
            if (segment.EndsWith(parts[last - 1].Value, literalComparison)) return false;
            return TryMatchComplexCore(segment, literalComparison, last - 2, parameters, constraintsByParameter);
        }

        return TryMatchComplexCore(segment, literalComparison, last, parameters, constraintsByParameter);
    }

    private bool TryMatchComplexCore(string segment, StringComparison literalComparison, int lastPartIndex,
        Dictionary<string, object?> parameters, Dictionary<string, string[]> constraintsByParameter)
    {
        var parts = Parts!;
        var lastIndex = segment.Length;

        BrouterTemplatePart? parameterNeedsValue = null;
        BrouterTemplatePart? lastLiteral = null;
        List<(BrouterTemplatePart Part, string Raw)>? captured = null;

        // Walk the parts right-to-left, locating each literal in the URL segment (rightmost
        // occurrence) and assigning the text between literals to the pending parameter.
        var index = lastPartIndex;
        while (index >= 0)
        {
            var newLastIndex = lastIndex;
            var part = parts[index];

            if (part.IsParameter)
            {
                parameterNeedsValue = part;
            }
            else
            {
                lastLiteral = part;

                var startIndex = lastIndex;
                // We're at the segment's left edge but still need to place a literal.
                if (startIndex == 0) return false;
                // Leave at least one character for a pending parameter to the literal's right.
                if (parameterNeedsValue is not null) startIndex--;

                var literalIndex = segment.AsSpan(0, startIndex).LastIndexOf(part.Value, literalComparison);
                if (literalIndex < 0) return false;

                // The right-most part, when literal, must sit at the segment's right edge
                // (template "x." must not match URL "ax.b").
                if (index == parts.Count - 1 && literalIndex + part.Value.Length != segment.Length) return false;

                newLastIndex = literalIndex;
            }

            if (parameterNeedsValue is not null && ((lastLiteral is not null && part.IsParameter is false) || index == 0))
            {
                int valueStart, valueLength;
                if (lastLiteral is null || (index == 0 && part.IsParameter))
                {
                    // The left-most part is the pending parameter: it takes everything up to lastIndex.
                    valueStart = 0;
                    valueLength = lastIndex;
                }
                else
                {
                    valueStart = newLastIndex + lastLiteral.Value.Length;
                    valueLength = lastIndex - valueStart;
                }

                // Every parameter in a complex segment must capture at least one character.
                if (valueLength <= 0) return false;

                (captured ??= []).Add((parameterNeedsValue, segment.Substring(valueStart, valueLength)));
                parameterNeedsValue = null;
                lastLiteral = null;
            }

            lastIndex = newLastIndex;
            index--;
        }

        // A left-most literal must consume the segment's left edge (template "x{p}" must not match "ax1").
        if (lastIndex != 0 && parts[0].IsParameter is false) return false;

        if (captured is null) return true;

        // Validate constraints on every captured value before publishing anything, so a failed
        // with-optional attempt can't leak partial bindings into the dictionaries.
        var converted = new object?[captured.Count];
        for (var i = 0; i < captured.Count; i++)
        {
            var (part, raw) = captured[i];
            object? value = raw;
            foreach (var binding in part.Constraints)
            {
                if (binding.Constraint.TryMatch(raw, out var c) is false) return false;
                if (binding.Constraint.ConvertsValue) value = c;
            }
            converted[i] = value;
        }

        for (var i = 0; i < captured.Count; i++)
        {
            var part = captured[i].Part;
            parameters[part.Value] = converted[i];
            constraintsByParameter[part.Value] = part.Constraints.Select(b => b.Name).ToArray();
        }

        return true;
    }

    /// <summary>
    /// A score used to rank competing matches. Higher = more specific.
    /// Heuristic borrowed from React Router v6 and ASP.NET Core's route matcher.
    /// </summary>
    public int Specificity
    {
        get
        {
            if (IsCatchAll) return Constraints.Length > 0 ? 2 : 1;
            if (IsSingleWildcard) return 2;
            // A complex segment ranks with the most-constrained parameter, just below a pure
            // literal - mirroring the framework, which scores complex segments like constrained
            // parameters and literals above both.
            if (IsComplex) return 10;
            if (IsParameter)
            {
                // Cap parameter scores below the literal score (11) so a literal segment always
                // wins a tie at the same depth, even when a parameter declares many constraints.
                // A default-valued parameter matches absent segments just like an optional one,
                // so it shares the optional score.
                var score = (IsOptional || HasDefault ? 4 : 6) + Constraints.Length * 2;
                return Math.Min(score, 10);
            }
            return 11; // literal
        }
    }
}
