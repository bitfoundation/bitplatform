using System;
using System.Collections.Generic;

namespace Bit.Brouter.Generators;

/// <summary>
/// A standalone (generator-side) parser for Brouter/Blazor route templates, deliberately minimal:
/// it only extracts what URL *generation* needs - segment kinds, parameter names, the CLR type of
/// the last type constraint, optionality, default values. Validation stays the runtime parser's
/// job; anything this parser can't make sense of (e.g. complex multi-part segments) yields null
/// and the route is simply skipped by the generator.
/// </summary>
internal static class RouteTemplateParser
{
    private static readonly HashSet<string> _typeConstraints = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "long", "bool", "guid", "datetime", "decimal", "double", "float",
    };

    /// <summary>
    /// Maps a route constraint token to the C# type keyword its converted value has. Mirrors the
    /// runtime rule that the last *type* constraint's conversion wins; validation-only constraints
    /// (min, alpha, regex, ...) keep the raw string.
    /// </summary>
    private static string MapConstraint(string constraint) => constraint.ToLowerInvariant() switch
    {
        "int" => "int",
        "long" => "long",
        "bool" => "bool",
        "guid" => "global::System.Guid",
        "datetime" => "global::System.DateTime",
        "decimal" => "decimal",
        "double" => "double",
        "float" => "float",
        _ => "string",
    };

    /// <summary>Parses <paramref name="template"/> into segments, or null when it isn't generatable.</summary>
    public static RouteModel? Parse(string template, string? name)
    {
        var normalized = template.Trim();
        if (normalized.StartsWith("~/", StringComparison.Ordinal)) normalized = normalized.Substring(2);
        normalized = normalized.Trim('/');
        var segments = new List<RouteSegment>();

        if (normalized.Length > 0)
        {
            foreach (var raw in normalized.Split('/'))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;

                if (part == "*" || part == "**")
                {
                    segments.Add(new RouteSegment(SegmentKind.Wildcard, part, "string", IsOptional: false));
                    continue;
                }

                if (part.Length >= 2 && part[0] == '{' && part[part.Length - 1] == '}')
                {
                    var inner = part.Substring(1, part.Length - 2).Trim();
                    if (inner.Length == 0) return null;

                    if (inner.StartsWith("*", StringComparison.Ordinal))
                    {
                        // Catch-all: "{*path}" or "{**path}"; constraints are allowed and stripped -
                        // catch-all values stay strings at runtime.
                        var catchAllName = inner.TrimStart('*').TrimEnd('?').Trim();
                        var colon = catchAllName.IndexOf(':');
                        if (colon >= 0) catchAllName = catchAllName.Substring(0, colon).Trim();
                        if (IsValidParamName(catchAllName) is false) return null;
                        segments.Add(new RouteSegment(SegmentKind.CatchAll, catchAllName, "string", IsOptional: true));
                        continue;
                    }

                    var optional = inner.EndsWith("?", StringComparison.Ordinal);
                    if (optional) inner = inner.Substring(0, inner.Length - 1);

                    var pieces = SplitNameConstraintsAndDefault(inner, out var defaultValue);
                    var paramName = pieces[0].Trim();
                    if (IsValidParamName(paramName) is false) return null;
                    if (optional && defaultValue is not null) return null; // invalid at runtime; skip

                    // Last type constraint wins the conversion, mirroring the runtime; validation-only
                    // constraints (min(1), alpha, regex(...), ...) don't change the CLR type.
                    var clrType = "string";
                    for (var i = pieces.Count - 1; i >= 1; i--)
                    {
                        var token = pieces[i].Trim();
                        var paren = token.IndexOf('(');
                        if (paren >= 0) token = token.Substring(0, paren);
                        if (_typeConstraints.Contains(token))
                        {
                            clrType = MapConstraint(token);
                            break;
                        }
                    }

                    segments.Add(new RouteSegment(SegmentKind.Parameter, paramName, clrType, optional, defaultValue));
                    continue;
                }

                // A complex segment (braces mixed with literals, e.g. "{name}.{ext}") is beyond
                // this parser; skip the route.
                if (part.IndexOf('{') >= 0 || part.IndexOf('}') >= 0) return null;

                segments.Add(new RouteSegment(SegmentKind.Literal, part, "string", IsOptional: false));
            }
        }

        // Runtime parity: a middle optional or default-valued parameter (one followed by a
        // non-skippable segment) is required at match time, so the generated method must require
        // its argument too (C# also rejects an optional argument before a required one).
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment.Kind != SegmentKind.Parameter) continue;
            if (segment.IsOptional is false && segment.DefaultValue is null) continue;

            for (var j = i + 1; j < segments.Count; j++)
            {
                if (IsSkippable(segments[j]) is false)
                {
                    segments[i] = segment with { IsOptional = false, DefaultValue = null };
                    break;
                }
            }
        }

        return new RouteModel(normalized, name, new EquatableArray<RouteSegment>(segments.ToArray()));
    }

    private static bool IsSkippable(RouteSegment segment) =>
        segment.Kind == SegmentKind.CatchAll ||
        (segment.Kind == SegmentKind.Parameter && (segment.IsOptional || segment.DefaultValue is not null));

    /// <summary>
    /// Splits a parameter's inner text into [name, constraint, constraint, ...] and extracts the
    /// trailing "=default". Parenthesis-aware so "range(1,5)" and "regex(...)" survive ':' or '='
    /// inside their arguments (mirrors the runtime parser).
    /// </summary>
    private static List<string> SplitNameConstraintsAndDefault(string inner, out string? defaultValue)
    {
        var pieces = new List<string>();
        defaultValue = null;

        var start = 0;
        var depth = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];
            if (c == '(') depth++;
            else if (c == ')' && depth > 0) depth--;
            else if (depth == 0 && c == ':')
            {
                pieces.Add(inner.Substring(start, i - start));
                start = i + 1;
            }
            else if (depth == 0 && c == '=')
            {
                pieces.Add(inner.Substring(start, i - start));
                defaultValue = inner.Substring(i + 1);
                return pieces;
            }
        }

        pieces.Add(inner.Substring(start));
        return pieces;
    }

    private static bool IsValidParamName(string name)
    {
        if (name.Length == 0) return false;
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) is false && c != '_') return false;
        }
        return char.IsDigit(name[0]) is false;
    }
}
