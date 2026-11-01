namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Reads a live component's parameters, its own and the ones it inherits, and how its tag in the code sets each one.</summary>
public static class WorkbenchComponentParameters
{
    private static readonly Lazy<Dictionary<string, Type>> componentTypes = new(() =>
    {
        Assembly[] assemblies = [typeof(BitButton).Assembly, typeof(BitChart).Assembly, typeof(BitAiChat).Assembly];

        return assemblies.Distinct()
                         .SelectMany(assembly => assembly.GetExportedTypes())
                         .Where(type => type.IsClass && type.IsAbstract is false && typeof(BitComponentBase).IsAssignableFrom(type))
                         .GroupBy(WorkbenchComponentSources.TagName)
                         .ToDictionary(group => group.Key, group => group.First());
    });

    /// <summary>A bit BlazorUI component type by the name a tag writes; a generic one as its definition.</summary>
    public static Type? TypeOf(string name) => componentTypes.Value.GetValueOrDefault(name);

    /// <summary>A component type's parameter by name, in any case, as Razor matches them.</summary>
    public static PropertyInfo? ParameterOf(Type type, string name) => ParametersOf(type).FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The component's [Parameter] properties: its own first, then the inherited ones, each group by name.</summary>
    public static IEnumerable<PropertyInfo> ParametersOf(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                   .Where(p => p.GetCustomAttribute<ParameterAttribute>() is { CaptureUnmatchedValues: false } && p.GetIndexParameters().Length is 0 && p.CanWrite)
                   .OrderBy(p => p.DeclaringType == type ? 0 : 1)
                   .ThenBy(p => p.Name, StringComparer.Ordinal);
    }

    public static WorkbenchComponentInfo Describe(BitComponentBase component, string elementId)
    {
        var type = component.GetType();
        var name = WorkbenchComponentSources.TagName(type);
        var (tag, drawnBy) = WorkbenchComponentSources.Find(component);
        var defaults = Create(type);
        var parameters = Read(component, defaults, tag is null ? [] : AttributesOf(tag, name), out var contradicts);

        // A tag whose plain enum values differ from the component's is another tag; changes made through it would land elsewhere.
        if (contradicts)
        {
            tag = null;
            parameters = Read(component, defaults, [], out _);
        }

        return new() { Name = name, ElementId = elementId, Tag = tag, DrawnBy = drawnBy, Parameters = parameters };
    }

    /// <summary>The attributes of a tag as the file has them now; empty when the tag is no longer there.</summary>
    public static List<WorkbenchRazorAttribute> AttributesOf(WorkbenchComponentTag tag, string name)
    {
        if (WorkbenchRazorFiles.FullPath(tag.File) is not { } path)
            return [];

        var text = File.ReadAllText(path);
        var starts = WorkbenchRazorTags.Find(text, name);

        return tag.Ordinal < starts.Count && WorkbenchRazorTags.Parse(text, starts[tag.Ordinal], name) is { } parsed ? parsed.Attributes : [];
    }

    /// <summary>The parameter an attribute sets: Value for @bind-Value, none for directives like @key or modifiers like @bind-Value:after.</summary>
    public static string? ParameterNameOf(WorkbenchRazorAttribute attribute)
    {
        if (attribute.Name.StartsWith("@bind-", StringComparison.Ordinal))
            return attribute.Name.Contains(':') ? null : attribute.Name["@bind-".Length..];

        return attribute.Name.StartsWith('@') ? null : attribute.Name;
    }

