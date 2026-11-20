using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ProjectBitTests
{
    private static readonly OldestProject[] oldestProjects = [new(SinceBit: "1.5.0", Template: "1.5.0"), new(SinceBit: "1.6.0", Template: "1.5.3")];

    [TestMethod]
    [DataRow("1.7.0", null, "1.6.2", false)]
    [DataRow("1.7.0", null, "1.7.0-pre-03", false)]
    [DataRow("1.7.0", null, "1.7.0", true)]
    [DataRow("1.7.0-pre-02", null, "1.7.0-pre-02", true)]
    [DataRow("1.7.0-pre-02", null, "1.7.0", true)]
    [DataRow("1.7.0", null, "2.0.0", true)]
    [DataRow(null, "1.5.0", "1.5.0", true)]
    [DataRow(null, "1.5.0", "1.5.0-pre-01", true)]
    [DataRow(null, "1.5.0", "1.5.1", false)]
    [DataRow("1.4.0", "1.5.0", "1.4.3", true)]
    [DataRow("1.4.0", "1.5.0", "1.3.9", false)]
    [DataRow("1.4.0", "1.5.0", "1.6.2", false)]
    public void TheVersionsInBitJson_Should_SayWhichBitFits(string? min, string? max, string version, bool fits)
    {
        using var host = new TestHost();
        WriteBitJson(host.WorkingDirectory, min, max);

        var project = ProjectBit.Find(host.WorkingDirectory, []);

        Assert.IsNotNull(project);
        Assert.AreEqual(fits, project.Fits(version));
        Assert.AreEqual(fits is false, project.NeedsNewerThan(version) || project.NeedsOlderThan(version));
    }

    [TestMethod]
    public void TheNearestBitJson_Should_BeTheOneThatCounts()
    {
        using var host = new TestHost();
        var inner = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Server", "Contoso.Server.Api")).FullName;
        WriteBitJson(host.Root, "1.7.0", null);

        Assert.AreEqual(Path.Combine(host.Root, "bit.json"), ProjectBit.Find(inner, [])!.FilePath);

        WriteBitJson(host.WorkingDirectory, "1.9.0", null);

        Assert.AreEqual("1.9.0", ProjectBit.Find(inner, [])!.MinVersion);

        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """{ "name": "Contoso" }""");

        Assert.IsNull(ProjectBit.Find(inner, []), "A project that doesn't say which version it is on takes any bit, whatever a folder above it says.");
    }

    [TestMethod]
    [DataRow("""{ "version": "the newest" }""")]
    [DataRow("""{ "version": 2 }""")]
    [DataRow("""{ "cli": "1.7.0" }""")]
    [DataRow("""{ "cli": { "minVersion": "1.7.0" } }""")]
    [DataRow("""{ "version": "1.7.0" """)]
    [DataRow("")]
    public void ABitJsonThatCantBeRead_Should_TakeAnyBit(string json)
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), json);

        Assert.IsNull(ProjectBit.Find(host.WorkingDirectory, []));
    }

    [TestMethod]
    public void ABoundThatCantBeRead_Should_LeaveTheOtherOne()
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """{ "version": " 1.7.0 ", "cli": { "maxVersion": "newest" } }""");

        var project = ProjectBit.Find(host.WorkingDirectory, [])!;

        Assert.AreEqual("1.7.0", project.MinVersion);
        Assert.IsNull(project.MaxVersion);
    }

    [TestMethod]
    [DataRow("1.4.0", "1.4.9", true)]
    [DataRow("1.4.0", "1.5.0-pre-01", true)]
    [DataRow("1.4.0", "1.5.0", false)]
    [DataRow("1.4.0", "1.6.2", false)]
    [DataRow("1.5.0-pre-02", "1.5.0", false)]
    [DataRow("1.5.0", "1.5.9", true)]
    [DataRow("1.5.0", "1.6.0-pre-01", true)]
    [DataRow("1.5.0", "1.6.0", false)]
    [DataRow("1.5.1", "1.6.2", false)]
    [DataRow("1.5.3", "1.6.2", true)]
    [DataRow("1.6.0", "2.0.0", true)]
    public void TheTemplateAProjectIsOn_Should_SayWhichBitIsTooNewForIt(string template, string version, bool fits)
    {
        using var host = new TestHost();
        WriteBitJson(host.WorkingDirectory, template, null);

        var project = ProjectBit.Find(host.WorkingDirectory, oldestProjects)!;

        Assert.AreEqual(fits, project.Fits(version));
        Assert.AreEqual(fits is false, project.IsTooOldFor(version));
        Assert.IsFalse(project.NeedsNewerThan(version));
    }

    [TestMethod]
    public void WhatBitJsonSaysAndWhatBitKnows_Should_BothCount()
    {
        using var host = new TestHost();

        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.4.5");
        var project = ProjectBit.Find(host.WorkingDirectory, oldestProjects)!;

        Assert.AreEqual("1.4.0", project.TemplateVersion);
        Assert.AreEqual("1.5.0", project.TooOldFor);
        Assert.IsTrue(project.Fits("1.4.5"));
        Assert.IsFalse(project.Fits("1.4.9"), "The lower of the two is the one that counts.");
        Assert.IsFalse(project.Fits("1.3.9"), "A bit is never older than the template the project is on.");

        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.9.0");

        Assert.IsFalse(ProjectBit.Find(host.WorkingDirectory, oldestProjects)!.Fits("1.5.0"), "A maxVersion written by hand doesn't make a newer bit work with the project.");

        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.3.5");
        project = ProjectBit.Find(host.WorkingDirectory, oldestProjects)!;

        Assert.IsTrue(project.Fits("1.3.5"), "A maxVersion below the project's own version was written on purpose, so it's the one that counts.");
        Assert.IsFalse(project.Fits("1.4.0"));
        Assert.IsNull(project.MinVersion);

        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """{ "version": "the newest", "name": "Contoso" }""");

        Assert.IsNull(ProjectBit.Find(host.WorkingDirectory, oldestProjects));

        WriteBitJson(host.WorkingDirectory, "1.4.0", null);

        Assert.IsNull(ProjectBit.Find(host.WorkingDirectory, [new(SinceBit: "soon", Template: "1.5.0"), new(SinceBit: "1.5.0", Template: "later"), new(SinceBit: "1.3.0", Template: "1.5.0")])!.TooOldFor);
    }

    [TestMethod]
    public void TheOldestProjectsBitWorksWith_Should_BeVersionsInTheOrderTheyCameOut()
    {
        var comparer = Comparer<string>.Create(SelfUpdate.CompareVersions);

        foreach (var oldest in ProjectBit.OldestProjects)
        {
            Assert.IsTrue(SelfUpdate.IsValidVersion(oldest.SinceBit), $"{oldest.SinceBit} isn't a bit version.");
            Assert.IsTrue(SelfUpdate.IsValidVersion(oldest.Template), $"{oldest.Template} isn't a bit Boilerplate version.");
            Assert.IsTrue(SelfUpdate.CompareVersions(oldest.Template, oldest.SinceBit) <= 0, $"bit {oldest.SinceBit} works with the template of its own version, so the oldest one it works with can't be {oldest.Template}.");
        }

        CollectionAssert.AreEqual(ProjectBit.OldestProjects.OrderBy(o => o.SinceBit, comparer).ToList(), ProjectBit.OldestProjects.ToList());
        CollectionAssert.AreEqual(ProjectBit.OldestProjects.OrderBy(o => o.Template, comparer).ToList(), ProjectBit.OldestProjects.ToList(), "A newer bit can't work with an older template than the bit before it did.");
    }

    [TestMethod]
    public void TheTemplatesBitJson_Should_SayWhichVersionAProjectIsOn()
    {
        var template = Path.GetDirectoryName(TestHost.RepositoryFile("src", "Templates", "Boilerplate", "Bit.Boilerplate", "bit.json"))!;

        var project = ProjectBit.Find(template);

        Assert.IsNotNull(project?.TemplateVersion, "bit.json of bit Boilerplate needs version: it is how bit knows which bit CLI versions work with the project.");
        Assert.AreEqual(project.TemplateVersion, project.MinVersion);
        Assert.IsNull(project.MaxVersion, "The template works with every newer bit, until bit says otherwise.");
        Assert.IsFalse(project.IsTooOldFor(BuildInfo.Version), "The template is never too old for the bit of its own version.");
    }

    [TestMethod]
    [DataRow(HostOs.Linux, "bit", "/bin/sh")]
    [DataRow(HostOs.Windows, "bit.exe", "powershell.exe")]
    public async Task ABitTooOldForTheProject_Should_HandTheCommandToTheNewestAndUpdateItself(HostOs os, string executable, string updater)
    {
        using var host = new TestHost(os);
        var copies = Official(host).CopiesDirectory;
        WriteBitJson(host.Root, "1.7.0", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0", "1.8.0", "1.9.0-pre-01"));
        var copy = InstallsCopy(host, "1.8.0", executable);
        RunsCopy(host, 7);

        Assert.AreEqual(7, await host.RunAsync("migration", "add", "AddRating", "--offline"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0 or newer, so bit 1.8.0 runs this, and your installed bit updates to it when the run ends.");

        var search = host.Runner.Calls[0];
        CollectionAssert.AreEqual(new[] { "package", "search", "Bit.Cli", "--exact-match", "--format", "json", "--prerelease" }, search.Arguments.ToArray());
        Assert.AreEqual(copies, search.WorkingDirectory, "The project's global.json and NuGet.config have no say in which bit runs.");

        var install = host.Runner.Calls[1];
        CollectionAssert.AreEqual(new[] { "tool", "install", "Bit.Cli", "--version", "1.8.0", "--tool-path", copy }, install.Arguments.ToArray());
        Assert.AreEqual(copies, install.WorkingDirectory);

        var handOff = host.Runner.Calls[2];
        Assert.AreEqual(Path.Combine(copy, executable), handOff.FileName);
        Assert.IsTrue(handOff.Interactive);
        Assert.IsTrue(handOff.TakesOver, "Ctrl+C is the other bit's to handle.");
        Assert.AreEqual(host.WorkingDirectory, handOff.WorkingDirectory);
        CollectionAssert.AreEqual(new[] { "migration", "add", "AddRating", "--offline" }, handOff.Arguments.ToArray());
        Assert.AreEqual("1.6.2", handOff.Environment![SelfUpdate.HandedOffVariable]);
        Assert.IsFalse(host.Runner.Calls.Skip(3).Any(c => c.Arguments is not ["--version"]), "The command itself is the other bit's to run.");

        var update = host.Runner.Detached.Single();
        Assert.AreEqual(updater, update.FileName);
        Assert.AreEqual(copies, update.WorkingDirectory);
        StringAssert.Contains(update.Arguments[^1], "tool update --global Bit.Cli --version 1.8.0");
        Assert.IsTrue(File.Exists(Path.Combine(copy, SelfUpdate.ProjectMarker)));
        Assert.IsTrue(File.Exists(Path.Combine(copy, executable)), "A copy that answers stays, whatever the command's exit code.");
    }

    [TestMethod]
    public async Task ABitTooNewForTheProject_Should_HandTheCommandToTheNewestTheProjectWorksWith()
    {
        using var host = new TestHost(HostOs.Linux);
        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.5.0");
        host.Runner.On("dotnet", "package search", 0, Search("1.3.0", "1.4.0", "1.4.2", "1.5.1", "1.6.2", "1.7.0"));
        var copy = InstallsCopy(host, "1.4.2", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("run", "android"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project works with bit up to 1.5.0, so bit 1.4.2 runs this.");
        CollectionAssert.AreEqual(new[] { "run", "android" }, host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, "bit")).Arguments.ToArray());
        Assert.IsEmpty(host.Runner.Detached, "The installed bit is newer than the one this project takes, so it stays.");
    }

    [TestMethod]
    [DataRow("1.4.0", "1.5.0", "1.4.9")]
    [DataRow("1.5.1", "1.6.0", "1.5.3")]
    public async Task ABitTooNewForTheTemplateAProjectIsOn_Should_HandTheCommandToTheNewestThatStillWorksWithIt(string template, string tooOldFor, string target)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, template, null);
        host.Runner.On("dotnet", "package search", 0, Search("1.3.0", "1.4.9", "1.5.0-pre-01", "1.5.0", "1.5.3", "1.6.0-pre-01", "1.6.0", "1.6.2", "1.7.0"));
        var copy = InstallsCopy(host, target, "bit");
        RunsCopy(host, 3);

        Assert.AreEqual(3, await host.RunAsync("run", "android"), host.Output);

        StringAssert.Contains(host.FlatOutput, $"This project is on bit Boilerplate {template}, too old for bit {tooOldFor} and newer, so bit {target} runs this.");
        StringAssert.Contains(host.FlatOutput, $"This project is on bit Boilerplate {template}, and 1.6.2 is out. bit diff shows what changed.");
        CollectionAssert.AreEqual(new[] { "run", "android" }, host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, "bit") && c.Arguments is not ["--version"]).Arguments.ToArray());
        Assert.IsEmpty(host.Runner.Detached, "The installed bit is the one newer projects take, so it stays.");
    }

    [TestMethod]
    public async Task TheNewestBitAnOlderProjectWorksWith_Should_RunTheCommandItself()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, "1.4.9", oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, "1.4.0", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.4.9", "1.5.0", "1.6.2"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        Assert.IsFalse(host.Output.Contains("runs this", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Output.Contains("too old", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]));
    }

    [TestMethod]
    [DataRow("1.6.0", null, true)]
    [DataRow("1.6.2", null, false)]
    [DataRow("1.6.0", "GITHUB_ACTIONS", false)]
    [DataRow("1.6.0", SelfUpdate.OptOutVariable, true)]
    public async Task AProjectOnAnOlderTemplateThanBit_Should_BeToldThatANewerOneIsOut(string template, string? variable, bool told)
    {
        using var host = new TestHost(HostOs.Linux, variable is null ? null : new Dictionary<string, string> { [variable] = "true" });
        Official(host);
        WriteBitJson(host.WorkingDirectory, template, null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        Assert.AreEqual(told, host.FlatOutput.Contains($"This project is on bit Boilerplate {template}, and 1.6.2 is out. bit diff shows what changed.", StringComparison.Ordinal), host.Output);
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
    }

    [TestMethod]
    public async Task NoOlderBitToBeFound_Should_LeaveTheCommandToThisOne()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, "1.4.0", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.5.0", "1.6.2"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project is on bit Boilerplate 1.4.0, too old for bit 1.5.0 and newer, and bit couldn't find an older one, so bit 1.6.2 goes on.");
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
    }

    [TestMethod]
    public async Task WhereBitRunsTheVersionItWasGiven_Should_OnlySayTheProjectIsTooOldForIt()
    {
        using var host = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        Official(host, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, "1.4.0", null);
        WriteCopy(host, "1.4.9", "bit");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project is on bit Boilerplate 1.4.0, too old for bit 1.5.0 and newer, and this is bit 1.6.2.");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    [DataRow("1.4.0", null)]
    [DataRow("1.6.0", "1.6.1")]
    public async Task BitDiff_Should_NeverBeHandedToAnOlderBit(string template, string? max)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, template, max);
        WriteCopy(host, "1.4.9", "bit");
        WriteCopy(host, "1.6.1", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("diff", "--yes"), host.Output);

        StringAssert.Contains(host.FlatOutput, "'--yes' isn't a bit Boilerplate version", "It upgrades the project to the version of the bit that runs it, so an older bit would stop at its own.");
        Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
        Assert.IsTrue(host.Runner.Calls.All(c => c.Arguments is ["package", "search", ..]), "It only looks whether a newer bit is out.");

        Official(host, oldestProjects: oldestProjects);
        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project", "Every other command of that project is the older bit's.");
    }

    [TestMethod]
    [DataRow("clean", "1.4.2")]
    [DataRow("diff", "1.6.2")]
    public async Task BitDiffInAProjectThatNeedsANewerBit_Should_GoToTheNewestOne(string command, string target)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, "1.3.0", oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.4.5");
        host.Runner.On("dotnet", "package search", 0, Search("1.3.0", "1.4.2", "1.4.9", "1.6.2"));
        var copy = InstallsCopy(host, target, "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync(command), host.Output);

        StringAssert.Contains(host.FlatOutput, $"This project needs bit 1.4.0 or newer, so bit {target} runs this");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName, host.Output);
        CollectionAssert.AreEqual(new[] { command }, host.Runner.Calls[^1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task BitDiff_Should_LookForTheNewestBitEveryTime()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.4.5");
        host.Runner.On("dotnet", "package search", 0, Search("1.4.2", "1.6.2"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("diff", "--yes"), host.Output);

        host.Runner.On("dotnet", "package search", 0, Search("1.4.2", "1.6.2", "1.7.0"));
        var copy = InstallsCopy(host, "1.7.0", "bit");
        RunsCopy(host, 0);
        Official(host, oldestProjects: oldestProjects);

        Assert.AreEqual(0, await host.RunAsync("diff", "--yes"), host.Output);

        StringAssert.Contains(host.FlatOutput, "bit 1.7.0 is out, so it runs this, and your installed bit updates to it when the run ends.");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName);
        CollectionAssert.AreEqual(new[] { "diff", "--yes" }, host.Runner.Calls[^1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task ABitThatFitsTheProject_Should_RunTheCommandItselfAndLookForANewerOneOnceADay()
    {
        using var host = new TestHost(HostOs.Linux);
        var copies = Official(host).CopiesDirectory;
        WriteBitJson(host.WorkingDirectory, "1.6.0", "1.6.2");
        host.Runner.On("dotnet", "package search", 0, Search("1.6.0", "1.6.2", "1.6.3-pre-01", "1.7.0"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
        Assert.IsFalse(host.Output.Contains("runs this", StringComparison.Ordinal), host.Output);

        var search = host.Runner.Calls.Single();
        CollectionAssert.AreEqual(new[] { "package", "search", "Bit.Cli", "--exact-match", "--format", "json" }, search.Arguments.ToArray(), "A stable bit looks among the stable ones.");
        Assert.AreEqual(copies, search.WorkingDirectory);

        Official(host);
        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        Assert.HasCount(1, host.Runner.Calls, "It looked today, so the next command doesn't wait for it.");

        foreach (var looked in Directory.GetFiles(copies, ".newest-*"))
        {
            File.SetLastWriteTimeUtc(looked, DateTime.UtcNow - SelfUpdate.NewerBitPause - TimeSpan.FromMinutes(1));
        }

        Official(host);
        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        Assert.HasCount(2, host.Runner.Calls, "A day later it looks again.");
    }

    [TestMethod]
    [DataRow("1.6.2", "1.6.0", null, "1.7.0")]
    [DataRow("1.4.2", "1.4.0", null, "1.4.9")]
    [DataRow("1.6.2", "1.6.0", "1.6.5", "1.6.4")]
    [DataRow("1.7.0-pre-01", "1.6.0", null, "1.8.0-pre-01")]
    public async Task ANewerBitTheProjectWorksWith_Should_RunTheCommandAndReplaceTheInstalledOne(string version, string template, string? max, string target)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, version, oldestProjects: oldestProjects);
        WriteBitJson(host.WorkingDirectory, template, max);
        host.Runner.On("dotnet", "package search", 0, Search("1.4.2", "1.4.9", "1.5.0", "1.6.2", "1.6.4", "1.7.0", "1.8.0-pre-01"));
        var copy = InstallsCopy(host, target, "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, $"bit {target} is out, so it runs this, and your installed bit updates to it when the run ends.");
        StringAssert.Contains(host.FlatOutput, $"This project is on bit Boilerplate {template}, and {target} is out. bit diff shows what changed.");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName);
        CollectionAssert.AreEqual(new[] { "clean" }, host.Runner.Calls[^1].Arguments.ToArray());
        StringAssert.Contains(host.Runner.Detached.Single().Arguments[^1], $"tool update --global Bit.Cli --version {target}");
    }

    [TestMethod]
    public async Task ANewerCopyThatIsAlreadyHere_Should_RunTheCommand()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.6.0", null);
        WriteCopy(host, "1.6.9-pre-01", "bit");
        var copy = WriteCopy(host, "1.6.5", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName, "A stable bit doesn't move to a prerelease.");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]));
    }

    [TestMethod]
    public async Task WhereBitRunsTheVersionItWasGiven_Should_NeverLookForANewerOne()
    {
        await AssertStaysAsync(h => Official(h), new() { ["GITHUB_ACTIONS"] = "true" });
        await AssertStaysAsync(h => Official(h), new() { [SelfUpdate.OptOutVariable] = "1" });
        await AssertStaysAsync(h => Official(h, install: "dnx"));
        await AssertStaysAsync(h => Official(h, install: "tool-path"));

        static async Task AssertStaysAsync(Action<TestHost> update, Dictionary<string, string>? variables = null)
        {
            using var host = new TestHost(HostOs.Linux, variables);
            update(host);
            WriteBitJson(host.WorkingDirectory, "1.6.0", null);
            WriteCopy(host, "1.7.0", "bit");

            Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

            Assert.IsFalse(host.Output.Contains("runs this", StringComparison.Ordinal), host.Output);
            Assert.IsEmpty(host.Runner.Calls);
        }
    }

    [TestMethod]
    public async Task ACopyThatFits_Should_RunTheCommandWithoutLookingAgainThatDay()
    {
        using var host = new TestHost(HostOs.Linux);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        var older = WriteCopy(host, "1.6.9", "bit");
        var copy = WriteCopy(host, "1.7.1", "bit");
        host.Runner.On("dotnet", "package search", 0, Search("1.6.9", "1.7.1"));
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0 or newer, so bit 1.7.1 runs this, and your installed bit updates to it when the run ends.");
        Assert.HasCount(2, host.Runner.Calls, "It looks once whether a newer one is out.");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName);
        StringAssert.Contains(host.Runner.Detached.Single().Arguments[^1], "tool update --global Bit.Cli --version 1.7.1");
        Assert.IsTrue(File.Exists(Path.Combine(copy, SelfUpdate.ProjectMarker)));
        Assert.IsFalse(File.Exists(Path.Combine(older, SelfUpdate.ProjectMarker)));

        Official(host);
        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.HasCount(3, host.Runner.Calls, "It looked today, so the copy runs the next command right away.");
        Assert.HasCount(1, host.Runner.Detached, "The update that was just started isn't started again by the next command.");
        StringAssert.Contains(host.FlatOutput, "so bit 1.7.1 runs this. ");

        File.SetLastWriteTimeUtc(Path.Combine(copy, SelfUpdate.UpdateMarker), DateTime.UtcNow - SelfUpdate.UpdatePause - TimeSpan.FromMinutes(1));
        Official(host);
        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.HasCount(2, host.Runner.Detached, "An update that didn't get through is tried again later.");
    }

    [TestMethod]
    public async Task AStableBit_Should_TakeAPrereleaseOnlyWhileNothingElseFits()
    {
        using var host = new TestHost(HostOs.Linux);
        WriteBitJson(host.WorkingDirectory, "1.7.0-pre-02", "1.8.0");
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-01", "1.7.0-pre-03", "1.9.0"));
        var prerelease = InstallsCopy(host, "1.7.0-pre-03", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0-pre-02 or newer, so bit 1.7.0-pre-03 runs this.");
        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName == Path.Combine(prerelease, "bit")));
        Assert.IsEmpty(host.Runner.Detached, "A stable bit isn't replaced by a prerelease because of one project.");

        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-03", "1.7.0", "1.8.0-pre-01"));
        var stable = InstallsCopy(host, "1.7.0", "bit");
        Official(host);
        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.HasCount(1, host.Runner.Calls.Where(c => c.Arguments is ["package", "search", ..]).ToList(), "It doesn't look again right away.");
        Assert.AreEqual(Path.Combine(prerelease, "bit"), host.Runner.Calls[^1].FileName);

        foreach (var lookedUp in Directory.GetFiles(Official(host).CopiesDirectory, ".newest-*"))
        {
            File.SetLastWriteTimeUtc(lookedUp, DateTime.UtcNow - SelfUpdate.NewerBitPause - TimeSpan.FromMinutes(1));
        }

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.AreEqual(Path.Combine(stable, "bit"), host.Runner.Calls[^1].FileName, "Once a stable bit that fits is out, it takes over.");
        StringAssert.Contains(host.Runner.Detached.Single().Arguments[^1], "tool update --global Bit.Cli --version 1.7.0");
    }

    [TestMethod]
    public async Task APrereleaseBit_Should_TakeTheNewestThatFits()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host, "1.7.0-pre-01");
        WriteBitJson(host.WorkingDirectory, "1.7.0-pre-02", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-03", "1.7.0", "1.8.0-pre-01"));
        var copy = InstallsCopy(host, "1.8.0-pre-01", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName == Path.Combine(copy, "bit")), host.Output);
        StringAssert.Contains(host.Runner.Detached.Single().Arguments[^1], "tool update --global Bit.Cli --version 1.8.0-pre-01");
    }

    [TestMethod]
    public async Task WhereBitRunsTheVersionItWasGiven_Should_OnlySayTheProjectNeedsAnother()
    {
        await AssertSaysAndStaysAsync(h => Official(h), new() { ["GITHUB_ACTIONS"] = "true" });
        await AssertSaysAndStaysAsync(h => Official(h), new() { [SelfUpdate.OptOutVariable] = "1" });
        await AssertSaysAndStaysAsync(h => Official(h, install: "dnx"));
        await AssertSaysAndStaysAsync(h => Official(h, install: "tool-path"));

        static async Task AssertSaysAndStaysAsync(Action<TestHost> update, Dictionary<string, string>? variables = null)
        {
            using var host = new TestHost(HostOs.Linux, variables);
            update(host);
            WriteBitJson(host.WorkingDirectory, "1.7.0", null);
            WriteCopy(host, "1.7.0", "bit");

            Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

            StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0 or newer, and this is bit 1.6.2.");
            StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
            Assert.IsEmpty(host.Runner.Calls);
        }
    }

    [TestMethod]
    public async Task ABuildFromSourceAndABitHandedTheCommand_Should_NeverCheckTheProject()
    {
        await AssertSilentAsync(h => h.Services.SelfUpdate = new SelfUpdate(h.Services) { CurrentVersion = "1.6.2", IsOfficialBuild = false, InstallMethod = "tool" });
        await AssertSilentAsync(h => Official(h), new() { [SelfUpdate.HandedOffVariable] = "1.6.1" });

        static async Task AssertSilentAsync(Action<TestHost> update, Dictionary<string, string>? variables = null)
        {
            using var host = new TestHost(HostOs.Linux, variables);
            update(host);
            WriteBitJson(host.WorkingDirectory, "1.7.0", null);
            WriteCopy(host, "1.7.0", "bit");

            Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

            Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
            Assert.IsEmpty(host.Runner.Calls);
        }
    }

    [TestMethod]
    public async Task ABitThatCantBeFoundOrDownloaded_Should_LeaveTheCommandToThisOne()
    {
        using var offline = new TestHost(HostOs.Linux);
        Official(offline);
        WriteBitJson(offline.WorkingDirectory, "1.7.0", null);
        offline.Runner.On("dotnet", "package search", 1, "error: Unable to load the service index for source https://api.nuget.org/v3/index.json.");

        Assert.AreEqual(CliApp.ExitUsage, await offline.RunAsync("run"), offline.Output);

        StringAssert.Contains(offline.FlatOutput, "This project needs bit 1.7.0 or newer, and bit couldn't find one, so bit 1.6.2 goes on.");
        StringAssert.Contains(offline.FlatOutput, "isn't a bit Boilerplate project folder");

        Official(offline);
        Assert.AreEqual(CliApp.ExitUsage, await offline.RunAsync("run"), offline.Output);

        Assert.HasCount(1, offline.Runner.Calls, "The next command doesn't wait for a lookup that just found nothing.");

        using var notOut = new TestHost(HostOs.Linux);
        Official(notOut);
        WriteBitJson(notOut.WorkingDirectory, "1.7.0", null);
        notOut.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.6.3"));

        Assert.AreEqual(CliApp.ExitUsage, await notOut.RunAsync("run"), notOut.Output);

        StringAssert.Contains(notOut.FlatOutput, "and bit couldn't find one, so bit 1.6.2 goes on.");
        Assert.IsFalse(notOut.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]));

        using var failing = new TestHost(HostOs.Linux);
        Official(failing);
        WriteBitJson(failing.WorkingDirectory, "1.7.0", null);
        failing.Runner.On("dotnet", "package search", 0, Search("1.7.0"));
        failing.Runner.On("dotnet", "tool install", 1, "error NU1101: Unable to find package Bit.Cli.");

        Assert.AreEqual(CliApp.ExitUsage, await failing.RunAsync("run"), failing.Output);

        StringAssert.Contains(failing.FlatOutput, "Couldn't get bit 1.7.0, so bit 1.6.2 goes on.");
        StringAssert.Contains(failing.FlatOutput, "isn't a bit Boilerplate project folder");
        Assert.IsEmpty(failing.Runner.Detached);
        Assert.IsFalse(Directory.Exists(Path.Combine(failing.Environment.BitDirectory, "cli", "1.7.0")));

        Official(failing);
        Assert.AreEqual(CliApp.ExitUsage, await failing.RunAsync("run"), failing.Output);

        Assert.HasCount(2, failing.Runner.Calls, "A download that just failed isn't tried again by the next command.");
    }

    [TestMethod]
    public async Task ACopyThatCantStart_Should_LeaveTheCommandToThisOne()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        host.Runner.NotFound("bit");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Couldn't start bit 1.7.0, so bit 1.6.2 goes on.");
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
    }

    [TestMethod]
    public async Task ABrokenCopy_Should_BeRemovedSoTheNextCommandGetsAWholeOne()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        var copy = WriteCopy(host, "1.7.0", "bit");
        host.Runner.On("bit", "", 131, "The application to execute does not exist");

        Assert.AreEqual(131, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, "The copy of bit 1.7.0 that ran this is broken, so bit removed it. Run the command again.");
        Assert.IsFalse(Directory.Exists(copy));
        CollectionAssert.AreEqual(new[] { "--version" }, host.Runner.Calls[^1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task ACommandStoppedBeforeTheOtherBitStarts_Should_NotStartIt()
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        using var stopped = new CancellationTokenSource();
        stopped.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => host.Services.SelfUpdate.HandOffToProjectBitAsync(host.WorkingDirectory, ["clean"], stopped.Token));

        Assert.IsFalse(host.Runner.Calls.Any(c => Path.GetFileNameWithoutExtension(c.FileName) is "bit"));
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    [DataRow("telemetry")]
    [DataRow("about")]
    [DataRow("update")]
    [DataRow("new Contoso --dry-run --yes --no-update")]
    [DataRow("new --not-an-option")]
    [DataRow("remove nothing-here --yes")]
    [DataRow("decode nothing.map nothing.txt")]
    [DataRow("trust nothing-here")]
    [DataRow("run --help")]
    [DataRow("frobnicate --help")]
    [DataRow("--version")]
    [DataRow("")]
    public async Task ACommandThatDoesntReadTheProject_Should_RunWithAnyBit(string command)
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2"));

        await host.RunAsync(command.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => Path.GetFileNameWithoutExtension(c.FileName) is "bit"), host.Output);
    }

    [TestMethod]
    [DataRow("frobnicate --fast")]
    [DataRow("run --a-new-option=--yes -- android")]
    [DataRow("migration squash")]
    public async Task ACommandOnlyTheOtherBitKnows_Should_ReachItAsItWasTyped(string command)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        var copy = WriteCopy(host, "1.7.0", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync(command.Split(' ')), host.Output);

        var handOff = host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, "bit"));
        Assert.AreEqual(Path.Combine(copy, "bit"), handOff.FileName);
        CollectionAssert.AreEqual(command.Split(' '), handOff.Arguments.ToArray());
    }

    [TestMethod]
    [DataRow("clean {0}")]
    [DataRow("rename Fabrikam {0}")]
    [DataRow("diff --project {0}")]
    [DataRow("setup {0} --platforms web,android -p:Configuration=Release --yes")]
    [DataRow("setup {0} --an-option-only-the-other-bit-knows")]
    [DataRow("clean {0} --an-option-only-the-other-bit-knows")]
    [DataRow("run --project {0}")]
    [DataRow("doctor --project {0}")]
    [DataRow("migration add Initial --project {0}")]
    [DataRow("api check --version 2.0.0 --project {0}")]
    [DataRow("keys android --project {0}")]
    [DataRow("pipeline --check --project {0}")]
    public async Task ACommandGivenAnotherFolder_Should_GoByThatProject(string command)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        var project = Path.Combine(host.Root, "Contoso");
        WriteProject(project, "1.7.0");
        var copy = WriteCopy(host, "1.7.0", "bit");
        RunsCopy(host, 0);
        var arguments = string.Format(command, project).Split(' ');

        Assert.AreEqual(0, await host.RunAsync(arguments), host.Output);

        var handOff = host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, "bit"));
        Assert.AreEqual(Path.Combine(copy, "bit"), handOff.FileName);
        Assert.AreEqual(host.WorkingDirectory, handOff.WorkingDirectory);
        CollectionAssert.AreEqual(arguments, handOff.Arguments.ToArray());
    }

    [TestMethod]
    [DataRow("clean {0}", CliApp.ExitUsage)]
    [DataRow("api check --version 2.0.0 --project {0}", CliApp.ExitUsage)]
    [DataRow("clean {0} --not-an-option", CliApp.ExitFailed)]
    [DataRow("setup {0} --not-an-option", CliApp.ExitFailed)]
    [DataRow("setup https://github.com/contoso/Contoso --not-an-option", CliApp.ExitFailed)]
    [DataRow("setup {0} --yes --no-tools --no-workloads --no-restore --no-build --no-browsers --no-run --no-trust --no-open", CliApp.ExitOk)]
    public async Task ACommandGivenAnotherFolder_Should_LeaveTheProjectItRunsInOutOfIt(string command, int exitCode)
    {
        using var host = new TestHost(HostOs.Linux);
        Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        var project = Path.Combine(host.Root, "Contoso");
        WriteProject(project, "1.6.0");
        File.WriteAllText(Path.Combine(project, "bit.json"), """{ "cli": { "maxVersion": "1.6.2" }, "name": "Contoso" }""");

        Assert.AreEqual(exitCode, await host.RunAsync(string.Format(command, project).Split(' ')), host.Output);

        Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => Path.GetFileNameWithoutExtension(c.FileName) is "bit"), host.Output);
    }

    [TestMethod]
    public async Task SetupWithARepositoryAddress_Should_GoByTheProjectOnceItIsCloned()
    {
        var prompter = new ScriptedPrompter().On("Set up native apps", new[] { "Android" });
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.Executables["git"] = "git";
        Official(host);
        host.Runner.On("git", "clone", spec =>
        {
            WriteProject(spec.Arguments[^1], "1.7.0");
            return new ProcessResult { ExitCode = 0 };
        });
        var copy = WriteCopy(host, "1.7.0", "bit");
        RunsCopy(host, 0);

        Assert.AreEqual(0, await host.RunAsync("setup", "https://github.com/contoso/Contoso", "--no-tools"), host.Output);

        var calls = host.Runner.Calls.Select(c => c.FileName is "git" ? c.Arguments[0] : Path.GetFileName(c.FileName)).ToList();
        Assert.AreEqual("bit", calls[^1], string.Join(' ', calls));
        CollectionAssert.Contains(calls, "clone");
        Assert.IsNotNull(prompter.Find("Set up native apps"));

        var handOff = host.Runner.Calls[^1];
        Assert.AreEqual(Path.Combine(copy, "bit"), handOff.FileName);
        CollectionAssert.AreEqual(new[] { "setup", Path.Combine(host.WorkingDirectory, "Contoso"), "--no-tools", "--platforms", "web,android" }, handOff.Arguments.ToArray(),
            "The other bit gets the folder that is cloned by now, and doesn't ask again what this one asked.");
    }

    [TestMethod]
    public async Task BitNew_Should_KeepTheCopiesProjectsUsedLately()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        var copies = Official(host).CopiesDirectory;
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2"));
        var used = WriteCopy(host, "1.5.0", "bit");
        var forgotten = WriteCopy(host, "1.4.0", "bit");
        var unused = WriteCopy(host, "1.3.0", "bit");
        var newer = WriteCopy(host, "1.7.0", "bit");
        File.WriteAllText(Path.Combine(used, SelfUpdate.ProjectMarker), "");
        File.WriteAllText(Path.Combine(forgotten, SelfUpdate.ProjectMarker), "");
        File.SetLastWriteTimeUtc(Path.Combine(forgotten, SelfUpdate.ProjectMarker), DateTime.UtcNow - SelfUpdate.ProjectCopyLifetime - TimeSpan.FromHours(1));

        using (var busy = await SelfUpdate.LockAsync(Path.Combine(copies, ".lock"), TimeSpan.Zero, CancellationToken.None))
        {
            Assert.IsNotNull(busy);
            Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);
            Assert.HasCount(4, Directory.GetDirectories(copies), "While another bit gets a copy, nothing is cleared.");
        }

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);

        Assert.IsTrue(Directory.Exists(used));
        Assert.IsTrue(Directory.Exists(newer));
        Assert.IsFalse(Directory.Exists(forgotten));
        Assert.IsFalse(Directory.Exists(unused));
        CollectionAssert.AreEquivalent(new[] { "1.5.0", "1.7.0" }, Directory.GetDirectories(copies).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public async Task TwoBitsGettingACopy_Should_TakeTurns()
    {
        using var host = new TestHost();
        var file = Path.Combine(host.Root, "copies", ".lock");

        var first = await SelfUpdate.LockAsync(file, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.IsNotNull(first);

        var second = SelfUpdate.LockAsync(file, TimeSpan.FromSeconds(30), CancellationToken.None);
        await Task.Delay(600);
        Assert.IsFalse(second.IsCompleted, "The second one waits while the first holds it.");

        first.Dispose();

        using var held = await second.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.IsNotNull(held);

        Assert.IsNull(await SelfUpdate.LockAsync(file, TimeSpan.FromMilliseconds(300), CancellationToken.None), "It gives up when its patience runs out.");

        File.WriteAllText(Path.Combine(host.Root, "a-file"), "");
        Assert.IsNull(await SelfUpdate.LockAsync(Path.Combine(host.Root, "a-file", ".lock"), TimeSpan.FromSeconds(30), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)), "A folder that can't be made isn't waited for.");
    }

    [TestMethod]
    public async Task ACopyAnotherBitIsStillGetting_Should_NotBeGotTwice()
    {
        using var host = new TestHost(HostOs.Linux);
        var copies = Official(host).CopiesDirectory;
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.7.0"));
        RunsCopy(host, 0);

        using var other = await SelfUpdate.LockAsync(Path.Combine(copies, ".lock"), TimeSpan.Zero, CancellationToken.None);
        var run = host.RunAsync("clean");
        await Task.Delay(800);
        Assert.IsFalse(run.IsCompleted, "It waits for the bit that is getting the copy.");

        var copy = WriteCopy(host, "1.7.0", "bit");
        other!.Dispose();

        Assert.AreEqual(0, await run.WaitAsync(TimeSpan.FromSeconds(20)), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]), "The other bit's copy is the one to use.");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls[^1].FileName);
    }

    private static SelfUpdate Official(TestHost host, string version = "1.6.2", string install = "tool", IReadOnlyList<OldestProject>? oldestProjects = null)
    {
        return host.Services.SelfUpdate = new SelfUpdate(host.Services) { CurrentVersion = version, IsOfficialBuild = true, InstallMethod = install, OldestProjects = oldestProjects ?? [] };
    }

    private static void RunsCopy(TestHost host, int exitCode)
    {
        if (host.Services.SelfUpdate.IsOfficialBuild is false)
        {
            Official(host);
        }

        host.Runner.On("bit", "", exitCode);
        host.Runner.On("bit", "--version", 0);
    }

    private static void WriteBitJson(string directory, string? version, string? max)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "bit.json"), JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["template"] = "bit-bp",
            ["version"] = version,
            ["cli"] = max is null ? null : new Dictionary<string, string> { ["maxVersion"] = max },
            ["name"] = "Contoso"
        }.Where(v => v.Value is not null).ToDictionary()));
    }

    private static void WriteProject(string directory, string version)
    {
        WriteBitJson(directory, version, null);
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
    }

    private static string WriteCopy(TestHost host, string version, string executable)
    {
        var copy = Directory.CreateDirectory(Path.Combine(host.Environment.BitDirectory, "cli", version)).FullName;
        File.WriteAllText(Path.Combine(copy, executable), "");
        return copy;
    }

    private static string InstallsCopy(TestHost host, string version, string executable)
    {
        var copy = Path.Combine(host.Environment.BitDirectory, "cli", version);

        host.Runner.On("dotnet", $"tool install Bit.Cli --version {version} ", _ =>
        {
            WriteCopy(host, version, executable);
            return new ProcessResult { ExitCode = 0 };
        });

        return copy;
    }

    private static string Search(params string[] versions)
    {
        return JsonSerializer.Serialize(new
        {
            version = 2,
            problems = Array.Empty<object>(),
            searchResult = new[] { new { sourceName = "nuget.org", packages = versions.Select(v => new { id = "Bit.Cli", version = v }) } }
        });
    }
}
