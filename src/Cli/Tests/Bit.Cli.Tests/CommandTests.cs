using Bit.Cli.Commands;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class CommandTests
{
    [TestMethod]
    public void TheRootCommand_Should_ListEveryCommand()
    {
        var root = CliApp.BuildRootCommand(() => throw new InvalidOperationException());

        CollectionAssert.AreEquivalent(
            new[] { "new", "setup", "doctor", "trust", "translate", "decode", "telemetry", "about" },
            root.Subcommands.Select(c => c.Name).ToArray());
    }

    [TestMethod]
    public void EveryCommandAndOption_Should_HaveADescription()
    {
        var root = CliApp.BuildRootCommand(() => throw new InvalidOperationException());

        foreach (var command in root.Subcommands)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(command.Description), command.Name);

            foreach (var option in command.Options.Where(o => o.Name is not "--help" and not "-h"))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Description), $"{command.Name} {option.Name}");
            }
        }
    }

    [TestMethod]
    public async Task About_Should_SayWhereTheBuildComesFrom()
    {
        using var host = new TestHost();

        Assert.AreEqual(0, await host.RunAsync("about"));

        StringAssert.Contains(host.Output, BuildInfo.Version);
        StringAssert.Contains(host.Output, "github.com/bitfoundation/bitplatform");
        StringAssert.Contains(host.Output, "MIT");
    }

    [TestMethod]
    public async Task Telemetry_Should_SaveTheLevelAndExplainWhatIsSent()
    {
        using var host = new TestHost();

        Assert.AreEqual(0, await host.RunAsync("telemetry", "off"));
        StringAssert.Contains(host.Output, "Telemetry is off");
        StringAssert.Contains(File.ReadAllText(Path.Combine(host.Home, ".bitplatform", "settings.json")), "\"telemetry\": \"off\"");
    }

    [TestMethod]
    public async Task Telemetry_Should_SayWhatOverridesTheSetting()
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { ["DO_NOT_TRACK"] = "1" });

        Assert.AreEqual(0, await host.RunAsync("telemetry", "all"));
        StringAssert.Contains(host.Output, "DO_NOT_TRACK");
        StringAssert.Contains(host.Output, "overrides");
    }

    [TestMethod]
    public async Task New_Should_RejectAnInvalidNameWithASuggestion()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "my-app", "--yes"));
        StringAssert.Contains(host.Output, "bit new MyApp");
    }

    [TestMethod]
    public async Task New_Should_AskForANameWhenItCantPrompt()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "--yes"));
        StringAssert.Contains(host.Output, "bit new <name>");
    }

    [TestMethod]
    public async Task New_Should_RefuseAFolderThatIsntEmpty()
    {
        using var host = new TestHost();
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
        File.WriteAllText(Path.Combine(folder, "keep.txt"), "");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--yes"));
        StringAssert.Contains(host.Output, "isn't empty");
    }

    [TestMethod]
    public async Task New_Should_RejectAnUnknownChoice()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--database", "Oracle", "--yes"));
        StringAssert.Contains(host.Output, "PostgreSQL");
    }

    [TestMethod]
    public async Task ADryRun_Should_ShowThePlanAndChangeNothing()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        var exitCode = await host.RunAsync("new", "Contoso.Shop", "--database", "postgresql", "--redis", "--aspire", "false", "--platforms", "web,android", "--dry-run", "--yes");

        Assert.AreEqual(0, exitCode, host.Output);
        StringAssert.Contains(host.Output, "Database: PostgreSQL");
        StringAssert.Contains(host.Output, "Web, Android");
        StringAssert.Contains(host.Output, "Dry run");
        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "Contoso.Shop")));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "new"));
    }

    [TestMethod]
    public async Task ADryRun_Should_ListMissingToolsNeededByTheProject()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Runner.NotFound("docker");
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";

        var exitCode = await host.RunAsync("new", "Contoso", "--dry-run", "--yes");

        Assert.AreEqual(0, exitCode, host.Output);
        StringAssert.Contains(host.Output, "Install");
        StringAssert.Contains(host.Output, "Docker");
        StringAssert.Contains(host.Output, "sudo");
    }

    [TestMethod]
    public async Task Doctor_Should_FailTheCheckWhenANeededToolIsMissing()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("doctor"));

        host.Runner.NotFound("node");
        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("doctor"));
        StringAssert.Contains(host.Output, "bit doctor --fix");
    }

    [TestMethod]
    public async Task Setup_Should_RequireAProjectFolder()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("setup", "--yes"));
        StringAssert.Contains(host.Output, "isn't a bit Boilerplate project folder");
    }

    [TestMethod]
    public async Task Decode_Should_ReadATraceBackIntoSourceNames()
    {
        using var host = new TestHost();
        var map = Path.Combine(host.WorkingDirectory, "bit-minifier.map");
        var trace = Path.Combine(host.WorkingDirectory, "trace.txt");
        File.WriteAllLines(map, ["App\tM\tApp.Services.Worker::Process\taa", "App\tT\tApp.Services.Worker\t_a"]);
        File.WriteAllText(trace, "   at _a.aa(String name)\n");

        var output = await CaptureConsoleAsync(() => host.RunAsync("decode", map, trace));

        Assert.AreEqual(0, output.ExitCode);
        Assert.AreEqual("   at App.Services.Worker.Process(String name)\n", output.Text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Decode_Should_FindTheNewestMapUnderObj()
    {
        using var host = new TestHost();
        var older = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "obj", "Release", "net9.0")).FullName, "bit-minifier.map");
        var newer = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "App", "obj", "Release", "net10.0")).FullName, "bit-minifier.map");
        File.WriteAllText(older, "");
        File.WriteAllText(newer, "");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddDays(-1));
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit-minifier.map"), "");

        Assert.AreEqual(newer, DecodeCommand.FindMap(host.WorkingDirectory));
    }

    private static async Task<(int ExitCode, string Text)> CaptureConsoleAsync(Func<Task<int>> run)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var exitCode = await run();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
