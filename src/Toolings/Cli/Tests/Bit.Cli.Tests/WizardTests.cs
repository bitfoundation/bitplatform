using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class WizardTests
{
    [TestMethod]
    public async Task Docker_Should_BeCheckedWhenAspireRunsTheProjectsContainers()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--redis"), host.Output);

        var install = prompter.Find("Install these?");
        Assert.IsNotNull(install, host.Output);
        var docker = install.Choices.Single(c => c.Contains("Docker", StringComparison.Ordinal));
        Assert.Contains(docker, install.Preselected.ToList());
        Assert.Contains("Keycloak, Mailpit and Redis in containers", docker);
    }

    [TestMethod]
    public async Task Docker_Should_BeOfferedUncheckedWithoutAspire()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--aspire", "false"), host.Output);

        var install = prompter.Find("Install these?");
        Assert.IsNotNull(install, host.Output);
        var docker = install.Choices.Single(c => c.Contains("Docker", StringComparison.Ordinal));
        Assert.DoesNotContain(docker, install.Preselected.ToList());
    }

    [TestMethod]
    public async Task SayingNo_Should_LeaveTheMachineAsItWas()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "Contoso")));
        Assert.DoesNotContain(c => c.FileName is "sudo" || c.Arguments.FirstOrDefault() is "new" or "install", host.Runner.Calls, string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine)));
    }

    [TestMethod]
    public async Task NothingAboutTheProject_Should_BeAsked()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.Executables["rider"] = typeof(WizardTests).Assembly.Location;

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        Assert.AreSequenceEqual(new[] { "Create it?" }, prompter.Prompts.Select(p => p.Question).ToArray(), host.Output);
        Assert.Contains("open in VS Code", host.FlatOutput);
    }

    [TestMethod]
    public async Task AMissingName_Should_PointToTheCreateProjectPage()
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new"), host.Output);

        Assert.Contains("bit new <name>", host.Output);
        Assert.Contains("https://bitplatform.dev/templates/create-project", host.Output);
        Assert.IsEmpty(prompter.Prompts);
    }

    [TestMethod]
    public async Task AnInvalidName_Should_StopWithASuggestion()
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "my-app"), host.Output);

        Assert.Contains("Try: bit new MyApp", host.Output);
        Assert.IsEmpty(prompter.Prompts);
        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "my-app")));
    }

    [TestMethod]
    public async Task VsCode_Should_BeTheIdeOutsideCi()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);
        Assert.Contains("VS Code extensions, a first Aspire start, dotnet format, open in VS Code", host.FlatOutput);

        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        ci.AllToolsInstalled();

        Assert.AreEqual(0, await ci.RunAsync("new", "Contoso", "--dry-run", "--yes"), ci.Output);
        Assert.IsFalse(ci.FlatOutput.Contains("open in", StringComparison.Ordinal), ci.Output);
        Assert.IsFalse(ci.Output.Contains("Chromium", StringComparison.Ordinal), ci.Output);
    }

    [TestMethod]
    [DataRow(HostOs.Linux, new[] { "Android" })]
    [DataRow(HostOs.Windows, new[] { "Android", "iOS", "macOS", "Windows" })]
    [DataRow(HostOs.MacOS, new[] { "Android", "iOS", "macOS" })]
    public void TheNativeAppsBitSetupOffers_Should_OnlyIncludeWhatThisMachineCanBuild(HostOs os, string[] expected)
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(os, prompter: prompter);

        NewWorkflow.AskPlatforms(host.Services, [Platform.Web]);

        var platforms = prompter.Find("Set up native apps on this machine now too?");
        Assert.IsNotNull(platforms, host.Output);
        Assert.AreSequenceEqual(expected, platforms.Choices.ToArray());
        Assert.IsEmpty(platforms.Preselected);
        Assert.Contains("Every project has the web, Android, iOS, Windows and macOS apps.", host.Output);
        Assert.Contains("builds the whole solution", host.Output);
    }

    private static TestHost MissingDocker(ScriptedPrompter prompter)
    {
        var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.NotFound("docker");
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        return host;
    }
}
