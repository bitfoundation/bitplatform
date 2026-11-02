using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Reads the plain values a tag writes for a parameter, like Color="BitColor.Error" or IconOnly, and writes values back as
/// attributes. Anything else, like @Localizer[...], is code the component panel shows but leaves alone.
/// </summary>
public static partial class WorkbenchRazorValues
{
    /// <summary>The value an attribute sets, as the panel shows it; false when code computes it.</summary>
    public static bool TryRead(Type type, string? raw, out string? value)
    {
        var core = Nullable.GetUnderlyingType(type) ?? type;
        value = null;

        if (IsNull(type, raw))
            return true;

        if (core == typeof(bool))
        {
            value = raw is null ? "true" : Unwrap(raw);
            return value is "true" or "false";
        }

        if (raw is null)
            return false;

        if (core == typeof(string))
        {
            // The way pages name an icon: @BitIconName.Delete.
            if (IconReference().Match(raw.Trim()) is { Success: true } icon && WorkbenchIconSearch.NameOf(icon.Groups["field"].Value) is { } iconName)
            {
                value = iconName;
                return true;
            }

            if (CSharpString().Match(raw.Trim()) is { Success: true } literal)
            {
                value = Regex.Unescape(literal.Groups["value"].Value);
                return true;
            }

            // A lone @ starts code; @@ is an escaped @.
            if (raw.Replace("@@", string.Empty).Contains('@'))
                return false;

            value = raw.Replace("@@", "@");
            return true;
        }

        var text = Unwrap(raw);

        if (core.IsEnum)
        {
            var match = EnumMember().Match(text);
            value = match.Success && match.Groups["type"].Value == core.Name && Enum.GetNames(core).Contains(match.Groups["member"].Value) ? match.Groups["member"].Value : null;
            return value is not null;
        }

        if (IsNumber(core))
        {
            var number = text.TrimEnd('d', 'D', 'f', 'F', 'm', 'M', 'l', 'L', 'u', 'U');
            value = decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? number : null;
            return value is not null;
        }

        return false;
    }

    /// <summary>Whether an attribute sets a parameter that can be null to null, like Color="null" or Title="@null".</summary>
    public static bool IsNull(Type type, string? raw)
    {
        if (raw is null || (type.IsValueType && Nullable.GetUnderlyingType(type) is null))
            return false;

        // In a text parameter a plain null is the word; only code makes it null.
        return type == typeof(string) ? raw.Trim() is "@null" or "@(null)" : Unwrap(raw) is "null";
    }

    /// <summary>
    /// The attribute that sets a parameter to its default, which a removal writes first, as hot reload leaves a parameter
    /// a tag stops setting at its last value. Null when it can't be written, like for a parameter typed by the component's
    /// type argument, whose type the page may infer from that attribute.
    /// </summary>
    public static string? DefaultAttribute(PropertyInfo property, string? defaultValue)
    {
        var type = property.PropertyType;

        if (type.ContainsGenericParameters)
            return null;

        if (defaultValue is not null)
            return Attribute(property, defaultValue);

        if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            return null;

        return type == typeof(string) ? $"{property.Name}=\"@null\"" : $"{property.Name}=\"null\"";
    }

    /// <summary>The parts an attribute sets for a Styles or Classes parameter, written as @(new() { Root = "..." }); null when code computes them.</summary>
    public static Dictionary<string, string>? TryReadParts(string? raw)
    {
        if (raw is null || NewObject().Match(raw.Trim()) is not { Success: true } match)
            return null;

        var body = match.Groups["body"].Value;
        Dictionary<string, string> parts = [];
        var index = 0;

        foreach (Match item in PartAssignment().Matches(body))
        {
            if (item.Index != index)
                return null;

            parts[item.Groups["name"].Value] = Regex.Unescape(item.Groups["value"].Value);
            index = item.Index + item.Length;
        }

        return body[index..].Trim().Length is 0 ? parts : null;
    }

