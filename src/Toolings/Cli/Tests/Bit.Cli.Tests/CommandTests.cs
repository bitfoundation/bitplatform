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

        Assert.AreSequenceEqual(
            new[] { "new", "setup", "run", "diff", "doctor", "trust", "clean", "rename", "remove", "migration", "translate", "decode", "keys", "pipeline", "aot-profile", "telemetry", "update", "about", "admin-helper" }, root.Subcommands.Select(c => c.Name).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public void ANestedCommand_Should_BeNamedByItsWholePath()
    {
        var root = CliApp.BuildRootCommand(() => throw new InvalidOperationException());

        Assert.AreSequenceEqual(new[] { "migration", "remove" }, CliApp.CommandPath(root.Parse(["migration", "remove"]).CommandResult, root).ToArray());
        Assert.AreSequenceEqual(new[] { "remove" }, CliApp.CommandPath(root.Parse(["remove"]).CommandResult, root).ToArray());
        Assert.IsEmpty(CliApp.CommandPath(root.Parse([]).CommandResult, root));
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

        Assert.Contains(BuildInfo.Version, host.Output);
        Assert.Contains("github.com/bitfoundation/bitplatform", host.Output);
        Assert.Contains("MIT", host.Output);
    }

    [TestMethod]
    public async Task Telemetry_Should_SaveTheLevelAndExplainWhatIsSent()
    {
        using var host = new TestHost();

        Assert.AreEqual(0, await host.RunAsync("telemetry", "off"));
        Assert.Contains("Telemetry is off", host.Output);
        Assert.Contains("\"telemetry\": \"off\"", File.ReadAllText(Path.Combine(host.Home, ".bitplatform", "settings.json")));
    }

    [TestMethod]
    public async Task Telemetry_Should_SayWhatOverridesTheSetting()
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { ["DO_NOT_TRACK"] = "1" });

        Assert.AreEqual(0, await host.RunAsync("telemetry", "all"));
        Assert.Contains("DO_NOT_TRACK", host.Output);
        Assert.Contains("overrides", host.Output);
    }

    [TestMethod]
    public async Task New_Should_RejectAnInvalidNameWithASuggestion()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "my-app", "--yes"));
        Assert.Contains("bit new MyApp", host.Output);
    }

    [TestMethod]
    public async Task New_Should_AskForANameWhenItCantPrompt()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "--yes"));
        Assert.Contains("bit new <name>", host.Output);
    }

    [TestMethod]
    public async Task New_Should_RefuseAFolderThatIsntEmpty()
    {
        using var host = new TestHost();
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
        File.WriteAllText(Path.Combine(folder, "keep.txt"), "");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--yes"));
        Assert.Contains("isn't empty", host.Output);
    }

    [TestMethod]
    public async Task New_Should_RejectAnUnknownChoice()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--database", "Oracle", "--yes"));
        Assert.Contains("PostgreSQL", host.Output);
    }

    [TestMethod]
    public async Task ADryRun_Should_ShowThePlanAndChangeNothing()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        var exitCode = await host.RunAsync("new", "Contoso.Shop", "--database", "postgresql", "--redis", "--aspire", "false", "--platforms", "web,android", "--dry-run", "--yes");

        Assert.AreEqual(0, exitCode, host.Output);
        Assert.Contains("Database: PostgreSQL", host.Output);
        Assert.Contains("Web, Android", host.Output);
        Assert.Contains("Dry run", host.Output);
        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "Contoso.Shop")));
        Assert.DoesNotContain(c => c.Arguments.FirstOrDefault() is "new", host.Runner.Calls);
    }

    [TestMethod]
    public async Task BuildProperties_Should_AcceptTheDotnetFormsAndRefuseOthers()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "-p:EnforceCodeStyleInBuild=true", "-p", "Environment=Staging", "--property:InvariantGlobalization=true", "--no-browsers", "--dry-run", "--yes"), host.Output);
        Assert.IsFalse(host.Output.Contains("Chromium", StringComparison.Ordinal), host.Output);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "-p:EnforceCodeStyleInBuild", "--dry-run", "--yes"), host.Output);
        Assert.Contains("isn't an MSBuild property", host.Output);
    }

    [TestMethod]
    public async Task AGitHubRepo_Should_NeedGit()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "Contoso", "--github-repo", "--no-git", "--dry-run", "--yes"), host.Output);
        Assert.Contains("can't go with --no-git", host.Output);
    }

    [TestMethod]
    public async Task ADryRun_Should_PlanTheGitHubRepoAndItsCli()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Runner.NotFound("gh");
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--github-repo", "--dry-run", "--yes"), host.Output);

        Assert.Contains("GitHub CLI", host.Output);
        Assert.Contains("private GitHub", host.Output.ReplaceLineEndings(" "));
        Assert.DoesNotContain(c => c.FileName is "gh" && c.Arguments.FirstOrDefault() is "repo" or "auth", host.Runner.Calls);
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
        Assert.Contains("Install", host.Output);
        Assert.Contains("Docker", host.Output);
        Assert.Contains("sudo", host.Output);
    }

    [TestMethod]
    public async Task Doctor_Should_FailTheCheckWhenANeededToolIsMissing()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("doctor"));

        host.Runner.NotFound("node");
        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("doctor"));
        Assert.Contains("bit doctor --fix", host.Output);
    }

    [TestMethod]
    public async Task Setup_Should_RequireAProjectFolder()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("setup", "--yes"));
        Assert.Contains("isn't a bit Boilerplate project folder", host.Output);
    }

    [TestMethod]
    public async Task Setup_Should_RefuseAnAppIdTheNativeAppsCantHave()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Shop")).FullName;
        File.WriteAllText(Path.Combine(folder, "Shop.Web.slnf"), "{}");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "src", "Client", "Shop.Client.Maui")).FullName, "Shop.Client.Maui.csproj"), "<Project>\n  <PropertyGroup>\n    <ApplicationId>com.companyname.</ApplicationId>\n  </PropertyGroup>\n</Project>\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("setup", folder, "--platforms", "android", "--yes"), host.Output);
        Assert.Contains("the app id com.companyname.,", host.FlatOutput);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Setup_Should_CheckTheAppIdOfAMauiProjectTheSolutionLists()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Shop")).FullName;
        const string maui = "src/Client/Shop.Client.Operations.Maui/Shop.Client.Operations.Maui.csproj";
        File.WriteAllText(Path.Combine(folder, "Shop.Web.slnf"), "{}");
        File.WriteAllText(Path.Combine(folder, "Shop.slnx"), $"""<Solution><Project Path="{maui}" /></Solution>""");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(folder, maui))!);
        File.WriteAllText(Path.Combine(folder, maui), "<Project><PropertyGroup><ApplicationId>com.companyname.</ApplicationId></PropertyGroup></Project>");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("setup", folder, "--platforms", "android", "--yes"), host.Output);

        Assert.Contains("the app id com.companyname.,", host.FlatOutput);
        Assert.Contains(maui, host.FlatOutput);
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
        Assert.AreSequenceEqual(new[] { minifier, "--decode", map, Path.Combine(host.WorkingDirectory, "trace.txt") }, run.Arguments.ToArray());
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

        Assert.Contains("<PackageDownload Include=\"Bit.Minifier\" Version=\"[10.8.0]\" />", project!);
        Assert.Contains($"-p:RestoreRootConfigDirectory={host.WorkingDirectory}", host.Runner.Calls[0].Arguments.ToArray());
        Assert.AreEqual(Path.Combine(host.Environment.BitDirectory, "decode"), Path.GetDirectoryName(host.Runner.Calls[0].WorkingDirectory), "Others can plant build files in the shared temp folder.");
        Assert.IsFalse(Directory.Exists(host.Runner.Calls[0].WorkingDirectory));
        Assert.AreSequenceEqual(new[] { minifier, "--decode", map }, host.Runner.Calls[1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task Decode_Should_SayWhyTheBitMinifierCouldNotBeRestored()
    {
        using var host = new TestHost();
        var map = WriteMap(host, "# Bit.Minifier 99.0.0");
        host.Runner.On("dotnet", "restore", 1, "  Determining projects to restore...\n  restore.csproj : error NU1102: Unable to find package Bit.Minifier with version (= 99.0.0)\n");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("decode", map));

        Assert.Contains("Couldn't restore Bit.Minifier 99.0.0", host.FlatOutput);
        Assert.Contains($"--decode {map}. The package is at https://www.nuget.org/packages/Bit.Minifier/99.0.0", host.Output);
        Assert.Contains("error NU1102", host.FlatOutput);
        Assert.ContainsSingle(host.Runner.Calls);
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

        Assert.Contains("doesn't name the Bit.Minifier that wrote it", host.FlatOutput);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public void Decode_Should_FindTheNewestMapUnderObj()
    {
        using var host = new TestHost();
        var older = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "obj", "Release", "net10.0")).FullName, "bit-minifier.map");
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
