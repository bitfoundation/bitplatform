using System.Text;

namespace Bit.Cli.Templates;

public static class ProjectName
{
    public const int MaxLength = 50;

    private static readonly HashSet<string> keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    ];

    private static readonly HashSet<string> reservedRoots = new(StringComparer.OrdinalIgnoreCase) { "System", "Microsoft" };

    public static string? Validate(string? name, IReadOnlySet<string>? typeNames = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Enter a name, e.g. Contoso.Shop.";

        if (name.Length > MaxLength)
            return $"Keep it under {MaxLength} characters; it ends up in every path and namespace of the project.";

        var segments = name.Split('.');

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
                return "A name can't start or end with a dot, or have two dots in a row.";

            if (char.IsLetter(segment[0]) is false && segment[0] is not '_')
                return $"'{segment}' must start with a letter, because each part of the name becomes a C# namespace.";

            if (segment.All(c => char.IsLetterOrDigit(c) || c is '_') is false)
                return "Use only letters, digits, underscores and dots. Dashes and spaces break the generated code.";

            if (keywords.Contains(segment))
                return $"'{segment}' is a C# keyword and can't be part of a namespace.";

            if (typeNames?.Contains(segment) is true)
                return $"'{segment}' is also the name of a type in the project's code, and the namespace '{segment}' would hide that type, so the project wouldn't build.";
        }

        if (reservedRoots.Contains(segments[0]))
            return $"A name starting with '{segments[0]}' clashes with .NET's own namespaces.";

        return null;
    }

    public static string? ValidateAppId(string name)
    {
        var appId = "com.companyname." + string.Concat(name.ToLowerInvariant().Where(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '.'));

        return IsValidAppId(appId)
            ? null
            : $"'{name}' would give the native apps the app id {appId}, and each part of an app id must start with a Latin letter. Use Latin letters in the name, or leave the native apps out of --platforms.";
    }

    public static bool IsValidAppId(string appId)
    {
        return appId.Split('.') is { Length: > 1 } parts && parts.All(part => part.Length > 0 && char.IsAsciiLetter(part[0]) && part.All(c => char.IsAsciiLetterOrDigit(c) || c is '_'));
    }

    public static string Suggest(string? input, IReadOnlySet<string>? typeNames = null)
    {
        var segments = new List<string>();

        foreach (var rawSegment in (input ?? "").Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var builder = new StringBuilder();
            var upperNext = true;

            foreach (var c in rawSegment)
            {
                if (char.IsLetterOrDigit(c) || c is '_')
                {
                    builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                    upperNext = false;
                }
                else
                {
                    upperNext = true;
                }
            }

            if (builder.Length == 0)
                continue;

            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, "App");
            }

            var segment = builder.ToString();
            segments.Add(keywords.Contains(segment) || typeNames?.Contains(segment) is true ? "My" + char.ToUpperInvariant(segment[0]) + segment[1..] : segment);
        }

        if (segments.Count == 0)
            return "MyApp";

        if (reservedRoots.Contains(segments[0]))
        {
            segments[0] = "My" + segments[0];
        }

        var suggestion = string.Join('.', segments);
        return suggestion.Length > MaxLength ? suggestion[..MaxLength].TrimEnd('.') : suggestion;
    }
}