    /// <summary>The attribute that sets a parameter to a value; null when the value doesn't fit the parameter's type.</summary>
    public static string? Attribute(PropertyInfo property, string value)
    {
        var core = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var name = property.Name;

        if (core == typeof(bool))
            return value switch { "true" => name, "false" => $"{name}=\"false\"", _ => null };

        if (core.IsEnum)
            return Enum.GetNames(core).Contains(value) ? $"{name}=\"{EnumReference(core)}.{value}\"" : null;

        if (IsNumber(core))
        {
            try
            {
                Convert.ChangeType(value, core, CultureInfo.InvariantCulture);
                return $"{name}=\"{value}\"";
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or InvalidCastException)
            {
                return null;
            }
        }

        if (core == typeof(string) && IsIcon(property) && WorkbenchIconSearch.FieldOf(value) is { } field)
            return $"{name}=\"@BitIconName.{field}\"";

        if (core == typeof(string))
            return value.IndexOfAny(['"', '@', '&']) >= 0 ? $"{name}=\"@({CSharp(value)})\"" : $"{name}=\"{value}\"";

        return null;
    }

    /// <summary>The attribute that sets the parts of a Styles or Classes parameter; null when every part is empty.</summary>
    public static string? PartsAttribute(PropertyInfo property, IEnumerable<WorkbenchComponentPart> parts)
    {
        var assignments = parts.Where(part => string.IsNullOrWhiteSpace(part.Value) is false && PartsOf(property.PropertyType).Any(p => p.Name == part.Name))
                               .Select(part => $"{part.Name} = {CSharp(part.Value!)}")
                               .ToArray();

        return assignments.Length is 0 ? null : $"{property.Name}=\"@(new() {{ {string.Join(", ", assignments)} }})\"";
    }

    /// <summary>Whether a parameter names an icon, like IconName or ClearButtonIconName.</summary>
    public static bool IsIcon(PropertyInfo property) => property.PropertyType == typeof(string) && property.Name.EndsWith("IconName", StringComparison.Ordinal);

    public static bool IsNumber(Type type) => Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
                                                                       or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal && type.IsEnum is false;

    /// <summary>Whether a type is a component's Styles or Classes type, like BitButtonClassStyles: a class of string parts.</summary>
    public static bool IsParts(Type type) => type.IsClass && type.Name.EndsWith("ClassStyles", StringComparison.Ordinal) && PartsOf(type).Length > 0;

    public static PropertyInfo[] PartsOf(Type type) => [.. type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.PropertyType == typeof(string) && p.CanWrite).OrderBy(p => p.MetadataToken)];

    /// <summary>An enum as the code names it: by its name for bit BlazorUI's, whose namespace every page imports, otherwise in full.</summary>
    private static string EnumReference(Type type) => type.Namespace is "Bit.BlazorUI" && type.DeclaringType is null ? type.Name : $"global::{type.FullName!.Replace('+', '.')}";

    private static string CSharp(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")}\"";

    /// <summary>Strips the @ and the parentheses around a plain C# value, as in @BitColor.Error or @(true).</summary>
    private static string Unwrap(string raw)
    {
        var text = raw.Trim();

        if (text.StartsWith('@'))
        {
            text = text[1..].Trim();
        }

        if (text.StartsWith('(') && text.EndsWith(')'))
        {
            text = text[1..^1].Trim();
        }

        return text;
    }

    [GeneratedRegex("""^@\(\s*"(?<value>(?:[^"\\]|\\.)*)"\s*\)$""")]
    private static partial Regex CSharpString();

    [GeneratedRegex(@"^@(?:global::)?(?:Bit\.BlazorUI\.)?BitIconName\.(?<field>\w+)$")]
    private static partial Regex IconReference();

    [GeneratedRegex(@"^(?:global::)?(?:\w+\.)*(?<type>\w+)\.(?<member>\w+)$")]
    private static partial Regex EnumMember();

    [GeneratedRegex(@"^@?\(?\s*new\s*(?:[\w.:]+)?\s*(?:\(\s*\))?\s*\{(?<body>.*)\}\s*\)?$", RegexOptions.Singleline)]
    private static partial Regex NewObject();

    [GeneratedRegex("""\s*(?<name>\w+)\s*=\s*"(?<value>(?:[^"\\]|\\.)*)"\s*,?""")]
    private static partial Regex PartAssignment();
}
