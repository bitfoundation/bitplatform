using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public sealed record ApiFinding(string Id, string Text, IReadOnlyList<string> Operations);

public sealed record ApiComparison(Version Version, IReadOnlyList<ApiFinding> Errors, IReadOnlyList<ApiFinding> Warnings, string? Attempt);

public sealed record SupportedAppVersions(Version Lowest, IReadOnlyList<(string Platform, Version Minimum)> Platforms);

public sealed partial class ApiCompatibility(CliServices cli, string directory, string name)
{
    public static readonly (string Key, string Title)[] Platforms = [("Android", "Android"), ("Ios", "iOS"), ("MacOS", "macOS"), ("Windows", "Windows"), ("Web", "web")];

    public const string SeverityLevels = "response-optional-property-removed ERR\n";

    public string Directory => directory;

    public string OpenApiDirectory => Path.Combine(directory, "openapi");

    public string CurrentDocument => Path.Combine(OpenApiDirectory, "current.json");

    public string ServerApiDirectory => Path.Combine(directory, "src", "Server", $"{name}.Server.Api");

    public string ServerApiProject => Path.Combine(ServerApiDirectory, $"{name}.Server.Api.csproj");

    public string VersionFile => Path.Combine(directory, "src", "Directory.Build.props");

    public string SettingsFile => Path.Combine(ServerApiDirectory, "appsettings.json");

    public string DocumentOf(Version version) => Path.Combine(OpenApiDirectory, $"{version}.json");

    public string Relative(string path) => Path.GetRelativePath(directory, path).Replace('\\', '/');

    public static Version Normalize(Version version) => version.Revision > 0
        ? new Version(version.Major, version.Minor, Math.Max(version.Build, 0), version.Revision)
        : new Version(version.Major, version.Minor, Math.Max(version.Build, 0));

    public (Version? Version, string? Problem) ReadVersion()
    {
        try
        {
            var value = File.Exists(VersionFile)
                ? XDocument.Load(VersionFile).Descendants().FirstOrDefault(e => e.Name.LocalName == "Version" && e.Parent?.Name.LocalName == "PropertyGroup")?.Value.Trim()
                : null;

            if (string.IsNullOrEmpty(value))
                return (null, $"{Relative(VersionFile)} has no <Version>, which is the app's version: add <Version>0.0.1</Version> to its first PropertyGroup.");

            return Version.TryParse(value, out var version)
                ? (Normalize(version), null)
                : (null, $"The <Version> in {Relative(VersionFile)} is '{value}', and a release needs a plain version such as 1.2.3: the stores and the force update compare it as numbers.");
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return (null, $"{Relative(VersionFile)} couldn't be read: {exp.Message}");
        }
    }

    public (SupportedAppVersions? Supported, string? Problem) ReadSupportedAppVersions(Version serverVersion)
    {
        var minimums = Platforms.ToDictionary(p => p.Key, _ => (string?)null);

        foreach (var file in new[] { SettingsFile, Path.Combine(ServerApiDirectory, "appsettings.Production.json") })
        {
            if (File.Exists(file) is false)
                continue;

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

                if (Property(document.RootElement, "SupportedAppVersions") is not { ValueKind: JsonValueKind.Object } section)
                    continue;

                foreach (var (key, _) in Platforms)
                {
                    if (Property(section, $"MinimumSupported{key}AppVersion") is { } value)
                    {
                        minimums[key] = value.ValueKind is JsonValueKind.String ? value.GetString() : null;
                    }
                }
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or JsonException)
            {
                return (null, $"{Relative(file)} couldn't be read: {exp.Message}");
            }
        }

        var platforms = new List<(string, Version)>();

        foreach (var (key, title) in Platforms)
        {
            if (string.IsNullOrWhiteSpace(minimums[key]))
            {
                platforms.Add((title, new Version(serverVersion.Major, 0, 0)));
            }
            else if (Version.TryParse(minimums[key], out var minimum))
            {
                platforms.Add((title, Normalize(minimum)));
            }
            else
            {
                return (null, $"SupportedAppVersions:MinimumSupported{key}AppVersion in {Relative(SettingsFile)} is '{minimums[key]}', which isn't a version.");
            }
        }

