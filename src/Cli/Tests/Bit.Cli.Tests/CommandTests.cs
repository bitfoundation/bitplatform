using System.Text.Json;
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
            new[] { "new", "setup", "diff", "doctor", "trust", "clean", "rename", "remove", "translate", "decode", "aot-profile", "telemetry", "update", "about" },
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
    public async Task BuildProperties_Should_AcceptTheDotnetFormsAndRefuseOthers()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "-p:EnforceCodeStyleInBuild=true", "-p", "Environment=Staging", "--property:InvariantGlobalization=true", "--no-browsers", "--dry-run", "--yes"), host.Output);
        Assert.IsFalse(host.Output.Contains("Chromium", StringComparison.Ordinal), host.Output);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "-p:EnforceCodeStyleInBuild", "--dry-run", "--yes"), host.Output);
        StringAssert.Contains(host.Output, "isn't an MSBuild property");
    }

    [TestMethod]
    public async Task AGitHubRepo_Should_NeedTheGitHubPipelineAndGit()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--pipeline", "Azure", "--github-repo", "--dry-run", "--yes"), host.Output);
        StringAssert.Contains(host.Output, "--github-repo needs the GitHub pipeline");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--github-repo", "--no-git", "--dry-run", "--yes"), host.Output);
        StringAssert.Contains(host.Output, "can't go with --no-git");
    }

    [TestMethod]
    public async Task ADryRun_Should_PlanTheGitHubRepoAndItsCli()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Runner.NotFound("gh");
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--github-repo", "--dry-run", "--yes"), host.Output);

        StringAssert.Contains(host.Output, "GitHub CLI");
        StringAssert.Contains(host.Output.ReplaceLineEndings(" "), "private GitHub");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "gh" && c.Arguments.FirstOrDefault() is "repo" or "auth"));
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
    public async Task Decode_Should_RunTheBitMinifierThatWroteTheMap()
    {
        using var host = new TestHost();
        var packages = Path.Combine(host.Home, ".nuget", "packages");
        var minifier = Path.Combine(Directory.CreateDirectory(Path.Combine(packages, "bit.minifier", "10.8.0-pre-01", "tools")).FullName, "Bit.Minifier.dll");
        File.WriteAllText(minifier, "");
        var map = WriteMap(host, "# Bit.Minifier 10.8.0-PRE-01");
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "trace.txt"), "   at _a.aa(String name)\n");

        Assert.AreEqual(0, await host.RunAsync("decode", map, "trace.txt"));

        var run = host.Runner.Calls.Single();
        Assert.AreEqual("dotnet", run.FileName);
        CollectionAssert.AreEqual(new[] { minifier, "--decode", map, Path.Combine(host.WorkingDirectory, "trace.txt") }, run.Arguments.ToArray());
        Assert.IsTrue(run.Interactive);
    }

    [TestMethod]
    public async Task Decode_Should_RestoreTheBitMinifierTheCacheDoesNotHave()
    {
        using var host = new TestHost();
        var packages = Path.Combine(host.Root, "packages");
        var minifier = Path.Combine(packages, "bit.minifier", "10.8.0", "tools", "Bit.Minifier.dll");
        var map = WriteMap(host, "# Bit.Minifier 10.8.0");
        string? project = null;
        host.Runner.On("dotnet", "restore", spec =>
        {
            project = File.ReadAllText(Path.Combine(spec.WorkingDirectory!, "restore.csproj"));
            Directory.CreateDirectory(Path.GetDirectoryName(minifier)!);
            File.WriteAllText(minifier, "");
            Directory.CreateDirectory(Path.Combine(spec.WorkingDirectory!, "obj"));
            File.WriteAllText(Path.Combine(spec.WorkingDirectory!, "obj", "project.assets.json"), JsonSerializer.Serialize(new { packageFolders = new Dictionary<string, object> { [packages + Path.DirectorySeparatorChar] = new { } } }));
            return new ProcessResult { ExitCode = 0 };
        });

        Assert.AreEqual(0, await host.RunAsync("decode", map));

        StringAssert.Contains(project, "<PackageDownload Include=\"Bit.Minifier\" Version=\"[10.8.0]\" />");
        CollectionAssert.Contains(host.Runner.Calls[0].Arguments.ToArray(), $"-p:RestoreRootConfigDirectory={host.WorkingDirectory}");
        Assert.IsFalse(Directory.Exists(host.Runner.Calls[0].WorkingDirectory));
        CollectionAssert.AreEqual(new[] { minifier, "--decode", map }, host.Runner.Calls[1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task Decode_Should_SayWhyTheBitMinifierCouldNotBeRestored()
    {
        using var host = new TestHost();
        var map = WriteMap(host, "# Bit.Minifier 99.0.0");
        host.Runner.On("dotnet", "restore", 1, "  Determining projects to restore...\n  restore.csproj : error NU1102: Unable to find package Bit.Minifier with version (= 99.0.0)\n");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("decode", map));

        StringAssert.Contains(host.FlatOutput, "Couldn't restore Bit.Minifier 99.0.0");
        StringAssert.Contains(host.FlatOutput, "error NU1102");
        Assert.HasCount(1, host.Runner.Calls);
    }

    [TestMethod]
    [DataRow("App\tT\tApp.Services.Worker\t_a")]
    [DataRow("# Bit.Minifier ../../evil")]
    [DataRow("# Bit.Minifier 10.8.0\" />")]
    public async Task Decode_Should_RefuseAMapThatDoesNotNameItsBitMinifier(string firstLine)
    {
        using var host = new TestHost();
        var map = WriteMap(host, firstLine);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("decode", map));

        StringAssert.Contains(host.FlatOutput, "doesn't name the Bit.Minifier that wrote it");
        Assert.IsEmpty(host.Runner.Calls);
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

    private static string WriteMap(TestHost host, string firstLine)
    {
        var map = Path.Combine(host.WorkingDirectory, "bit-minifier.map");
        File.WriteAllLines(map, [firstLine, "App\tT\tApp.Services.Worker\t_a"]);
        return map;
    }
}
