using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// Writes the members of a type as Markdown, read off the loaded assembly and annotated with the
/// XML documentation that ships beside it. Everything a caller needs to write the call correctly -
/// the exact member name, its type, whether it is a Blazor parameter - comes from the build rather
/// than from a description of it, so it cannot disagree with the package the app references.
/// </summary>
public static class BlazorUIReflection
{
    private const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>How many constants one answer prints before it stops being a table and starts being a dump.</summary>
    private const int ConstantsCap = 80;

    public static void AppendMembers(StringBuilder builder, Type type)
    {
        if (type.IsEnum)
        {
            AppendEnum(builder, type);
            return;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            AppendDelegate(builder, type);
            return;
        }

        AppendConstants(builder, type);
        AppendProperties(builder, type);
        AppendMethods(builder, type);
        AppendEvents(builder, type);
    }

    private static void AppendEnum(StringBuilder builder, Type type)
    {
        builder.AppendLine("| Name | Value | Description |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var name in Enum.GetNames(type))
        {
            var value = Convert.ToInt64(Enum.Parse(type, name), System.Globalization.CultureInfo.InvariantCulture);
            var summary = BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(type, name, isField: true));

            builder.AppendLine($"| `{name}` | {value} | {Cell(summary)} |");
        }

        builder.AppendLine();
    }

    private static void AppendDelegate(StringBuilder builder, Type type)
    {
        var invoke = type.GetMethod("Invoke");

        if (invoke is null) return;

        builder.AppendLine($"`{BlazorUITypeNames.Of(invoke.ReturnType)} {type.Name}({Parameters(invoke)})`").AppendLine();
    }

    /// <summary>
    /// The constants and static readonly values of a type. This is what the string catalogs are -
    /// <c>BitThemePresets</c>, <c>BitCss.Class</c>, <c>BitIconName</c> - and the value is the whole
    /// point of them, so it is a column rather than something a caller has to infer from the name.
    /// </summary>
    private static void AppendConstants(StringBuilder builder, Type type)
    {
        var fields = type.GetFields(Public)
                         .Where(f => f.IsLiteral || f.IsInitOnly)
                         .Where(f => f.IsStatic)
                         .ToArray();

        if (fields.Length == 0) return;

        builder.AppendLine("## Constants").AppendLine();

        // A catalog of a few dozen names is a table; a catalog of two thousand is a search problem,
        // and BitIconName is the one type here that is the second thing. Handing over all of it
        // would spend a client's whole context window on names it will use one of.
        if (fields.Length > ConstantsCap)
        {
            builder.AppendLine($"{fields.Length:N0} of them - too many to list. {(type == typeof(BitIconName) ? "`FindBitBlazorUIIcons` searches them by what the glyph shows" : $"They follow one naming scheme; `SearchBitBlazorUI` finds the one you want by name")}. The first {ConstantsCap} in declaration order:").AppendLine();

            fields = [.. fields.Take(ConstantsCap)];
        }

        builder.AppendLine("| Name | Type | Value | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (var field in fields)
        {
            var value = field.IsLiteral ? field.GetRawConstantValue() : Safe(field);

            builder.AppendLine($"| `{field.Name}` | `{BlazorUITypeNames.Of(field.FieldType)}` | {Code(value?.ToString())} | {Cell(BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(type, field.Name, isField: true)))} |");
        }

        builder.AppendLine();

        // The nested static classes a catalog is organised into - BitCss.Var.Color.Primary and its
        // siblings - are types of their own and reached by name, so they are named rather than
        // inlined, which would flatten a tree of five hundred values into one table.
        var nested = type.GetNestedTypes(BindingFlags.Public).Where(t => t.IsAbstract && t.IsSealed).ToArray();

        if (nested.Length > 0)
        {
            builder.AppendLine($"Nested: {string.Join(", ", nested.Select(t => $"`{type.Name}.{t.Name}`"))}.").AppendLine();
        }
    }

    private static void AppendProperties(StringBuilder builder, Type type)
    {
        var properties = type.GetProperties(Public).Where(p => p.GetIndexParameters().Length == 0).ToArray();

        if (properties.Length == 0) return;

        var parameters = properties.Any(p => p.IsDefined(typeof(ParameterAttribute)));

        builder.AppendLine("## Properties").AppendLine();
        builder.AppendLine(parameters ? "| Name | Type | Blazor parameter | Description |" : "| Name | Type | Description |");
        builder.AppendLine(parameters ? "| --- | --- | --- | --- |" : "| --- | --- | --- |");

        foreach (var property in properties)
        {
            builder.Append($"| `{property.Name}` | `{BlazorUITypeNames.Of(property.PropertyType)}` | ");
            if (parameters) builder.Append(property.IsDefined(typeof(ParameterAttribute)) ? "yes | " : " | ");
            builder.AppendLine($"{Cell(BlazorUIXmlDocs.GetPropertySummary(type, property))} |");
        }

        builder.AppendLine();
    }

    private static void AppendMethods(StringBuilder builder, Type type)
    {
        var methods = type.GetMethods(Public)
                          .Where(m => m.IsSpecialName is false && m.DeclaringType != typeof(object))
                          .ToArray();

        if (methods.Length == 0) return;

        builder.AppendLine("## Methods").AppendLine();
        builder.AppendLine("| Signature | Description |");
        builder.AppendLine("| --- | --- |");

        foreach (var method in methods)
        {
            var generics = method.IsGenericMethodDefinition ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>" : null;
            var signature = $"{BlazorUITypeNames.Of(method.ReturnType)} {method.Name}{generics}({Parameters(method)})";

            builder.AppendLine($"| `{signature}` | {Cell(BlazorUIXmlDocs.GetSummary($"M:{type.FullName}.{method.Name}"))} |");
        }

        builder.AppendLine();
    }

    private static void AppendEvents(StringBuilder builder, Type type)
    {
        var events = type.GetEvents(Public);

        if (events.Length == 0) return;

        builder.AppendLine("## Events").AppendLine();
        builder.AppendLine("| Name | Type | Description |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var @event in events)
        {
            builder.AppendLine($"| `{@event.Name}` | `{BlazorUITypeNames.Of(@event.EventHandlerType ?? typeof(object))}` | {Cell(BlazorUIXmlDocs.GetSummary($"E:{type.FullName}.{@event.Name}"))} |");
        }

        builder.AppendLine();
    }

    private static string Parameters(MethodInfo method)
    {
        return string.Join(", ", method.GetParameters().Select(p =>
        {
            var optional = p.HasDefaultValue ? $" = {p.DefaultValue ?? "null"}" : null;

            return $"{BlazorUITypeNames.Of(p.ParameterType)} {p.Name}{optional}";
        }));
    }

    /// <summary>
    /// The value of a static readonly field, when reading it does not run anything. A field whose
    /// initializer needs a live app must not take a documentation request down with it.
    /// </summary>
    private static object? Safe(FieldInfo field)
    {
        try
        {
            return field.GetValue(null);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Code(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : $"`{Cell(text)}`";

    private static string Cell(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text.Replace('\r', ' ')
                   .Replace('\n', ' ')
                   .Replace("|", @"\|", StringComparison.Ordinal)
                   .Trim();
    }
}
