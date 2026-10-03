using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Bit.Cli.Infrastructure;

public enum HostOs
{
    Windows,
    MacOS,
    Linux
}

public sealed class CliEnvironment
{
    public static readonly string[] InProcessOnlyVariables =
    [
        "APPLICATIONINSIGHTS_STATSBEAT_DISABLED",
        "APPLICATIONINSIGHTS_CLOUD_ROLE_NAME",
        "APPLICATIONINSIGHTS_CLOUD_ROLE_INSTANCE",
        "OTEL_RESOURCE_ATTRIBUTES",
        "OTEL_SERVICE_NAME",
        "OTEL_TRACES_SAMPLER",
        "OTEL_TRACES_SAMPLER_ARG"
    ];

    public required HostOs Os { get; init; }

    public required Architecture Architecture { get; init; }

    public required string HomeDirectory { get; init; }

    public required string CurrentDirectory { get; init; }

    private readonly Dictionary<string, string> variables = new(StringComparer.Ordinal);

    public required IReadOnlyDictionary<string, string> Variables
    {
        get => variables;
        init => variables = new Dictionary<string, string>(value, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    public bool IsInputRedirected { get; init; }

    public bool IsOutputRedirected { get; init; }

    public bool IsElevated { get; init; }

    public string BitDirectory => Path.Combine(HomeDirectory, ".bitplatform");

    public string LogsDirectory => Path.Combine(BitDirectory, "logs");

    public bool IsWindows => Os is HostOs.Windows;

    public bool IsMacOS => Os is HostOs.MacOS;

    public bool IsLinux => Os is HostOs.Linux;

    public string? GetVariable(string name)
    {
        return Variables.TryGetValue(name, out var value) && string.IsNullOrEmpty(value) is false ? value : null;
    }

    public void PrependToPath(IEnumerable<string> directories)
    {
        var current = (GetVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        var comparer = IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var added = directories.Where(d => Directory.Exists(d) && current.Contains(d, comparer) is false).ToList();

        if (added.Count == 0)
            return;

        var path = string.Join(Path.PathSeparator, added.Concat(current));
        variables["PATH"] = path;
        Environment.SetEnvironmentVariable("PATH", path);
    }

    public bool IsVariableTrue(string name)
    {
        return GetVariable(name)?.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";
    }

    public string? CiName => this switch
    {
        _ when GetVariable("GITHUB_ACTIONS") is "true" => "github-actions",
        _ when GetVariable("TF_BUILD") is not null => "azure-pipelines",
        _ when GetVariable("GITLAB_CI") is not null => "gitlab",
        _ when GetVariable("CIRCLECI") is not null => "circleci",
        _ when GetVariable("APPVEYOR") is not null => "appveyor",
        _ when GetVariable("TRAVIS") is not null => "travis",
        _ when GetVariable("TEAMCITY_VERSION") is not null => "teamcity",
        _ when GetVariable("JENKINS_URL") is not null => "jenkins",
        _ when GetVariable("BITBUCKET_BUILD_NUMBER") is not null => "bitbucket",
        _ when GetVariable("CODEBUILD_BUILD_ID") is not null => "aws-codebuild",
        _ when GetVariable("BUILDKITE") is not null => "buildkite",
        _ when IsVariableTrue("CI") => "other",
        _ => null
    };

    public bool IsCI => CiName is not null;

    public string? CodingAgent => this switch
    {
        _ when GetVariable("CLAUDECODE") is not null => "claude-code",
        _ when Variables.Keys.Any(k => k.StartsWith("COPILOT_AGENT", StringComparison.OrdinalIgnoreCase)) => "copilot",
        _ when Variables.Keys.Any(k => k.StartsWith("CODEX_SANDBOX", StringComparison.OrdinalIgnoreCase)) => "codex",
        _ when GetVariable("GEMINI_CLI") is not null => "gemini",
        _ when GetVariable("AGENT_CLI") is not null => "other",
        _ => null
    };

    public string Terminal => this switch
    {
        _ when GetVariable("TERM_PROGRAM") is "vscode" => "vscode",
        _ when GetVariable("WT_SESSION") is not null => "windows-terminal",
        _ when GetVariable("TERM_PROGRAM") is "Apple_Terminal" => "apple-terminal",
        _ when GetVariable("TERM_PROGRAM") is "iTerm.app" => "iterm",
        _ when GetVariable("TERMINAL_EMULATOR")?.Contains("JetBrains", StringComparison.Ordinal) is true => "jetbrains",
        _ => "other"
    };

    public static CliEnvironment FromProcess()
    {
        var variables = new Dictionary<string, string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                variables[key] = value;
            }
        }

        var home = variables.TryGetValue("BIT_CLI_HOME", out var overriddenHome) && string.IsNullOrWhiteSpace(overriddenHome) is false
            ? overriddenHome
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new CliEnvironment
        {
            Os = OperatingSystem.IsWindows() ? HostOs.Windows : OperatingSystem.IsMacOS() ? HostOs.MacOS : HostOs.Linux,
            Architecture = RuntimeInformation.OSArchitecture,
            HomeDirectory = home,
            CurrentDirectory = Environment.CurrentDirectory,
            Variables = variables,
            IsInputRedirected = Console.IsInputRedirected,
            IsOutputRedirected = Console.IsOutputRedirected,
            IsElevated = DetectElevation()
        };
    }

    private static bool DetectElevation()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }

            return Environment.UserName is "root";
        }
        catch (Exception)
        {
            return false;
        }
    }
}
