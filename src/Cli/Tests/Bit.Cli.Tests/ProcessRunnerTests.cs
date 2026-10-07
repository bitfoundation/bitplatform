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
    public void ABatchFile_Should_GetEveryArgumentQuotedAndCaretEscaped()
    {
        Assert.AreEqual("/d /s /c \"C:\\Program^ Files\\Microsoft^ VS^ Code\\bin\\code.cmd ^\"C:\\R^&D\\Shop^\" ^\"a^ b^\"\"",
            WindowsShell.CommandLine(@"C:\Program Files\Microsoft VS Code\bin\code.cmd", [@"C:\R&D\Shop", "a b"]));
        Assert.IsTrue(WindowsShell.IsBatchFile(@"C:\x\code.CMD"));
        Assert.IsFalse(WindowsShell.IsBatchFile(@"C:\x\Code.exe"));
    }

    [TestMethod]
    public async Task ABatchFile_Should_ReceiveSpecialCharactersAsTyped()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var directory = Directory.CreateTempSubdirectory("bit-cli-cmd-").FullName;
        File.WriteAllText(Path.Combine(directory, "print.ps1"), "foreach ($a in $args) { [Console]::Out.WriteLine($a) }");
        File.WriteAllText(Path.Combine(directory, "shim.cmd"), "@\"%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe\" -NoProfile -File \"%~dp0print.ps1\" %*\r\n");
        string[] arguments = [@"C:\R&D\Shop", "a b", "x^y", "100%", "%PATH%", "(paren)", "semi;colon", "A&B (1)"];
        var runner = new ProcessRunner(CliEnvironment.FromProcess(), CliLog.None);

        var result = await runner.RunAsync(new ProcessSpec { FileName = Path.Combine(directory, "shim.cmd"), Arguments = arguments });

        Assert.IsTrue(result.Succeeded, result.Output);
        CollectionAssert.AreEqual(arguments, result.OutputLines.ToArray(), result.Output);
        Directory.Delete(directory, recursive: true);
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
