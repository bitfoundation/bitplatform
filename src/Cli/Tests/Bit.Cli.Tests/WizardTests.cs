using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class WizardTests
{
    [TestMethod]
    public async Task Docker_Should_BeCheckedWhenAspireRunsTheProjectsContainers()
    {
        var prompter = new ScriptedPrompter().On("Features", new[] { "Redis" }).On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        var install = prompter.Find("Install these?");
        Assert.IsNotNull(install, host.Output);
        var docker = install.Choices.Single(c => c.Contains("Docker", StringComparison.Ordinal));
        CollectionAssert.Contains(install.Preselected.ToList(), docker);
        StringAssert.Contains(docker, "Keycloak, Mailpit and Redis in containers");
    }

    [TestMethod]
    public async Task Docker_Should_BeOfferedUncheckedWithoutAspire()
    {
        var prompter = new ScriptedPrompter().On(".NET Aspire?", false).On("Create it?", false);
        using var host = MissingDocker(prompter);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

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
    [DataRow(HostOs.Linux, new[] { "Android" })]
    [DataRow(HostOs.Windows, new[] { "Android", "Windows" })]
    [DataRow(HostOs.MacOS, new[] { "Android", "iOS", "macOS" })]
    public async Task NativeApps_Should_OnlyIncludeWhatThisMachineCanBuild(HostOs os, string[] expected)
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(os, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        var platforms = prompter.Find("Set up native apps now too?");
        Assert.IsNotNull(platforms, host.Output);
        CollectionAssert.AreEqual(expected, platforms.Choices.ToArray());
        Assert.IsEmpty(platforms.Preselected);
        StringAssert.Contains(host.Output, "much longer");
    }

    [TestMethod]
    public async Task OptionsOnTheCommandLine_Should_NotBeAskedAgain()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--database", "PostgreSQL", "--aspire", "false", "--platforms", "web", "--ide", "none"), host.Output);

        Assert.IsNull(prompter.Find("Database"));
        Assert.IsNull(prompter.Find(".NET Aspire?"));
        Assert.IsNull(prompter.Find("Set up native apps now too?"));
        Assert.IsNull(prompter.Find("Open it in"));
        Assert.IsNotNull(prompter.Find("Features"));
        Assert.IsNull(prompter.Find("Install these?"), "every tool is installed");
    }

    [TestMethod]
    public async Task TheAnswers_Should_BecomeACommandThatAsksNothing()
    {
        var prompter = new ScriptedPrompter()
            .On("Database", "PostgreSQL")
            .On("Set up native apps now too?", new[] { "Android" })
            .On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        StringAssert.Contains(host.Output, "bit new Contoso --database PostgreSQL --platforms web,android --ide code --yes");
    }

    [TestMethod]
    public async Task AnInvalidName_Should_BeAskedAgainWithASuggestion()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "my-app"), host.Output);

        var name = prompter.Find("Project name");
        Assert.IsNotNull(name, host.Output);
        CollectionAssert.AreEqual(new[] { "MyApp" }, name.Preselected.ToArray());
        StringAssert.Contains(host.Output, "MyApp in ");
    }

    [TestMethod]
    public async Task TheIdeQuestion_Should_PreferVsCode()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.Executables["rider"] = typeof(WizardTests).Assembly.Location;

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        var ide = prompter.Find("Open it in");
        Assert.IsNotNull(ide, host.Output);
        CollectionAssert.AreEqual(new[] { "VS Code" }, ide.Preselected.ToArray());
        Assert.AreEqual("Don't open it", ide.Choices[^1]);
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
