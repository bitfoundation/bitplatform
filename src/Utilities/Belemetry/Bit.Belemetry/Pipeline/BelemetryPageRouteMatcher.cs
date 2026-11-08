using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>
/// The app's page routes, matched against a page's path the way the Blazor router matches them, for the
/// <c>url.template</c> of the page views .NET records itself (WebAssembly, Hybrid, a server without a page). The C# side
/// of <c>Scripts/routes.ts</c>, which the script's page views use: one rule set (the order, the segments of several
/// parts, the base path), except for the constraints, which here are the framework's own parsers and there are
/// approximations of them.
/// </summary>
internal sealed class BelemetryPageRouteMatcher
{
    // A route's regex runs on every page view .NET records; one that takes this long is the app's bug, not a match.
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    private readonly Route[] _routes;

    /// <param name="templates">Route templates (<c>/orders/{id:int}</c>); a malformed one is skipped, a repeated one listed once.</param>
    public BelemetryPageRouteMatcher(IEnumerable<string?> templates)
    {
        var routes = new List<Route>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in templates)
        {
            if (raw is null) continue;
            var trimmed = raw.Trim();
            if (trimmed.StartsWith('~')) trimmed = trimmed[1..];
            trimmed = trimmed.Trim('/');
            var template = "/" + trimmed;
            if (seen.Add(template) is false) continue;

            var segments = new List<Segment>();
            var valid = true;
            foreach (var text in trimmed.Length == 0 ? Array.Empty<string>() : trimmed.Split('/'))
            {
                if (text.Length == 0 || ParseSegment(text) is not { } segment)
                {
                    valid = false;
                    break;
                }
                segments.Add(segment);
            }
            if (valid) routes.Add(new(template, [.. segments]));
        }

