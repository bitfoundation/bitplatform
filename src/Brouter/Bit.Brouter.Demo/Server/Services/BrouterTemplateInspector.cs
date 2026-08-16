using System.Collections;
using System.Reflection;
using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Parses a route template with Brouter's own parser and reports what it made of it.
/// <para>
/// The point is that the answer is authoritative: rather than re-implementing the template grammar
/// here - where it would quietly drift from the router and start telling MCP clients things that
/// are not true - the real parser is invoked and its result is read back. It is internal to the
/// library (nothing outside the router has a reason to parse a template), so this reaches it
/// through reflection, and degrades to a plain "unavailable" answer if that surface ever moves.
/// </para>
/// </summary>
public static class BrouterTemplateInspector
{
    private static readonly Lazy<Reflected?> _parser = new(Reflect);

    public static BrouterTemplateInspectionDto Inspect(string template, BrouterConstraintRegistry? constraints)
    {
        template ??= string.Empty;

        var parser = _parser.Value;
        if (parser is null)
        {
            return new BrouterTemplateInspectionDto
            {
                Template = template,
                IsValid = false,
                Error = "The route template parser could not be reached in this build, so the template was not checked."
            };
        }

        object? parsed;
        try
        {
            parsed = parser.ParseTemplate.Invoke(null, [template, constraints]);
        }
        catch (TargetInvocationException exception)
        {
            // Exactly the exception the router throws while rendering an invalid <Broute Path="...">.
            return new BrouterTemplateInspectionDto
            {
                Template = template,
                IsValid = false,
                Error = exception.InnerException?.Message ?? exception.Message
            };
        }
        catch (Exception exception)
        {
            return new BrouterTemplateInspectionDto
            {
                Template = template,
                IsValid = false,
                Error = exception.Message
            };
        }

        if (parsed is null)
        {
            return Unavailable(template);
        }

        try
        {
            var segments = (parser.Segments.GetValue(parsed) as IEnumerable)?.Cast<object>().ToArray() ?? [];
            var dtos = new BrouterTemplateSegmentDto[segments.Length];

            for (int i = 0; i < segments.Length; i++)
            {
                dtos[i] = Describe(parser, segments[i]);
            }

            return new BrouterTemplateInspectionDto
            {
                Template = template,
                IsValid = true,
                NormalizedTemplate = parser.Template.GetValue(parsed) as string,
                Specificity = dtos.Sum(s => s.Specificity),
                ParameterNames = [.. dtos.SelectMany(s => s.ParameterNames ?? [])],
                Segments = dtos,
                Notes = [.. Notes(dtos)]
            };
        }
        catch (Exception)
        {
            // The template parsed; only reading the result back failed - a property that moved or
            // changed shape. Same answer as an unreachable parser: unavailable, never a wrong report.
            return Unavailable(template);
        }
    }

    private static BrouterTemplateInspectionDto Unavailable(string template) => new()
    {
        Template = template,
        IsValid = false,
        Error = "The template could not be parsed."
    };

    /// <summary>
    /// Parses a whole set of templates and reports how they relate: which one the router prefers
    /// when more than one matches, and which of them are indistinguishable.
    /// </summary>
    public static BrouterRouteTableAnalysisDto Analyze(IEnumerable<string> templates, BrouterConstraintRegistry? constraints)
    {
        var inspections = templates.Where(t => string.IsNullOrWhiteSpace(t) is false)
                                   .Select(t => Inspect(t.Trim(), constraints))
                                   .ToArray();

        var entries = inspections.Select(inspection => new BrouterRouteTableEntryDto
        {
            Template = inspection.Template,
            IsValid = inspection.IsValid,
            Error = inspection.Error,
            Specificity = inspection.Specificity,
            Shape = inspection.IsValid ? Shape(inspection) : null
        }).ToArray();

        // Specificity is the router's tie-break between routes that all match a URL.
        var ordered = entries.OrderByDescending(e => e.IsValid)
                             .ThenByDescending(e => e.Specificity)
                             .Select((entry, index) => entry with { MatchOrder = index + 1 })
                             .ToArray();

        var ambiguous = ordered.Where(e => e.Shape is not null)
                               .GroupBy(e => e.Shape!, StringComparer.Ordinal)
                               .Where(group => group.Count() > 1)
                               .Select(group => group.Select(e => e.Template).ToArray())
                               .ToArray();

        var notes = new List<string>();

        if (ambiguous.Length > 0)
        {
            notes.Add("Templates sharing a shape match exactly the same URLs, so the winner would come down to " +
                      "registration order alone - Brouter refuses to register them and throws. Change or remove one of each group.");
        }

        if (ordered.Any(e => e.IsValid is false))
        {
            notes.Add("An invalid template throws while its <Broute> initializes, so the route never registers.");
        }

        notes.Add("Specificity ranks routes that ALL match the same URL; it does not decide whether a route matches at all. " +
                  "This analysis treats the templates as a flat set - nesting depth and index routes ('' under a parent) add " +
                  "further tie-breaks that only exist once the routes sit in a tree.");

        return new BrouterRouteTableAnalysisDto { Routes = ordered, Ambiguous = ambiguous, Notes = [.. notes] };
    }

