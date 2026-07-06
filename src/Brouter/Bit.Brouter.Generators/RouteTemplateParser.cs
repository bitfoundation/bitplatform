using System;
using System.Collections.Generic;

namespace Bit.Brouter.Generators;

/// <summary>
/// A standalone (generator-side) parser for Brouter/Blazor route templates, deliberately minimal:
/// it only extracts what URL *generation* needs - segment kinds, parameter names, the CLR type of
/// the last constraint, optionality. Validation stays the runtime parser's job; anything this
/// parser can't make sense of yields null and the route is simply skipped by the generator.
/// </summary>
internal static class RouteTemplateParser
{
    /// <summary>
    /// Maps a route constraint token to the C# type keyword its converted value has. Mirrors the
    /// runtime rule that the LAST constraint's conversion wins. Unknown (custom) constraints keep
    /// the raw string.
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
        var normalized = template.Trim().Trim('/');
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

                    if (inner.StartsWith("**", StringComparison.Ordinal))
                    {
                        var catchAllName = inner.Substring(2).TrimEnd('?').Trim();
                        if (IsValidParamName(catchAllName) is false) return null;
                        segments.Add(new RouteSegment(SegmentKind.CatchAll, catchAllName, "string", IsOptional: true));
                        continue;
                    }

                    var optional = inner.EndsWith("?", StringComparison.Ordinal);
                    if (optional) inner = inner.Substring(0, inner.Length - 1);

                    var pieces = inner.Split(':');
                    var paramName = pieces[0].Trim();
                    if (IsValidParamName(paramName) is false) return null;

                    // Last constraint wins the conversion, mirroring the runtime.
                    var clrType = pieces.Length > 1 ? MapConstraint(pieces[pieces.Length - 1].Trim()) : "string";
                    segments.Add(new RouteSegment(SegmentKind.Parameter, paramName, clrType, optional));
                    continue;
                }

                // A literal containing braces mid-string is beyond this parser; skip the route.
                if (part.IndexOf('{') >= 0 || part.IndexOf('}') >= 0) return null;

                segments.Add(new RouteSegment(SegmentKind.Literal, part, "string", IsOptional: false));
            }
        }

        return new RouteModel(normalized, name, new EquatableArray<RouteSegment>(segments.ToArray()));
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