        return (new SupportedAppVersions(platforms.Min(p => p.Item2)!, platforms), null);
    }

    private static JsonElement? Property(JsonElement element, string name) => element.ValueKind is JsonValueKind.Object
        ? element.EnumerateObject().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Select(p => (JsonElement?)p.Value).LastOrDefault()
        : null;

    public IReadOnlyList<Version> ReleasedVersions()
    {
        if (System.IO.Directory.Exists(OpenApiDirectory) is false)
            return [];

        return [.. System.IO.Directory.EnumerateFiles(OpenApiDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(file => Version.TryParse(file, out var version) && file == Normalize(version).ToString() ? version : null)
            .OfType<Version>()
            .Order()];
    }

    public async Task<StepResult> BuildAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var title = $"Built {name}.Server.Api, which wrote {Relative(CurrentDocument)}";
        var failedTitle = $"Couldn't build {name}.Server.Api";

        var generated = Path.Combine(ServerApiDirectory, "obj", $"{name}.Server.Api.OpenApiFiles.cache");

        if (File.Exists(generated))
        {
            File.Delete(generated);
        }

        var startedAt = DateTime.UtcNow.AddSeconds(-2);

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["build", ServerApiProject],
            WorkingDirectory = directory,
            OnOutputLine = progress,
            Timeout = TimeSpan.FromMinutes(20),
            Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["MSBUILDTERMINALLOGGER"] = "off", ["DOTNET_CLI_UI_LANGUAGE"] = "en" }
        }, cancellationToken);

        if (result.Succeeded is false)
            return StepResult.FromProcess(result, title, failedTitle, $"Fix the build, then run this again: dotnet build {Relative(ServerApiProject)}");

        if (File.Exists(CurrentDocument) is false || File.GetLastWriteTimeUtc(CurrentDocument) < startedAt)
        {
            var followUp = $"{name}.Server.Api writes it on a Debug build once it references Microsoft.Extensions.ApiDescription.Server the way the bit Boilerplate template does: bit diff shows the template's setup.";
            return StepResult.Failed(failedTitle, $"the build didn't write {Relative(CurrentDocument)}", followUp) with { Attempt = result.Spec?.Attempt };
        }

        return StepResult.Succeeded(title);
    }

    public async Task<ApiComparison> CompareAsync(string oasdiff, Version version, CancellationToken cancellationToken)
    {
        var severityLevels = Path.Combine(Path.GetDirectoryName(oasdiff)!, "severity-levels.txt");
        await File.WriteAllTextAsync(severityLevels, SeverityLevels, cancellationToken);

        var result = await cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = oasdiff,
            Arguments = ["breaking", Relative(DocumentOf(version)), Relative(CurrentDocument), "--format", "json", "--severity-levels", severityLevels],
            WorkingDirectory = directory,
            Timeout = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        var json = result.OutputLines.FirstOrDefault(l => l.StartsWith('['));

        if (json is null)
            throw new InvalidOperationException($"oasdiff didn't compare {Relative(DocumentOf(version))} with {Relative(CurrentDocument)}: {(result.NotFound ? "it wasn't found" : result.OutputLines.FirstOrDefault() ?? $"exit code {result.ExitCode}")}");

        var findings = new List<(int Level, string Id, string Text, string Operation)>();

        using (var document = JsonDocument.Parse(json))
        {
            foreach (var item in document.RootElement.EnumerateArray())
            {
                findings.Add((
                    item.TryGetProperty("level", out var level) && level.TryGetInt32(out var number) ? number : 0,
                    item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    MediaTypeRegex().Replace(item.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "", ""),
                    $"{(item.TryGetProperty("operation", out var operation) ? operation.GetString() : null)} {(item.TryGetProperty("path", out var path) ? path.GetString() : null)}".Trim()));
            }
        }

        List<ApiFinding> Group(Func<int, bool> level) => [.. findings
            .Where(f => level(f.Level))
            .GroupBy(f => (f.Id, f.Text))
            .Select(g => new ApiFinding(g.Key.Id, g.Key.Text, [.. g.Select(f => f.Operation).Where(o => o.Length > 0).Distinct().Order(StringComparer.Ordinal)]))];

        return new ApiComparison(version, Group(l => l >= 3), Group(l => l == 2), result.Spec?.Attempt);
    }

    public bool DescribesCurrentApi(Version version)
    {
        var file = DocumentOf(version);

        return File.Exists(file) && JsonNode.DeepEquals(ApiOf(file), ApiOf(CurrentDocument));
    }

    public IReadOnlyList<(Version[] Versions, bool IsCurrent)> GroupBySameApi(IEnumerable<Version> versions)
    {
        var current = ApiOf(CurrentDocument);
        var groups = new List<(JsonNode? Api, List<Version> Versions)>();

        foreach (var version in versions)
        {
            var api = ApiOf(DocumentOf(version));
            var index = groups.FindIndex(g => JsonNode.DeepEquals(g.Api, api));

            if (index < 0)
            {
                groups.Add((api, [version]));
            }
            else
            {
                groups[index].Versions.Add(version);
            }
        }

        return [.. groups.Select(g => (g.Versions.ToArray(), JsonNode.DeepEquals(g.Api, current)))];
    }

    private static JsonNode? ApiOf(string file) => WithoutProse(JsonNode.Parse(File.ReadAllText(file)));

    private static JsonNode? WithoutProse(JsonNode? node) => node switch
    {
        JsonObject properties => new JsonObject(properties
            .Where(p => p.Key is not ("summary" or "description") || p.Value?.GetValueKind() is not JsonValueKind.String)
            .Select(p => KeyValuePair.Create(p.Key, WithoutProse(p.Value)))),
        JsonArray items => new JsonArray([.. items.Select(WithoutProse)]),
        JsonValue value when value.GetValueKind() is JsonValueKind.String => JsonValue.Create(value.GetValue<string>().ReplaceLineEndings("\n")),
        _ => node?.DeepClone()
    };

    [GeneratedRegex(@" \(media type: [^)]*\)")]
    private static partial Regex MediaTypeRegex();
}
