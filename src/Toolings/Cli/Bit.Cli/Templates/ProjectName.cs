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

    public static string? Validate(string? name)
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
        }

        if (reservedRoots.Contains(segments[0]))
            return $"A name starting with '{segments[0]}' clashes with .NET's own namespaces.";

        return null;
    }

    public static string Suggest(string? input)
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
            segments.Add(keywords.Contains(segment) ? "My" + char.ToUpperInvariant(segment[0]) + segment[1..] : segment);
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
