using System.Text.Json;
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

public sealed record SdkRequirement(Version Minimum, bool Preview, string? Exact = null)
{
    public static SdkRequirement? FromGlobalJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (document.RootElement.TryGetProperty("sdk", out var sdk) is false || sdk.TryGetProperty("version", out var version) is false || version.GetString() is not { } text)
                return null;

            var exact = sdk.TryGetProperty("rollForward", out var rollForward) && string.Equals(rollForward.GetString(), "disable", StringComparison.OrdinalIgnoreCase) ? text : null;

            return ToolCatalog.ParseVersion(text) is { } minimum ? new(minimum, text.Contains('-'), exact) : null;
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
