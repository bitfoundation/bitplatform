using System.Reflection;
using Bit.BlazorUI.Demo.Client.Core.Models;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>The samples one demo page or tab holds, keyed by the field each is declared as.</summary>
/// <param name="Code">The Razor and C# strings - <c>example1RazorCode</c> and its siblings.</param>
/// <param name="Files">The multi-file examples' file lists - <c>example1CodeFiles</c> and its siblings.</param>
public sealed record DemoSamples(
    IReadOnlyDictionary<string, string> Code,
    IReadOnlyDictionary<string, IReadOnlyList<DemoCodeFile>> Files)
{
    /// <summary>The answer for a page that could not be constructed, and for an example with no owner.</summary>
    public static readonly DemoSamples Empty = new(
        new Dictionary<string, string>(StringComparer.Ordinal),
        new Dictionary<string, IReadOnlyList<DemoCodeFile>>(StringComparer.Ordinal));
}

/// <summary>The API tables a demo page renders, read off the page type rather than out of its markup.</summary>
/// <param name="CssVariables">
/// The public CSS custom properties the component reads off its root - <c>componentCssVariables</c> -
/// carried as members whose type is empty, since a custom property has a default and a meaning but no type.
/// </param>
public sealed record DemoTables(
    IReadOnlyList<ComponentMember> Parameters,
    IReadOnlyList<ComponentMember> PublicMembers,
    IReadOnlyList<ComponentSubType> SubClasses,
    IReadOnlyList<ComponentSubType> SubEnums,
    IReadOnlyList<ComponentMember> CssVariables)
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// The tables of one demo page.
    /// <para>
    /// Every parameter, its type, its default and its prose is already written out on the page - by
    /// hand, reviewed, and kept in step with the component because the site renders it. Reflecting
    /// the fields that hold it is what lets an MCP client read the same table a person reads,
    /// rather than a second description of the same component that drifts from it.
    /// </para>
    /// <para>
    /// The page is instantiated to read them, since they are instance fields. That runs the page's
    /// field initializers and nothing else - no lifecycle method, no injected service - and a page
    /// whose initializers need more than that is skipped rather than allowed to fail the request.
    /// </para>
    /// </summary>
    public static DemoTables? Read(Type demoPageType, string prefix = "component")
    {
        var page = Construct(demoPageType);

        if (page is null) return null;

        return new DemoTables(
            Members(page, demoPageType, $"{prefix}Parameters"),
            Members(page, demoPageType, $"{prefix}PublicMembers"),
            ReadSubClasses(page, demoPageType, $"{prefix}SubClasses"),
            ReadSubEnums(page, demoPageType, $"{prefix}SubEnums"),
            ReadCssVariables(page, demoPageType, $"{prefix}CssVariables"));
    }

    /// <summary>
    /// An instance of a demo page, made only so its field initializers have run.
    /// <para>
    /// Most pages have the parameterless constructor a Razor component is compiled with, but the
    /// ones that inject a service into the page class itself are given a constructor that takes it -
    /// the AutoInject generator writes one whose whole body is a run of field assignments. Handing
    /// that constructor nulls is therefore safe and is the only way to reach the field initializers,
    /// which is all any of this is after: nothing here calls a method on the instance, and it is
    /// discarded as soon as the tables have been read off it.
    /// </para>
    /// </summary>
    private static object? Construct(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                        .OrderBy(c => c.GetParameters().Length))
        {
            try
            {
                return constructor.Invoke([.. constructor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)]);
            }
            catch (Exception)
            {
                // A page that cannot be constructed outside a renderer is documented from its
                // component type instead - see BlazorUIComponentCatalog.ReflectParameters.
            }
        }

        return null;
    }

    /// <summary>
    /// The sample fields of one demo page or tab - <c>example1RazorCode</c> and its siblings - read
    /// in a single pass, because they are instance fields and each pass costs an instance.
    /// <para>
    /// Two kinds of field, because an example shows two kinds of sample: the Razor and C# strings
    /// nearly every one of them is written with, and - for the few whose source is spread over
    /// more than one file - the list of files behind its <c>CodeFiles</c>. Both are read in the
    /// same pass over the same instance.
    /// </para>
    /// </summary>
    public static DemoSamples Samples(Type owner)
    {
        var page = Construct(owner);

        if (page is null) return DemoSamples.Empty;

        var code = new Dictionary<string, string>(StringComparer.Ordinal);
        var files = new Dictionary<string, IReadOnlyList<DemoCodeFile>>(StringComparer.Ordinal);

        foreach (var field in owner.GetFields(Fields))
        {
            var value = field.GetValue(page);

            if (field.FieldType == typeof(string) && value is string sample) code[field.Name] = sample;
            else if (value is IEnumerable<DemoCodeFile> sources) files[field.Name] = [.. sources.Where(f => f is not null)];
        }

        return new DemoSamples(code, files);
    }

    private static ComponentMember[] Members(object page, Type type, string fieldName)
    {
        if (Value<List<ComponentParameter>>(page, type, fieldName) is not { } parameters) return [];

        return [.. parameters.Select(p => new ComponentMember(p.Name ?? string.Empty, p.Type ?? string.Empty, p.DefaultValue, p.Description))];
    }

    private static ComponentMember[] ReadCssVariables(object page, Type type, string fieldName)
    {
        if (Value<List<ComponentCssVariable>>(page, type, fieldName) is not { } variables) return [];

        return [.. variables.Select(v => new ComponentMember(v.Name ?? string.Empty, string.Empty, v.DefaultValue, v.Description))];
    }

    private static ComponentSubType[] ReadSubClasses(object page, Type type, string fieldName)
    {
        if (Value<List<ComponentSubClass>>(page, type, fieldName) is not { } classes) return [];

        return [.. classes.Select(c => new ComponentSubType(
            c.Title ?? string.Empty,
            c.Description,
            [.. c.Parameters.Select(p => new ComponentMember(p.Name ?? string.Empty, p.Type ?? string.Empty, p.DefaultValue, p.Description))],
            IsEnum: false))];
    }

    private static ComponentSubType[] ReadSubEnums(object page, Type type, string fieldName)
    {
        if (Value<List<ComponentSubEnum>>(page, type, fieldName) is not { } enums) return [];

        return [.. enums.Select(e => new ComponentSubType(
            e.Name ?? string.Empty,
            e.Description,
            [.. e.Items.Select(i => new ComponentMember(i.Name ?? string.Empty, string.Empty, i.Value, i.Description))],
            IsEnum: true))];
    }

    private static T? Value<T>(object page, Type type, string fieldName) where T : class
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetField(fieldName, Fields) is { } field) return field.GetValue(page) as T;
        }

        return null;
    }
}
