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

    /// <summary>
    /// The members of a type, under the name a caller reaches it by: <paramref name="path"/> is the
    /// dotted path for a nested type - <c>BitCss.Var.Color</c> - because the CLR simple name of one
    /// branch of a token tree (<c>Color</c>) is not a name anything else on this server resolves.
    /// </summary>
    public static void AppendMembers(StringBuilder builder, Type type, string? path = null)
    {
        if (type.IsEnum)
        {
            AppendEnum(builder, type);
            AppendExtensions(builder, type);
            return;
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            AppendDelegate(builder, type);
            return;
        }

        AppendConstants(builder, type);
        AppendNested(builder, type, path);
        AppendProperties(builder, type);
        AppendInherited(builder, type);
        AppendMethods(builder, type);
        AppendEvents(builder, type);
        AppendExtensions(builder, type);
    }

    /// <summary>
    /// The members a type takes from a base class of this library, named rather than tabulated
    /// again under it.
    /// <para>
    /// The tables above are read with <c>DeclaredOnly</c>, which is right for the hundred types
    /// whose base is <c>object</c> and wrong for the ones that have a real base: a
    /// <c>BitCalendarParams</c> answered with its own hundred properties alone says its
    /// <c>Class</c>, <c>Style</c> and <c>IsEnabled</c> do not exist, while the component's own
    /// answer counts them. So each library base is named with the members it brings and the call
    /// that documents them - the set once, not once per type that closes it.
    /// </para>
    /// </summary>
    private static void AppendInherited(StringBuilder builder, Type type)
    {
        for (var current = type.BaseType; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (BlazorUIAssemblies.All.Contains(current.Assembly) is false) continue;

            var names = current.GetProperties(Public)
                               .Where(p => p.GetIndexParameters().Length == 0)
                               .Select(p => p.Name)
                               .ToArray();

            if (names.Length == 0) continue;

            builder.AppendLine($"Inherited from `{current.Name}`: {string.Join(", ", names.Select(n => $"`{n}`"))}. `GetBitBlazorUIType(typeName: \"{current.Name}\")` documents them.").AppendLine();
        }
    }

    /// <summary>
    /// What another package adds to this type. Answered here, under the type the members are
    /// written on, because that is where they are read: <c>BitThemePresets.MaterialDark</c> is
    /// reached exactly like <c>BitThemePresets.FluentDark</c>, and an answer that listed only the
    /// members the type's own assembly declares would say the first of those does not exist.
    /// <para>
    /// The prose is the difference between the two forms, which is the part that bites: a
    /// contributed name is a static property rather than a <c>const</c>, and reading one needs
    /// C# 14 on the reading side as well. What to write where that will not do is the container's
    /// own documentation, which is one call away rather than repeated per member.
    /// </para>
    /// </summary>
    private static void AppendExtensions(StringBuilder builder, Type type)
    {
        foreach (var group in BlazorUIExtensionMembers.For(type))
        {
            builder.AppendLine($"## Added by {group.Package.PackageId}").AppendLine();

            builder.Append($"Extension members on `{group.ReceiverName}`, so they are read exactly like the members above - ")
                   .Append($"`{group.ReceiverName}.{group.Members[0].Name}` - with nothing but `@using Bit.BlazorUI` in scope, once the app references `{group.Package.PackageId}`. ")
                   .Append("Each is a static property rather than a `const`, so it cannot be a `case` label, an attribute argument or a default parameter value, and a project compiling at an older C# version cannot read one at all. ")
                   .AppendLine($"`GetBitBlazorUIType(typeName: \"{group.Container.Name}\")` has what to write in either case.")
                   .AppendLine();

            AppendExtensionMembers(builder, group.Members);
        }
    }

    /// <summary>The members of one extension block as a table - the same rows on either side of it.</summary>
    public static void AppendExtensionMembers(StringBuilder builder, IReadOnlyList<BlazorUIExtensionMember> members)
    {
        var values = members.Any(m => string.IsNullOrWhiteSpace(m.Value) is false);

        builder.AppendLine(values ? "| Name | Type | Value | Description |" : "| Name | Type | Description |");
        builder.AppendLine(values ? "| --- | --- | --- | --- |" : "| --- | --- | --- |");

        foreach (var member in members)
        {
            builder.Append($"| `{member.Name}` | `{member.Type}` | ");
            if (values) builder.Append($"{Code(member.Value)} | ");
            builder.AppendLine($"{Cell(member.Summary)} |");
        }

        builder.AppendLine();
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
    }

    /// <summary>
    /// The nested static classes a catalog is organised into - <c>BitCss.Var.Color.Primary</c> and
    /// its siblings - are types of their own and reached by name, so they are named rather than
    /// inlined, which would flatten a tree of five hundred values into one table.
    /// <para>
    /// Written outside <see cref="AppendConstants"/> because the branches of such a tree hold no
    /// constants of their own: <c>BitCss</c> is nothing but nested classes, and listing them only
    /// when it also had a constants table left the whole token tree undiscoverable.
    /// </para>
    /// </summary>
    private static void AppendNested(StringBuilder builder, Type type, string? path)
    {
        var nested = type.GetNestedTypes(BindingFlags.Public).Where(t => t.IsAbstract && t.IsSealed).ToArray();

        if (nested.Length == 0) return;

        // The path a caller types, not the CLR simple name: `GetBitBlazorUIType` walks the full
        // dotted name, and "Color.Primary" would send it to the BitColor enum instead.
        var prefix = string.IsNullOrWhiteSpace(path) ? type.Name : path.Trim();

        builder.AppendLine($"Nested: {string.Join(", ", nested.Select(t => $"`{prefix}.{t.Name}`"))}.").AppendLine();
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
