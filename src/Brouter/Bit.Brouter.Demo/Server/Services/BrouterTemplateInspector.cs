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
            return new BrouterTemplateInspectionDto { Template = template, IsValid = false, Error = "The template could not be parsed." };
        }

        var segments = ((IEnumerable)parser.Segments.GetValue(parsed)!).Cast<object>().ToArray();
        var dtos = new BrouterTemplateSegmentDto[segments.Length];

        for (int i = 0; i < segments.Length; i++)
        {
            dtos[i] = Describe(parser, segments[i]);
        }

        return new BrouterTemplateInspectionDto
        {
            Template = template,
            IsValid = true,
            NormalizedTemplate = (string?)parser.Template.GetValue(parsed),
            Specificity = dtos.Sum(s => s.Specificity),
            ParameterNames = [.. dtos.SelectMany(s => s.ParameterNames ?? [])],
            Segments = dtos,
            Notes = [.. Notes(dtos)]
        };
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

            return new Reflected
            {
                ParseTemplate = parse,
                Template = templateType.GetProperty("Template", flags)!,
                Segments = templateType.GetProperty("TemplateSegments", flags)!,
                Value = segmentType.GetProperty("Value", flags)!,
                IsParameter = segmentType.GetProperty("IsParameter", flags)!,
                IsCatchAll = segmentType.GetProperty("IsCatchAll", flags)!,
                IsSingleWildcard = segmentType.GetProperty("IsSingleWildcard", flags)!,
                IsOptional = segmentType.GetProperty("IsOptional", flags)!,
                DefaultValue = segmentType.GetProperty("DefaultValue", flags)!,
                Constraints = segmentType.GetProperty("Constraints", flags)!,
                Parts = segmentType.GetProperty("Parts", flags)!,
                ParameterNames = segmentType.GetProperty("ParameterNames", flags)!,
                Specificity = segmentType.GetProperty("Specificity", flags)!,
                PartConstraints = partType.GetProperty("Constraints", flags)!,
                ConstraintName = bindingType.GetProperty("Name", flags)!
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
