using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;

namespace Bit.Cli.Tools;

public enum ToolState
{
    Installed,
    Missing,
    Outdated,
    NotRunning
}

public sealed record ToolStatus(ToolState State, string? Version = null, string? Detail = null)
{
    public bool IsSatisfied => State is ToolState.Installed;

    public static ToolStatus Missing(string? detail = null) => new(ToolState.Missing, null, detail);

    public static ToolStatus Installed(string? version = null) => new(ToolState.Installed, version);
}

public sealed record SdkRequirement(string Version, string? RollForward = null, bool? AllowPrerelease = null)
{
    public bool Preview => Version.Contains('-');

    public bool Pinned => string.Equals(RollForward, "disable", StringComparison.OrdinalIgnoreCase);

    public string Channel => string.Join('.', Version.Split('.').Take(2));

    public string GlobalJson
    {
        get
        {
            var sdk = new JsonObject { ["version"] = Version };

            if (RollForward is not null)
            {
                sdk["rollForward"] = RollForward;
            }

            if (AllowPrerelease is not null)
            {
                sdk["allowPrerelease"] = AllowPrerelease;
            }

            return new JsonObject { ["sdk"] = sdk }.ToJsonString();
        }
    }

    public static SdkRequirement? FromGlobalJson(string? json)
    {
        if (json is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (document.RootElement.TryGetProperty("sdk", out var sdk) is false || sdk.TryGetProperty("version", out var version) is false || version.GetString() is not { } text || ToolCatalog.ParseVersion(text) is null)
                return null;

            var rollForward = sdk.TryGetProperty("rollForward", out var roll) && roll.ValueKind is JsonValueKind.String ? roll.GetString() : null;
            bool? allowPrerelease = sdk.TryGetProperty("allowPrerelease", out var allow) && allow.ValueKind is JsonValueKind.True or JsonValueKind.False ? allow.GetBoolean() : null;

            return new(text, rollForward, allowPrerelease);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record ToolNeeds
{
    public bool Aspire { get; init; }

    public IReadOnlyList<string> Containers { get; init; } = [];

    public bool NativeWebAssembly { get; init; }

    public bool GitHubRepo { get; init; }

    public IReadOnlySet<Platform> Platforms { get; init; } = new HashSet<Platform> { Platform.Web };

    public string? Ide { get; init; }

    public SdkRequirement? Sdk { get; init; }

    public int? NodeMajor { get; init; }

    public Version? AspireVersion { get; init; }

    public bool NeedsMaui => Platforms.Any(p => p is not Platform.Web);
}

public enum Elevation
{
    None,
    Admin,
    Sudo
}

public sealed record ToolAction
{
    public required string ToolId { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyList<ProcessSpec> Commands { get; init; }

    public Elevation Elevation { get; init; }

    public bool Interactive { get; init; }

    public string? AfterInstall { get; init; }

    public IReadOnlyList<int> SuccessExitCodes { get; init; } = [0];

    public bool Optional { get; init; }

    public PartialSuccess? Partial { get; init; }
}

public sealed record PartialSuccess(int ExitCode, string Detail, string Hint);

public sealed class ToolContext(CliEnvironment environment, IProcessRunner runner, ToolNeeds needs, PackageManagers packageManagers)
{
    public CliEnvironment Environment { get; } = environment;

    public IProcessRunner Runner { get; } = runner;

    public ToolNeeds Needs { get; } = needs;

    public PackageManagers PackageManagers { get; } = packageManagers;

    public async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken, TimeSpan? timeout = null, IReadOnlyDictionary<string, string?>? environment = null)
    {
        return await Runner.RunAsync(new ProcessSpec
        {
            FileName = fileName,
            Arguments = arguments,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            Environment = environment
        }, cancellationToken);
    }
}

public abstract class Tool
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public virtual bool AppliesTo(ToolContext context) => true;

    public virtual bool AppliesInCi(ToolContext context) => true;

    public abstract string Why(ToolContext context);

    public virtual bool IsNeeded(ToolContext context) => false;

    public abstract Task<ToolStatus> DetectAsync(ToolContext context, CancellationToken cancellationToken);

    public abstract ToolAction? PlanInstall(ToolContext context, ToolStatus status);

    public virtual string? ManualInstructions(ToolContext context) => null;
}

public sealed record ToolCheck(Tool Tool, ToolStatus Status, bool Needed, string Why, ToolAction? Action, string? Manual)
{
    public bool CanInstall => Action is not null;
}
