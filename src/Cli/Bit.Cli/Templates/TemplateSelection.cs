namespace Bit.Cli.Templates;

public sealed class TemplateSelection(TemplateManifest manifest)
{
    private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public TemplateManifest Manifest { get; } = manifest;

    public string this[string name] => values.TryGetValue(name, out var value) ? value : Manifest.Find(name)?.DefaultValue ?? "";

    public bool IsTrue(string name) => string.Equals(this[name], "true", StringComparison.OrdinalIgnoreCase);

    public bool Aspire => IsTrue("aspire");

    public string Database => this["database"];

    public IReadOnlyList<string> AspireContainers()
    {
        if (Aspire is false)
            return [];

        var containers = new List<string> { "Keycloak", "Mailpit" };

        if (Database is "SqlServer" or "PostgreSQL" or "MySql")
        {
            containers.Add(Database is "SqlServer" ? "SQL Server" : Database);
        }

        if (IsTrue("redis"))
        {
            containers.Add("Redis");
        }

        switch (this["filesStorage"])
        {
            case "AzureBlobStorage":
                containers.Add("Azurite");
                break;
            case "S3":
                containers.Add("RustFS");
                break;
        }

        return containers;
    }

    public bool HasExplicitValue(string name) => values.ContainsKey(name);

    public string? Set(string name, string value)
    {
        var parameter = Manifest.Find(name);

        if (parameter is null)
            return $"bit Boilerplate has no '{name}' option.";

        var normalized = parameter.Normalize(value);

        if (normalized is null)
        {
            return parameter.Type is TemplateParameterType.Choice
                ? $"'{value}' isn't a valid --{parameter.Name}. Use one of: {string.Join(", ", parameter.Choices.Select(c => c.Value))}."
                : $"'{value}' isn't a valid --{parameter.Name}. Use true or false.";
        }

        values[parameter.Name] = normalized;
        return null;
    }

    public IEnumerable<(TemplateParameter Parameter, string Value)> NonDefaultValues()
    {
        foreach (var parameter in Manifest.Parameters)
        {
            var value = this[parameter.Name];

            if (string.Equals(value, parameter.DefaultValue, StringComparison.OrdinalIgnoreCase) is false)
                yield return (parameter, value);
        }
    }

    public IReadOnlyList<string> ToTemplateArguments()
    {
        var arguments = new List<string>();

        foreach (var (parameter, value) in NonDefaultValues())
        {
            arguments.Add($"--{parameter.Name}");
            arguments.Add(value);
        }

        return arguments;
    }

    public IReadOnlyList<string> ToCommandLineTokens()
    {
        var tokens = new List<string>();

        foreach (var (parameter, value) in NonDefaultValues())
        {
            tokens.Add($"--{parameter.Name}");

            if (parameter.Type is not TemplateParameterType.Bool || value is not "true")
            {
                tokens.Add(value);
            }
        }

        return tokens;
    }
}
