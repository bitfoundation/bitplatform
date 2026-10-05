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
        CollectionAssert.Contains(install.Preselected.ToList(), docker);
        StringAssert.Contains(docker, "Keycloak, Mailpit and Redis in containers");
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
        CollectionAssert.DoesNotContain(install.Preselected.ToList(), docker);
    }

    [TestMethod]
    public async Task SayingNo_Should_LeaveTheMachineAsItWas()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "Contoso")));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "sudo" || c.Arguments.FirstOrDefault() is "new" or "install"), string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine)));
    }

    [TestMethod]
    public async Task NothingAboutTheProject_Should_BeAsked()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.Executables["rider"] = typeof(WizardTests).Assembly.Location;

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        CollectionAssert.AreEqual(new[] { "Create it?" }, prompter.Prompts.Select(p => p.Question).ToArray(), host.Output);
        StringAssert.Contains(host.Output, "open in VS Code");
    }

    [TestMethod]
    public async Task AMissingName_Should_PointToTheCreateProjectPage()
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new"), host.Output);

        StringAssert.Contains(host.Output, "bit new <name>");
        StringAssert.Contains(host.Output, "https://bitplatform.dev/templates/create-project");
        Assert.IsEmpty(prompter.Prompts);
    }

    [TestMethod]
    public async Task AnInvalidName_Should_StopWithASuggestion()
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("new", "my-app"), host.Output);

        StringAssert.Contains(host.Output, "Try: bit new MyApp");
        Assert.IsEmpty(prompter.Prompts);
        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "my-app")));
    }

    [TestMethod]
    public async Task VsCode_Should_BeTheIdeOutsideCi()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);
        StringAssert.Contains(host.Output, "VS Code extensions, open in VS Code");

        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        ci.AllToolsInstalled();

        Assert.AreEqual(0, await ci.RunAsync("new", "Contoso", "--dry-run", "--yes"), ci.Output);
        Assert.IsFalse(ci.Output.Contains("open in", StringComparison.Ordinal), ci.Output);
        Assert.IsFalse(ci.Output.Contains("Chromium", StringComparison.Ordinal), ci.Output);
    }

    [TestMethod]
    [DataRow(HostOs.Linux, new[] { "Android" })]
    [DataRow(HostOs.Windows, new[] { "Android", "Windows" })]
    [DataRow(HostOs.MacOS, new[] { "Android", "iOS", "macOS" })]
    public void TheNativeAppsBitSetupOffers_Should_OnlyIncludeWhatThisMachineCanBuild(HostOs os, string[] expected)
    {
        var prompter = new ScriptedPrompter();
        using var host = new TestHost(os, prompter: prompter);

        NewWorkflow.AskPlatforms(host.Services, [Platform.Web]);

        var platforms = prompter.Find("Set up native apps on this machine now too?");
        Assert.IsNotNull(platforms, host.Output);
        CollectionAssert.AreEqual(expected, platforms.Choices.ToArray());
        Assert.IsEmpty(platforms.Preselected);
        StringAssert.Contains(host.Output, "Every project has the web, Android, iOS, Windows and macOS apps.");
        StringAssert.Contains(host.Output, "builds the whole solution");
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
