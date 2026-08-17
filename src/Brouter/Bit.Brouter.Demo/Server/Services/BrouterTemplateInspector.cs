using System.Text;
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
                Shape = string.Join('/', segments.Select(segment => SegmentShape(parser, segment))),
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
            Shape = inspection.Shape
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
    /// One segment stripped of everything matching ignores - parameter names above all, since
    /// "/users/{id}" and "/users/{userId}" accept exactly the same URLs.
    /// <para>
    /// This mirrors the router's own BuildTemplateCollisionKey segment for segment, because that is
    /// the function deciding which registrations it refuses as ambiguous. What it keeps is kept here
    /// too: a declared default (so "{page}" and "{page=1}" stay distinct - they bind different
    /// values for the same URL), the constraints of a catch-all, and the literal text between the
    /// parameters of a complex segment.
    /// </para>
    /// </summary>
    private static string SegmentShape(Reflected parser, object segment)
    {
        var builder = new StringBuilder();

        if ((bool)parser.IsCatchAll.GetValue(segment)!)
        {
            // The literal "**" and a "{**rest}" parameter unify: a catch-all's name and optional
            // flag change nothing, it already matches zero or more segments. Constraints do not.
            builder.Append("**");

            foreach (var constraint in ConstraintNames(parser, parser.Constraints.GetValue(segment)))
            {
                builder.Append(':').Append(constraint.ToLowerInvariant());
            }
        }
        else if (parser.Parts.GetValue(segment) is IEnumerable parts)
        {
            foreach (var part in parts.Cast<object>())
            {
                if ((bool)parser.PartIsParameter.GetValue(part)! is false)
                {
                    AppendShapeLiteral(builder, (string)parser.PartValue.GetValue(part)!);
                    continue;
                }

                builder.Append('{')
                       .Append(string.Join(':', ConstraintNames(parser, parser.PartConstraints.GetValue(part)).Select(c => c.ToLowerInvariant())));
                if ((bool)parser.PartIsOptional.GetValue(part)!) builder.Append('?');
                builder.Append('}');
            }
        }
        else if ((bool)parser.IsParameter.GetValue(segment)!)
        {
            builder.Append('{')
                   .Append(string.Join(':', ConstraintNames(parser, parser.Constraints.GetValue(segment)).Select(c => c.ToLowerInvariant())));
            if ((bool)parser.IsOptional.GetValue(segment)!) builder.Append('?');

            // A default changes the value bound when the URL omits the segment, so "{page?}" and
            // "{page=1}" match the same URLs without being interchangeable.
            if (parser.DefaultValue.GetValue(segment) is string defaultValue) builder.Append('=').Append(defaultValue);

            builder.Append('}');
        }
        else
        {
            // Plain literals, and the single-segment wildcard "*" - the parser never produces a
            // literal "*", so it cannot collide with one.
            AppendShapeLiteral(builder, (string)parser.Value.GetValue(segment)!);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Literal text goes into the shape with its braces doubled, exactly as the router does, so a
    /// literal written with escaped braces ("{{x}}" -> "{x}") can never read as a parameter.
    /// </summary>
    private static void AppendShapeLiteral(StringBuilder builder, string literal)
    {
        // Case folds because the router matches literals case-insensitively unless BrouterOptions
        // .CaseSensitive is turned on, which this demo - like the default - leaves off.
        var text = literal.ToLowerInvariant();

        builder.Append(text.Contains('{') || text.Contains('}')
            ? text.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal)
            : text);
    }

    private static BrouterTemplateSegmentDto Describe(Reflected parser, object segment)
    {
        var parts = parser.Parts.GetValue(segment);
        var isParameter = (bool)parser.IsParameter.GetValue(segment)!;

        var kind = (bool)parser.IsCatchAll.GetValue(segment)! ? "CatchAll"
                 : (bool)parser.IsSingleWildcard.GetValue(segment)! ? "Wildcard"
                 : parts is not null ? "Complex"
                 : isParameter ? "Parameter"
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
            // written, it stays recognizable in the notes below. A literal segment - including the
            // nameless catch-all "**" and the wildcard "*" - already IS its own text.
            Value = isParameter
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
                yield return $"'{segment.Value}' is a catch-all: it matches the whole remainder of the URL (slashes included) and must be the last segment. " +
                             "'{*name}', '{**name}' and the nameless '**' all match the same URLs; only the named forms bind the remainder to a parameter.";
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

            // The signature is checked, not just the name: Inspect invokes this with exactly
            // (template, constraints), so a method that grew a parameter - or an overload set this
            // no longer picks the right member out of - has to read as unavailable here rather than
            // throw on every single call.
            var parse = parserType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                  .FirstOrDefault(method => method.Name == "ParseTemplate" && Accepts(method));
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
            var partValue = partType.GetProperty("Value", flags);
            var partIsParameter = partType.GetProperty("IsParameter", flags);
            var partIsOptional = partType.GetProperty("IsOptional", flags);
            var partConstraints = partType.GetProperty("Constraints", flags);
            var constraintName = bindingType.GetProperty("Name", flags);

            if (template is null || segments is null || value is null || isParameter is null || isCatchAll is null ||
                isSingleWildcard is null || isOptional is null || defaultValue is null || constraints is null ||
                parts is null || parameterNames is null || specificity is null || partValue is null ||
                partIsParameter is null || partIsOptional is null || partConstraints is null ||
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
                PartValue = partValue,
                PartIsParameter = partIsParameter,
                PartIsOptional = partIsOptional,
                PartConstraints = partConstraints,
                ConstraintName = constraintName
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Whether a method takes the two arguments <see cref="Inspect"/> passes it.</summary>
    private static bool Accepts(MethodInfo method)
    {
        var parameters = method.GetParameters();

        return parameters.Length == 2 &&
               parameters[0].ParameterType == typeof(string) &&
               parameters[1].ParameterType.IsAssignableFrom(typeof(BrouterConstraintRegistry));
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
        public required PropertyInfo PartValue { get; init; }
        public required PropertyInfo PartIsParameter { get; init; }
        public required PropertyInfo PartIsOptional { get; init; }
        public required PropertyInfo PartConstraints { get; init; }
        public required PropertyInfo ConstraintName { get; init; }
    }
}