    private static WorkbenchComponentParameter[] Read(BitComponentBase component, object? defaults, List<WorkbenchRazorAttribute> attributes, out bool contradicts)
    {
        var type = component.GetType();
        List<WorkbenchComponentParameter> parameters = [];
        contradicts = false;

        foreach (var property in ParametersOf(type))
        {
            var core = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var editor = EditorFor(core);
            var live = ValueOf(property, component);
            var attribute = attributes.LastOrDefault(a => string.Equals(ParameterNameOf(a), property.Name, StringComparison.OrdinalIgnoreCase));
            var value = editor is WorkbenchParameterEditor.ReadOnly or WorkbenchParameterEditor.Parts ? null : Display(live);
            Dictionary<string, string>? tagParts = null;
            string? code = null;
            var isSet = false;

            if (attribute is not null)
            {
                if (attribute.Name.StartsWith('@'))
                {
                    code = $"{attribute.Name}=\"{attribute.Value}\"";
                }
                else if (editor is WorkbenchParameterEditor.Parts && WorkbenchRazorValues.TryReadParts(attribute.Value) is { } parts)
                {
                    tagParts = parts;
                    isSet = true;
                }
                else if (editor is not (WorkbenchParameterEditor.ReadOnly or WorkbenchParameterEditor.Parts) && WorkbenchRazorValues.TryRead(property.PropertyType, attribute.Value, out var literal))
                {
                    isSet = true;
                    contradicts |= editor is WorkbenchParameterEditor.Choice && literal != value;
                }
                else
                {
                    code = attribute.Value ?? attribute.Name;
                }
            }

            // Templates and callbacks the tag doesn't set say nothing about this component.
            if (editor is WorkbenchParameterEditor.ReadOnly && attribute is null)
                continue;

            parameters.Add(new()
            {
                Name = property.Name,
                TypeName = Friendly(property.PropertyType),
                Editor = editor,
                Value = value,
                DefaultValue = editor is WorkbenchParameterEditor.ReadOnly or WorkbenchParameterEditor.Parts || defaults is null ? null : Display(ValueOf(property, defaults)),
                Choices = core.IsEnum ? Enum.GetNames(core) : [],
                IsNullable = property.PropertyType.IsValueType is false || core != property.PropertyType,
                InheritedFrom = property.DeclaringType == type ? null : WorkbenchComponentSources.TagName(property.DeclaringType!),
                IsSet = isSet,
                Code = code,
                Parts = editor is WorkbenchParameterEditor.Parts
                    ? [.. WorkbenchRazorValues.PartsOf(core).Select(part => new WorkbenchComponentPart(part.Name, tagParts is null ? (live is null ? null : part.GetValue(live) as string) : tagParts.GetValueOrDefault(part.Name)))]
                    : []
            });
        }

        return [.. parameters];
    }

    private static WorkbenchParameterEditor EditorFor(Type core)
    {
        if (core == typeof(bool))
            return WorkbenchParameterEditor.Toggle;

        if (core.IsEnum)
            return core.IsDefined(typeof(FlagsAttribute)) ? WorkbenchParameterEditor.ReadOnly : WorkbenchParameterEditor.Choice;

        if (core == typeof(string))
            return WorkbenchParameterEditor.Text;

        if (WorkbenchRazorValues.IsNumber(core))
            return WorkbenchParameterEditor.Number;

        return WorkbenchRazorValues.IsParts(core) ? WorkbenchParameterEditor.Parts : WorkbenchParameterEditor.ReadOnly;
    }

    private static string? Display(object? value) => value switch
    {
        null => null,
        bool flag => flag ? "true" : "false",
        Enum member => member.ToString(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        string text => text,
        _ => null
    };

    private static string Friendly(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{Friendly(underlying)}?";

        return type.IsGenericType ? $"{WorkbenchComponentSources.TagName(type)}<{string.Join(", ", type.GetGenericArguments().Select(Friendly))}>" : type.Name;
    }

    private static object? ValueOf(PropertyInfo property, object target)
    {
        try
        {
            return property.GetValue(target);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    /// <summary>A fresh instance, whose values are the parameters' defaults; null when the type can't be made that way.</summary>
    private static object? Create(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception exception) when (exception is MissingMethodException or TargetInvocationException or MemberAccessException or NotSupportedException)
        {
            return null;
        }
    }
}
