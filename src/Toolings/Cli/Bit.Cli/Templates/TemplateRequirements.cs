using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Tools;

namespace Bit.Cli.Templates;

public sealed partial record TemplateRequirements(int? NodeMajor, Version? Aspire, SdkRequirement? Sdk)
{
    private const string PackageJsonEntry = "Bit.Boilerplate/src/Client/Boilerplate.Client.Core/package.json";

    private const string AppHostEntry = "Bit.Boilerplate/src/Server/Boilerplate.Server.AppHost/Boilerplate.Server.AppHost.csproj";

    private const string GlobalJsonEntry = "Bit.Boilerplate/global.json";

    public static TemplateRequirements Embedded { get; } = new(ParseNodeMajor(ReadResource("Bit.Cli.package.json")), ParseAspire(ReadResource("Bit.Cli.AppHost.csproj")), SdkRequirement.FromGlobalJson(ReadResource("Bit.Cli.global.json")));

    public static TemplateRequirements FromPackage(string packagePath)
    {
        return new(
            ParseNodeMajor(TemplateSource.ReadEntry(packagePath, PackageJsonEntry)),
            ParseAspire(TemplateSource.ReadEntry(packagePath, AppHostEntry)),
            SdkRequirement.FromGlobalJson(TemplateSource.ReadEntry(packagePath, GlobalJsonEntry)));
    }

    public static TemplateRequirements FromProject(string directory, string name)
    {
        return new(
            ParseNodeMajor(ReadFile(Path.Combine(directory, "src", "Client", $"{name}.Client.Core", "package.json"))),
            ParseAspire(ReadFile(Path.Combine(directory, "src", "Server", $"{name}.Server.AppHost", $"{name}.Server.AppHost.csproj"))),
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

    public static Version? ParseAspire(string? appHostProject)
    {
        return appHostProject is not null && AspireSdkRegex().Match(appHostProject) is { Success: true } match && Version.TryParse(match.Groups["version"].Value, out var version)
            ? version
            : null;
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

    [GeneratedRegex(@"Aspire\.AppHost\.Sdk/(?<version>\d+\.\d+\.\d+)")]
    private static partial Regex AspireSdkRegex();
}
