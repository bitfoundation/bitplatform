using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tests.Infrastructure;

/// <summary>
/// Lets a test run git for real, as an identity of its own and with none of the machine's git settings, for this process and every git it starts.
/// </summary>
public sealed class GitIdentity : IDisposable
{
    private readonly Dictionary<string, string?> previous = [];

    public GitIdentity(string root)
    {
        var emptyConfig = Path.Combine(root, "empty.gitconfig");
        File.WriteAllText(emptyConfig, "");

        Set("GIT_AUTHOR_NAME", "bit tests");
        Set("GIT_AUTHOR_EMAIL", "tests@bitplatform.dev");
        Set("GIT_COMMITTER_NAME", "bit tests");
        Set("GIT_COMMITTER_EMAIL", "tests@bitplatform.dev");
        Set("GIT_CONFIG_GLOBAL", emptyConfig);
        Set("GIT_CONFIG_NOSYSTEM", "1");
    }

    public static Dictionary<string, string> RealPath() => new() { ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "" };

    public static async Task<bool> GitAvailableAsync()
    {
        using var host = new TestHost(variables: RealPath());
        var result = await new ProcessRunner(host.Environment, CliLog.None).RunAsync(new ProcessSpec { FileName = "git", Arguments = ["--version"] });
        return result.Succeeded;
    }

    public void Dispose()
    {
        foreach (var (key, value) in previous)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private void Set(string key, string value)
    {
        previous[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }
}