    /// <summary>
    /// The template stripped of everything matching ignores - parameter names above all, since
    /// "/users/{id}" and "/users/{userId}" accept exactly the same URLs.
    /// </summary>
    private static string Shape(BrouterTemplateInspectionDto inspection)
    {
        return string.Join('/', (inspection.Segments ?? []).Select(segment => segment.Kind switch
        {
            "Literal" => segment.Value.ToLowerInvariant(),
            "Wildcard" => "*",
            "CatchAll" => $"{{**{string.Concat((segment.Constraints ?? []).Select(c => $":{c.ToLowerInvariant()}"))}}}",
            "Complex" => ComplexShape(segment),
            _ => $"{{{string.Concat((segment.Constraints ?? []).Select(c => $":{c.ToLowerInvariant()}"))}{(segment.IsOptional ? "?" : null)}}}"
        }));
    }

    private static string ComplexShape(BrouterTemplateSegmentDto segment)
    {
        // The literal text between the parameters is what distinguishes two complex segments, and it
        // survives in the segment's own text once the parameter names are blanked out.
        var shape = segment.Value;

        foreach (var name in segment.ParameterNames ?? [])
        {
            shape = shape.Replace($"{{{name}", "{", StringComparison.OrdinalIgnoreCase);
        }

        return shape.ToLowerInvariant();
    }

    private static BrouterTemplateSegmentDto Describe(Reflected parser, object segment)
    {
        var parts = parser.Parts.GetValue(segment);

        var kind = (bool)parser.IsCatchAll.GetValue(segment)! ? "CatchAll"
                 : (bool)parser.IsSingleWildcard.GetValue(segment)! ? "Wildcard"
                 : parts is not null ? "Complex"
                 : (bool)parser.IsParameter.GetValue(segment)! ? "Parameter"
                 : "Literal";

        var names = ((IEnumerable)parser.ParameterNames.GetValue(segment)!).Cast<string>().ToArray();

        var constraints = ConstraintNames(parser, parser.Constraints.GetValue(segment));
        if (parts is not null)
        {
            // A complex segment carries its constraints on the individual parts.
            constraints = [.. ((IEnumerable)parts).Cast<object>()
                .SelectMany(part => ConstraintNames(parser, parser.PartConstraints.GetValue(part)))];
        }

        var value = (string)parser.Value.GetValue(segment)!;
        var isOptional = (bool)parser.IsOptional.GetValue(segment)!;
        var defaultValue = (string?)parser.DefaultValue.GetValue(segment);

        return new BrouterTemplateSegmentDto
        {
            // A parameter segment stores the bare parameter name; spelled back out the way it was
            // written, it stays recognizable in the notes below.
            Value = kind is "Parameter" or "CatchAll"
                ? $"{{{(kind == "CatchAll" ? "*" : null)}{value}{string.Concat(constraints.Select(c => $":{c}"))}{(isOptional ? "?" : null)}{(defaultValue is null ? null : $"={defaultValue}")}}}"
                : value,
            Kind = kind,
            ParameterNames = names.Length == 0 ? null : names,
            Constraints = constraints.Length == 0 ? null : constraints,
            IsOptional = isOptional,
            DefaultValue = defaultValue,
            Specificity = (int)parser.Specificity.GetValue(segment)!
        };
    }

    private static string[] ConstraintNames(Reflected parser, object? bindings)
    {
        if (bindings is not IEnumerable enumerable) return [];

        return [.. enumerable.Cast<object>().Select(b => (string)parser.ConstraintName.GetValue(b)!)];
    }

