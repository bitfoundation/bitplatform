using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ProcessRunnerTests
{
    [TestMethod]
    public async Task AMissingCommand_Should_BeReportedAsNotFound()
    {
        var runner = new ProcessRunner(CliEnvironment.FromProcess(), CliLog.None);

        var result = await runner.RunAsync(new ProcessSpec { FileName = "bit-cli-no-such-command-4f1d" });

        Assert.IsTrue(result.NotFound);
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task OutputAndExitCode_Should_BeCaptured()
    {
        var runner = new ProcessRunner(CliEnvironment.FromProcess(), CliLog.None);
        var lines = new List<string>();

        var result = await runner.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = ["--version"], OnOutputLine = lines.Add });

        Assert.IsTrue(result.Succeeded, result.Output);
        Assert.IsNotNull(Bit.Cli.Tools.ToolCatalog.ParseVersion(result.Output));
        Assert.IsNotEmpty(lines);
    }

    [TestMethod]
    public async Task ACommandThatTakesTooLong_Should_BeStopped()
    {
        var runner = new ProcessRunner(CliEnvironment.FromProcess(), CliLog.None);
        var spec = OperatingSystem.IsWindows()
            ? new ProcessSpec { FileName = "powershell.exe", Arguments = ["-NoProfile", "-Command", "Start-Sleep -Seconds 30"], Timeout = TimeSpan.FromSeconds(1) }
            : new ProcessSpec { FileName = "sleep", Arguments = ["30"], Timeout = TimeSpan.FromSeconds(1) };

        var started = DateTime.UtcNow;
        var result = await runner.RunAsync(spec);

        Assert.IsTrue(result.TimedOut);
        Assert.IsLessThan(TimeSpan.FromSeconds(20), DateTime.UtcNow - started);
    }

    [TestMethod]
    public void Executables_Should_BeFoundOnThePath()
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "" });
        var runner = new ProcessRunner(host.Environment, CliLog.None);

        Assert.IsNotNull(runner.FindExecutable("dotnet"));
        Assert.IsNull(runner.FindExecutable("bit-cli-no-such-command-4f1d"));
    }

    [TestMethod]
    [DataRow("simple", "simple")]
    [DataRow("with space", "\"with space\"")]
    [DataRow("C:\\Program Files\\x", "\"C:\\Program Files\\x\"")]
    [DataRow("a\"b", "\"a\\\"b\"")]
    public void Quote_Should_OnlyQuoteWhenNeeded(string value, string expected)
    {
        Assert.AreEqual(expected, ProcessSpec.Quote(value));
    }
}
