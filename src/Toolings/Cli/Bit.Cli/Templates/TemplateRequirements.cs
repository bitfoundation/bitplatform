using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Bit.Cli.Tools;

namespace Bit.Cli.Templates;

public sealed partial record TemplateRequirements(int? NodeMajor, Version? Aspire, SdkRequirement? Sdk, string? GlobalJson = null)
{
    private const string PackageJsonEntry = "Bit.Boilerplate/src/Client/Boilerplate.Client.Core/package.json";

    private const string PackagesPropsEntry = "Bit.Boilerplate/src/Directory.Packages.props";

    private const string GlobalJsonEntry = "Bit.Boilerplate/global.json";

    public static TemplateRequirements Embedded { get; } = FromTemplate(ReadResource("Bit.Cli.package.json"), ReadResource("Bit.Cli.Directory.Packages.props"), ReadResource("Bit.Cli.global.json"));

    public static TemplateRequirements FromPackage(string packagePath)
    {
        return FromTemplate(
            TemplateSource.ReadEntry(packagePath, PackageJsonEntry),
            TemplateSource.ReadEntry(packagePath, PackagesPropsEntry),
            TemplateSource.ReadEntry(packagePath, GlobalJsonEntry));
    }

    public TemplateRequirements For(TemplateSelection selection)
    {
        return GlobalJson is null ? this : this with { Sdk = SdkRequirement.FromGlobalJson(selection.Manifest.ApplySwitches(GlobalJson, selection)) ?? Sdk };
    }

    private static TemplateRequirements FromTemplate(string? packageJson, string? packagesProps, string? globalJson)
    {
        return new(ParseNodeMajor(packageJson), ParseAspire(packagesProps), SdkRequirement.FromGlobalJson(globalJson), globalJson);
    }

    public static TemplateRequirements FromProject(string directory, string name)
    {
        return new(
            ParseNodeMajor(ReadFile(Path.Combine(directory, "src", "Client", $"{name}.Client.Core", "package.json"))),
            ParseAspire(ReadFile(Path.Combine(directory, "src", "Directory.Packages.props"))),
            SdkRequirement.FromGlobalJson(ReadFile(Path.Combine(directory, "global.json"))));
    }

    public static int? ParseNodeMajor(string? packageJson)
    {
        if (packageJson is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(packageJson, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            return document.RootElement.TryGetProperty("engines", out var engines) && engines.TryGetProperty("node", out var node) && node.ValueKind is JsonValueKind.String
                && NodeMajorRegex().Match(node.GetString()!) is { Success: true } match
                ? int.Parse(match.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static Version? ParseAspire(string? packagesProps)
    {
        if (packagesProps is null)
            return null;

        try
        {
            return XDocument.Parse(packagesProps).Descendants()
                .Where(e => e.Name.LocalName == "PackageVersion" && string.Equals((string?)e.Attribute("Include"), "Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase))
                .Select(e => Version.TryParse((string?)e.Attribute("Version"), out var version) ? version : null)
                .FirstOrDefault(v => v is not null);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static string? ReadResource(string name)
    {
        using var stream = typeof(TemplateRequirements).Assembly.GetManifestResourceStream(name);

        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? ReadFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"(?<major>\d+)")]
    private static partial Regex NodeMajorRegex();
}