    /// <summary>The behaviors of a parsed template that are easy to get wrong.</summary>
    private static IEnumerable<string> Notes(BrouterTemplateSegmentDto[] segments)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];

            if (segment.IsOptional && i < segments.Length - 1)
            {
                yield return $"'{segment.Value}' is an optional parameter that is not last. Like the built-in Blazor router, " +
                             "it parses but matches as required - only the trailing run of optional/default-valued segments can be omitted by a shorter URL.";
            }

            if (segment.Kind == "CatchAll")
            {
                yield return $"'{segment.Value}' is a catch-all: it binds the whole remainder of the URL (slashes included) and must be the last segment. " +
                             "{*name} and {**name} match identically.";
            }

            if (segment.Kind == "Complex")
            {
                yield return $"'{segment.Value}' is a complex segment - several parameters inside one URL segment. It is matched right-to-left, " +
                             "and each of its parameters needs at least one character (a declared default is used for URL generation only).";
            }

            if (segment.DefaultValue is not null)
            {
                yield return $"'{segment.Value}' binds \"{segment.DefaultValue}\" when the URL omits the segment.";
            }

            if (segment.Constraints is { Length: > 1 })
            {
                yield return $"'{segment.Value}' chains constraints: every one of them has to accept the value, and the last TYPE constraint " +
                             "decides the type the parameter binds as.";
            }
        }
    }

    private static Reflected? Reflect()
    {
        try
        {
            var assembly = typeof(BrouterLink).Assembly;

            var parserType = assembly.GetType("Bit.Brouter.BrouterTemplateParser", throwOnError: false);
            var templateType = assembly.GetType("Bit.Brouter.BrouterRouteTemplate", throwOnError: false);
            var segmentType = assembly.GetType("Bit.Brouter.BrouterTemplateSegment", throwOnError: false);
            var partType = assembly.GetType("Bit.Brouter.BrouterTemplatePart", throwOnError: false);
            var bindingType = assembly.GetType("Bit.Brouter.BrouterRouteConstraintBinding", throwOnError: false);

            if (parserType is null || templateType is null || segmentType is null || partType is null || bindingType is null) return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var parse = parserType.GetMethod("ParseTemplate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (parse is null) return null;

            // Every property is read unconditionally later on, so a single one that moved has to fail
            // the whole reflection - the tool then says "unavailable" instead of throwing per call.
            var template = templateType.GetProperty("Template", flags);
            var segments = templateType.GetProperty("TemplateSegments", flags);
            var value = segmentType.GetProperty("Value", flags);
            var isParameter = segmentType.GetProperty("IsParameter", flags);
            var isCatchAll = segmentType.GetProperty("IsCatchAll", flags);
            var isSingleWildcard = segmentType.GetProperty("IsSingleWildcard", flags);
            var isOptional = segmentType.GetProperty("IsOptional", flags);
            var defaultValue = segmentType.GetProperty("DefaultValue", flags);
            var constraints = segmentType.GetProperty("Constraints", flags);
            var parts = segmentType.GetProperty("Parts", flags);
            var parameterNames = segmentType.GetProperty("ParameterNames", flags);
            var specificity = segmentType.GetProperty("Specificity", flags);
            var partConstraints = partType.GetProperty("Constraints", flags);
            var constraintName = bindingType.GetProperty("Name", flags);

            if (template is null || segments is null || value is null || isParameter is null || isCatchAll is null ||
                isSingleWildcard is null || isOptional is null || defaultValue is null || constraints is null ||
                parts is null || parameterNames is null || specificity is null || partConstraints is null ||
                constraintName is null) return null;

            return new Reflected
            {
                ParseTemplate = parse,
                Template = template,
                Segments = segments,
                Value = value,
                IsParameter = isParameter,
                IsCatchAll = isCatchAll,
                IsSingleWildcard = isSingleWildcard,
                IsOptional = isOptional,
                DefaultValue = defaultValue,
                Constraints = constraints,
                Parts = parts,
                ParameterNames = parameterNames,
                Specificity = specificity,
                PartConstraints = partConstraints,
                ConstraintName = constraintName
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed record Reflected
    {
        public required MethodInfo ParseTemplate { get; init; }
        public required PropertyInfo Template { get; init; }
        public required PropertyInfo Segments { get; init; }
        public required PropertyInfo Value { get; init; }
        public required PropertyInfo IsParameter { get; init; }
        public required PropertyInfo IsCatchAll { get; init; }
        public required PropertyInfo IsSingleWildcard { get; init; }
        public required PropertyInfo IsOptional { get; init; }
        public required PropertyInfo DefaultValue { get; init; }
        public required PropertyInfo Constraints { get; init; }
        public required PropertyInfo Parts { get; init; }
        public required PropertyInfo ParameterNames { get; init; }
        public required PropertyInfo Specificity { get; init; }
        public required PropertyInfo PartConstraints { get; init; }
        public required PropertyInfo ConstraintName { get; init; }
    }
}