        // As the script orders them: by precedence digits, a shorter prefix first, then by the lower-cased template.
        routes.Sort((a, b) => string.CompareOrdinal(a.Precedence, b.Precedence) is var byPrecedence and not 0
            ? byPrecedence
            : string.CompareOrdinal(a.Template.ToLowerInvariant(), b.Template.ToLowerInvariant()));
        _routes = [.. routes];
    }

    /// <summary>The templates, in the order they are tried.</summary>
    public IEnumerable<string> Templates => _routes.Select(r => r.Template);

    /// <summary>
    /// The template of the first route that matches <paramref name="path"/> below <paramref name="basePath"/> (a directory:
    /// <c>/</c>, <c>/shop/</c>), written after that base path; null when none matches.
    /// </summary>
    public string? Match(string path, string basePath)
    {
        if (_routes.Length == 0) return null;
        var relative = path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) ? path[basePath.Length..]
                     : string.Equals(path + "/", basePath, StringComparison.OrdinalIgnoreCase) ? string.Empty
                     : null;
        if (relative is null) return null;

        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        foreach (var route in _routes)
        {
            if (Matches(route, segments)) return basePath[..^1] + route.Template;
        }
        return null;
    }

    /// <summary>The template of a page's path: the route it matches, else the path with its ids replaced.</summary>
    public string Template(string path, string basePath) => Match(path, basePath) ?? BelemetryUrlTemplate.TemplatePath(path);

    /// <summary>The directory of a base URI (Blazor's <c>NavigationManager.BaseUri</c>), which the routes are relative to; <c>/</c> without one.</summary>
    public static string BasePath(string? baseUri)
    {
        if (baseUri is null || Uri.TryCreate(baseUri, UriKind.Absolute, out var uri) is false) return "/";
        var path = uri.AbsolutePath;
        return path[..(path.LastIndexOf('/') + 1)] is { Length: > 0 } directory ? directory : "/";
    }

    private static bool Matches(Route route, string[] segments)
    {
        var t = route.Segments;
        for (var i = 0; i < t.Length; i++)
        {
            var s = t[i];
            var single = s.Single;
            if (single is { CatchAll: true })
            {
                // The rest of the path, if any; a parameter left out is not checked.
                return i >= segments.Length || single.Passes(string.Join('/', segments, i, segments.Length - i));
            }
            if (i >= segments.Length)
            {
                for (var j = i; j < t.Length; j++)
                {
                    if (t[j].Single is not { } p || (p.Optional || p.CatchAll) is false) return false;
                }
                return true;
            }

            var value = segments[i];
            if (s.Literal is not null)
            {
                if (string.Equals(value, s.Literal, StringComparison.OrdinalIgnoreCase) is false) return false;
            }
            else if (s.Parts is not null)
            {
                if (MatchParts(s.Parts, value) is false) return false;
            }
            else if (single!.Passes(value) is false)
            {
                return false;
            }
        }
        return segments.Length == t.Length;
    }

    /// <summary>
    /// A segment of several parts, as ASP.NET Core matches it: from the right (<c>{name}.{ext}</c> splits
    /// <c>archive.tar.gz</c> into <c>archive.tar</c> and <c>gz</c>). An optional last parameter after a <c>.</c> may be left
    /// out with its dot (<c>{name}.{ext?}</c> takes <c>report</c>), unless the value ends with the dot.
    /// </summary>
    private static bool MatchParts(Part[] parts, string value)
    {
        var last = parts.Length - 1;
        if (parts[last].Parameter is { Optional: true } && parts[last - 1].Literal == ".")
        {
            if (MatchPartsFrom(parts, value, last)) return true;
            return value.EndsWith('.') is false && MatchPartsFrom(parts, value, last - 2);
        }
        return MatchPartsFrom(parts, value, last);
    }

    /// <summary>
    /// ASP.NET Core's <c>RoutePatternMatcher.MatchComplexSegmentCore</c>, from the part at <paramref name="from"/> leftwards:
    /// each literal at its last occurrence left of what the parts after it took, a parameter the text up to the literal
    /// after it. The constraints are checked on that one split only, as the router does.
    /// </summary>
    private static bool MatchPartsFrom(Part[] parts, string value, int from)
    {
        var captured = new List<(Parameter Parameter, string Value)>();
        var lastIndex = value.Length;
        Parameter? pending = null;
        string? lastLiteral = null;
        for (var i = from; i >= 0; i--)
        {
            var part = parts[i];
            var newLastIndex = lastIndex;
            if (part.Parameter is { } parameter)
            {
                pending = parameter;
            }
            else
            {
                var literal = part.Literal!;
                lastLiteral = literal;
                // A parameter waiting for its value needs at least one character.
                var end = pending is null ? lastIndex : lastIndex - 1;
                if (end <= 0) return false;
                var index = value.AsSpan(0, end).LastIndexOf(literal, StringComparison.OrdinalIgnoreCase);
                if (index < 0) return false;
                // A literal that ends the template ends the value.
                if (i == parts.Length - 1 && index + literal.Length != value.Length) return false;
                newLastIndex = index;
            }

            if (pending is not null && ((lastLiteral is not null && part.Literal is not null) || i == 0))
            {
                var start = lastLiteral is null || part.Parameter is not null ? 0 : newLastIndex + lastLiteral.Length;
                if (lastIndex - start <= 0) return false;
                captured.Add((pending, value[start..lastIndex]));
                pending = null;
                lastLiteral = null;
            }
            lastIndex = newLastIndex;
        }

        if (lastIndex != 0 && parts[0].Literal is not null) return false;
        foreach (var (parameter, text) in captured)
        {
            if (parameter.Passes(text) is false) return false;
        }
        return true;
    }

    /// <summary>One <c>/</c>-separated part of a template; null when it is malformed (an unclosed brace).</summary>
    private static Segment? ParseSegment(string text)
    {
        var parts = new List<Part>();
        var literal = new System.Text.StringBuilder();
        for (var i = 0; i < text.Length;)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (c == '{' && next == '{')
            {
                literal.Append('{');
                i += 2;
            }
            else if (c == '}' && next == '}')
            {
                literal.Append('}');
                i += 2;
            }
            else if (c == '{')
            {
                // The parameter ends at the first single '}'; '}}' inside it is an escaped brace (in a regex constraint).
                var end = i + 1;
                while (end < text.Length && (text[end] == '}' && (end + 1 >= text.Length || text[end + 1] != '}')) is false)
                {
                    end += text[end] == '}' ? 2 : 1;
                }
                if (end >= text.Length) return null;
                if (literal.Length > 0) parts.Add(new(literal.ToString(), null));
                literal.Clear();
                parts.Add(new(null, ParseParameter(text[(i + 1)..end].Replace("{{", "{").Replace("}}", "}"))));
                i = end + 1;
            }
            else
            {
                literal.Append(c);
                i++;
            }
        }
        if (literal.Length > 0) parts.Add(new(literal.ToString(), null));

        if (parts is [{ Literal: { } only }]) return new(1, only, null, null);
        if (parts is [{ Parameter: { } p }]) return new((p.CatchAll ? 5 : 3) - (p.Constrained ? 1 : 0), null, null, p);
        return new(2, null, [.. parts], null);
    }

    /// <summary><c>name</c>, <c>*rest</c>, <c>id:int</c>, <c>id:int?</c>, <c>page=1</c>, <c>ssn:regex(^\d{3}$)</c> - the inside of a parameter's braces.</summary>
    private static Parameter ParseParameter(string inner)
    {
        var text = inner.Trim();
        var catchAll = false;
        if (text.StartsWith("**", StringComparison.Ordinal))
        {
            catchAll = true;
            text = text[2..];
        }
        else if (text.StartsWith('*'))
        {
            catchAll = true;
            text = text[1..];
        }

        var parameter = new Parameter { CatchAll = catchAll };
        var pos = 0;
        while (pos < text.Length && text[pos] is not (':' or '=' or '?')) pos++;
        while (pos < text.Length)
        {
            if (text[pos] == ':')
            {
                var start = ++pos;
                var depth = 0;
                for (; pos < text.Length; pos++)
                {
                    var d = text[pos];
                    if (d == '(') depth++;
                    else if (d == ')') depth = Math.Max(0, depth - 1);
                    else if (depth == 0 && (d is ':' or '=' || (d == '?' && pos == text.Length - 1))) break;
                }
                parameter.Constrained = true;
                if (Constraint(text[start..pos]) is { } check) parameter.Checks.Add(check);
            }
            else
            {
                // A default value (`=...`) or `?`: the parameter may be left out.
                parameter.Optional = catchAll is false;
                break;
            }
        }
        return parameter;
    }

    /// <summary>The check of one inline constraint, as the framework runs it; null for one only the app knows (still more specific).</summary>
    private static Func<string, bool>? Constraint(string spec)
    {
        var open = spec.IndexOf('(');
        var close = spec.LastIndexOf(')');
        var name = (open < 0 ? spec : spec[..open]).Trim().ToLowerInvariant();
        var argument = open < 0 ? string.Empty : spec[(open + 1)..(close > open ? close : spec.Length)];
        var args = argument.Split(',').Select(a => long.TryParse(a.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : (long?)null).ToArray();
        var invariant = CultureInfo.InvariantCulture;

        switch (name)
        {
            case "int": return v => int.TryParse(v, NumberStyles.Integer, invariant, out _);
            case "long": return v => long.TryParse(v, NumberStyles.Integer, invariant, out _);
            case "bool": return v => bool.TryParse(v, out _);
            case "guid": return v => Guid.TryParse(v, out _);
            case "decimal": return v => decimal.TryParse(v, NumberStyles.Number, invariant, out _);
            case "double": return v => double.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, invariant, out _);
            case "float": return v => float.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, invariant, out _);
            case "datetime": return v => DateTime.TryParse(v, invariant, DateTimeStyles.None, out _);
            case "alpha": return v => v.All(char.IsAsciiLetter);
            case "nonfile": return v => IsFileName(v) is false;
            case "file": return IsFileName;
            case "required": return v => v.Length > 0;
        }

        // The numeric arguments' constraints; a malformed argument, which the router would refuse, matches nothing.
        if (args.Any(a => a is null)) return name is "length" or "minlength" or "maxlength" or "min" or "max" or "range" ? _ => false : Other(name, argument);
        return name switch
        {
            "length" => args.Length > 1 ? v => v.Length >= args[0] && v.Length <= args[1] : v => v.Length == args[0],
            "minlength" => v => v.Length >= args[0],
            "maxlength" => v => v.Length <= args[0],
            "min" => v => long.TryParse(v, NumberStyles.Integer, invariant, out var n) && n >= args[0],
            "max" => v => long.TryParse(v, NumberStyles.Integer, invariant, out var n) && n <= args[0],
            "range" => args.Length > 1 ? v => long.TryParse(v, NumberStyles.Integer, invariant, out var n) && n >= args[0] && n <= args[1] : _ => false,
            _ => Other(name, argument),
        };
    }

    /// <summary>The check of a constraint without numeric arguments that is not one of the framework's simple ones: <c>regex</c>, else null (the app's own).</summary>
    private static Func<string, bool>? Other(string name, string argument)
    {
        if (name != "regex") return null;
        try
        {
            // As ASP.NET Core's RegexRouteConstraint: case-insensitive, and anchored only if the pattern anchors itself.
            var regex = new Regex(argument, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, _regexTimeout);
            return v =>
            {
                try
                {
                    return regex.IsMatch(v);
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>ASP.NET Core's file name test: the last segment has a dot, and something other than dots after its first one.</summary>
    private static bool IsFileName(string value)
    {
        var name = value.AsSpan(value.LastIndexOf('/') + 1);
        var dot = name.IndexOf('.');
        return dot >= 0 && name[(dot + 1)..].ContainsAnyExcept('.');
    }

    private sealed class Route(string template, Segment[] segments)
    {
        /// <summary>The template as the app declared it, with one leading <c>/</c> and no trailing one.</summary>
        public string Template { get; } = template;

        public Segment[] Segments { get; } = segments;

        /// <summary>ASP.NET Core's inbound precedence, one digit per segment.</summary>
        public string Precedence { get; } = string.Concat(segments.Select(s => s.Rank));
    }

    /// <param name="Rank">
    /// ASP.NET Core's inbound precedence digit, which the Blazor router orders its routes by: 1 a literal, 2 a constrained
    /// parameter or a segment of several parts, 3 a parameter, 4 a constrained catch-all, 5 a catch-all.
    /// </param>
    /// <param name="Literal">A literal segment.</param>
    /// <param name="Parts">A segment of several parts (<c>{name}.{extension}</c>): its literals and parameters, in order.</param>
    /// <param name="Single">A segment that is one parameter.</param>
    private sealed record Segment(int Rank, string? Literal, Part[]? Parts, Parameter? Single);

    private sealed record Part(string? Literal, Parameter? Parameter);

    private sealed class Parameter
    {
        public List<Func<string, bool>> Checks { get; } = [];

        public bool Constrained { get; set; }

        public bool Optional { get; set; }

        public bool CatchAll { get; set; }

        public bool Passes(string value)
        {
            foreach (var check in Checks)
            {
                if (check(value) is false) return false;
            }
            return true;
        }
    }
}
