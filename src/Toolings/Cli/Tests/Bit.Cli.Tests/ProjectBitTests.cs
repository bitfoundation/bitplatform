using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ProjectBitTests
{
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

        var project = ProjectBit.Find(host.WorkingDirectory);

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

        Assert.AreEqual(Path.Combine(host.Root, "bit.json"), ProjectBit.Find(inner)!.FilePath);

        WriteBitJson(host.WorkingDirectory, "1.9.0", null);

        Assert.AreEqual("1.9.0", ProjectBit.Find(inner)!.MinVersion);

        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """{ "version": "1.9.0", "name": "Contoso" }""");

        Assert.IsNull(ProjectBit.Find(inner), "A project that names no bit versions takes any bit, whatever a folder above it says.");
    }

    [TestMethod]
    [DataRow("""{ "cli": { "minVersion": "the newest" } }""")]
    [DataRow("""{ "cli": { "minVersion": 2 } }""")]
    [DataRow("""{ "cli": "1.7.0" }""")]
    [DataRow("""{ "cli": { "minVersion": "1.7.0", "maxVersion": "1.5.0" } }""")]
    [DataRow("""{ "cli": { "minVersion": "1.7.0" """)]
    [DataRow("")]
    public void ABitJsonThatCantBeRead_Should_TakeAnyBit(string json)
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), json);

        Assert.IsNull(ProjectBit.Find(host.WorkingDirectory));
    }

    [TestMethod]
    public void ABoundThatCantBeRead_Should_LeaveTheOtherOne()
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """{ "cli": { "minVersion": " 1.7.0 ", "maxVersion": "newest" } }""");

        var project = ProjectBit.Find(host.WorkingDirectory)!;

        Assert.AreEqual("1.7.0", project.MinVersion);
        Assert.IsNull(project.MaxVersion);
    }

    [TestMethod]
    public void TheTemplatesBitJson_Should_NameTheOldestBitItWorksWith()
    {
        var template = Path.GetDirectoryName(TestHost.RepositoryFile("src", "Templates", "Boilerplate", "Bit.Boilerplate", "bit.json"))!;

        var project = ProjectBit.Find(template);

        Assert.IsNotNull(project?.MinVersion, "bit.json of bit Boilerplate needs cli.minVersion, the oldest bit CLI that understands the template.");
        Assert.IsNull(project.MaxVersion, "The template works with every newer bit.");
    }

    [TestMethod]
    [DataRow(HostOs.Linux, "bit", "/bin/sh")]
    [DataRow(HostOs.Windows, "bit.exe", "powershell.exe")]
    public async Task ABitTooOldForTheProject_Should_HandTheCommandToTheNewestAndUpdateItself(HostOs os, string executable, string updater)
    {
        using var host = new TestHost(os);
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.Root, "1.7.0", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0", "1.8.0", "1.9.0-pre-01"));
        var copy = InstallsCopy(host, "1.8.0", executable);
        host.Runner.On("bit", "", 7);

        Assert.AreEqual(7, await host.RunAsync("migration", "add", "AddRating", "--offline"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0 or newer, so bit 1.8.0 runs this, and your installed bit updates to it when the run ends.");
        CollectionAssert.AreEqual(new[] { "package", "search", "Bit.Cli", "--exact-match", "--format", "json" }, host.Runner.Calls[0].Arguments.ToArray());
        CollectionAssert.AreEqual(new[] { "tool", "install", "Bit.Cli", "--version", "1.8.0", "--tool-path", copy }, host.Runner.Calls[1].Arguments.ToArray());

        var handOff = host.Runner.Calls[2];
        Assert.AreEqual(Path.Combine(copy, executable), handOff.FileName);
        Assert.IsTrue(handOff.Interactive);
        Assert.AreEqual(host.WorkingDirectory, handOff.WorkingDirectory);
        CollectionAssert.AreEqual(new[] { "migration", "add", "AddRating", "--offline" }, handOff.Arguments.ToArray());
        Assert.AreEqual("1.6.2", handOff.Environment![SelfUpdate.HandedOffVariable]);
        Assert.AreEqual(3, host.Runner.Calls.Count, "The command itself is the other bit's to run.");

        var update = host.Runner.Detached.Single();
        Assert.AreEqual(updater, update.FileName);
        StringAssert.Contains(update.Arguments[^1], "tool update --global Bit.Cli --version 1.8.0");
        Assert.IsTrue(File.Exists(Path.Combine(copy, SelfUpdate.ProjectMarker)));
    }

    [TestMethod]
    public async Task ABitTooNewForTheProject_Should_HandTheCommandToTheNewestTheProjectWorksWith()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.WorkingDirectory, "1.4.0", "1.5.0");
        host.Runner.On("dotnet", "package search", 0, Search("1.3.0", "1.4.0", "1.4.2", "1.5.1", "1.6.2", "1.7.0"));
        var copy = InstallsCopy(host, "1.4.2", "bit");
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("run", "android"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project works with bit up to 1.5.0, so bit 1.4.2 runs this.");
        CollectionAssert.AreEqual(new[] { "run", "android" }, host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, "bit")).Arguments.ToArray());
        Assert.IsEmpty(host.Runner.Detached, "The installed bit is newer than the one this project takes, so it stays.");
    }

    [TestMethod]
    public async Task ABitThatFitsTheProject_Should_RunTheCommandItself()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.WorkingDirectory, "1.6.0", "1.6.2");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run"), host.Output);

        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");
        Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task ACopyThatFits_Should_RunTheCommandWithoutLookingForANewerOne()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        var older = WriteCopy(host, "1.6.9", "bit");
        var copy = WriteCopy(host, "1.7.1", "bit");
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0 or newer, so bit 1.7.1 runs this.");
        Assert.AreEqual(Path.Combine(copy, "bit"), host.Runner.Calls.Single().FileName);
        Assert.IsEmpty(host.Runner.Detached, "Only a copy bit has just got starts the installed bit's update.");
        Assert.IsTrue(File.Exists(Path.Combine(copy, SelfUpdate.ProjectMarker)));
        Assert.IsFalse(File.Exists(Path.Combine(older, SelfUpdate.ProjectMarker)));
    }

    [TestMethod]
    public async Task AStableBit_Should_TakeAPrereleaseOnlyWhenNothingElseFits()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0-pre-02", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-01", "1.7.0-pre-03"));
        var prerelease = InstallsCopy(host, "1.7.0-pre-03", "bit");
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("clean"), host.Output);

        CollectionAssert.Contains(host.Runner.Calls[0].Arguments.ToList(), "--prerelease");
        StringAssert.Contains(host.FlatOutput, "This project needs bit 1.7.0-pre-02 or newer, so bit 1.7.0-pre-03 runs this.");
        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName == Path.Combine(prerelease, "bit")));
        Assert.IsEmpty(host.Runner.Detached, "A stable bit isn't replaced by a prerelease because of one project.");

        using var released = new TestHost(HostOs.Linux);
        released.Services.SelfUpdate = Official(released);
        WriteBitJson(released.WorkingDirectory, "1.7.0-pre-02", null);
        released.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-03", "1.7.0", "1.8.0-pre-01"));
        var stable = InstallsCopy(released, "1.7.0", "bit");
        released.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await released.RunAsync("clean"), released.Output);

        Assert.IsTrue(released.Runner.Calls.Any(c => c.FileName == Path.Combine(stable, "bit")), released.Output);
        StringAssert.Contains(released.Runner.Detached.Single().Arguments[^1], "tool update --global Bit.Cli --version 1.7.0");
    }

    [TestMethod]
    public async Task APrereleaseBit_Should_TakeTheNewestThatFits()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host, "1.7.0-pre-01");
        WriteBitJson(host.WorkingDirectory, "1.7.0-pre-02", null);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.7.0-pre-03", "1.7.0", "1.8.0-pre-01"));
        var copy = InstallsCopy(host, "1.8.0-pre-01", "bit");
        host.Runner.On("bit", "", 0);

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

        static async Task AssertSaysAndStaysAsync(Func<TestHost, SelfUpdate> update, Dictionary<string, string>? variables = null)
        {
            using var host = new TestHost(HostOs.Linux, variables);
            host.Services.SelfUpdate = update(host);
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
        await AssertSilentAsync(h => new SelfUpdate(h.Services) { CurrentVersion = "1.6.2", IsOfficialBuild = false, InstallMethod = "tool" });
        await AssertSilentAsync(h => Official(h), new() { [SelfUpdate.HandedOffVariable] = "1.6.1" });

        static async Task AssertSilentAsync(Func<TestHost, SelfUpdate> update, Dictionary<string, string>? variables = null)
        {
            using var host = new TestHost(HostOs.Linux, variables);
            host.Services.SelfUpdate = update(host);
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
        offline.Services.SelfUpdate = Official(offline);
        WriteBitJson(offline.WorkingDirectory, "1.7.0", null);
        offline.Runner.On("dotnet", "package search", 1, "error: Unable to load the service index for source https://api.nuget.org/v3/index.json.");

        Assert.AreEqual(CliApp.ExitUsage, await offline.RunAsync("run"), offline.Output);

        StringAssert.Contains(offline.FlatOutput, "This project needs bit 1.7.0 or newer, and bit couldn't find one, so bit 1.6.2 goes on.");
        StringAssert.Contains(offline.FlatOutput, "isn't a bit Boilerplate project folder");

        using var notOut = new TestHost(HostOs.Linux);
        notOut.Services.SelfUpdate = Official(notOut);
        WriteBitJson(notOut.WorkingDirectory, "1.7.0", null);
        notOut.Runner.On("dotnet", "package search", 0, Search("1.6.2", "1.6.3"));

        Assert.AreEqual(CliApp.ExitUsage, await notOut.RunAsync("run"), notOut.Output);

        StringAssert.Contains(notOut.FlatOutput, "and bit couldn't find one, so bit 1.6.2 goes on.");
        Assert.IsFalse(notOut.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]));

        using var failing = new TestHost(HostOs.Linux);
        failing.Services.SelfUpdate = Official(failing);
        WriteBitJson(failing.WorkingDirectory, "1.7.0", null);
        failing.Runner.On("dotnet", "package search", 0, Search("1.7.0"));
        failing.Runner.On("dotnet", "tool install", 1, "error NU1101: Unable to find package Bit.Cli.");

        Assert.AreEqual(CliApp.ExitUsage, await failing.RunAsync("run"), failing.Output);

        StringAssert.Contains(failing.FlatOutput, "Couldn't get bit 1.7.0, so bit 1.6.2 goes on.");
        StringAssert.Contains(failing.FlatOutput, "isn't a bit Boilerplate project folder");
        Assert.IsEmpty(failing.Runner.Detached);
        Assert.IsFalse(Directory.Exists(Path.Combine(failing.Environment.BitDirectory, "cli", "1.7.0")));
    }

    [TestMethod]
    [DataRow("telemetry")]
    [DataRow("about")]
    [DataRow("update")]
    [DataRow("new Contoso --dry-run --yes --no-update")]
    [DataRow("remove nothing-here --yes")]
    [DataRow("decode nothing.map nothing.txt")]
    public async Task ACommandThatDoesntReadTheProject_Should_RunWithAnyBit(string command)
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2"));

        await host.RunAsync(command.Split(' '));

        Assert.IsFalse(host.Output.Contains("This project", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => Path.GetFileNameWithoutExtension(c.FileName) is "bit"), host.Output);
    }

    [TestMethod]
    public async Task SetupOfAnotherFolder_Should_GoByThatProject()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        var project = Path.Combine(host.Root, "Contoso");
        WriteProject(project, "1.7.0");
        var copy = WriteCopy(host, "1.7.0", "bit");
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("setup", project, "--platforms", "web,android", "-p:Configuration=Release", "--yes"), host.Output);

        var handOff = host.Runner.Calls.Single();
        Assert.AreEqual(Path.Combine(copy, "bit"), handOff.FileName);
        Assert.AreEqual(host.WorkingDirectory, handOff.WorkingDirectory);
        CollectionAssert.AreEqual(new[] { "setup", project, "--platforms", "web,android", "-p", "Configuration=Release", "--yes" }, handOff.Arguments.ToArray());
    }

    [TestMethod]
    public async Task SetupWithARepositoryAddress_Should_GoByTheProjectOnceItIsCloned()
    {
        var prompter = new ScriptedPrompter().On("Set up native apps", new[] { "Android" });
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Runner.Executables["git"] = "git";
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("git", "clone", spec =>
        {
            WriteProject(spec.Arguments[^1], "1.7.0");
            return new ProcessResult { ExitCode = 0 };
        });
        var copy = WriteCopy(host, "1.7.0", "bit");
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("setup", "https://github.com/contoso/Contoso", "--no-tools"), host.Output);

        var calls = host.Runner.Calls.Select(c => c.FileName is "git" ? c.Arguments[0] : Path.GetFileName(c.FileName)).ToList();
        Assert.AreEqual("bit", calls[^1], string.Join(' ', calls));
        CollectionAssert.Contains(calls, "clone");
        Assert.IsNotNull(prompter.Find("Set up native apps"));

        var handOff = host.Runner.Calls[^1];
        Assert.AreEqual(Path.Combine(copy, "bit"), handOff.FileName);
        CollectionAssert.AreEqual(new[] { "setup", "https://github.com/contoso/Contoso", "--no-tools", "--platforms", "web,android" }, handOff.Arguments.ToArray(), "The other bit doesn't ask again what this one asked.");
    }

    [TestMethod]
    public async Task ABitToldToStop_Should_WaitForTheOtherBitThenStopIt()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = new SelfUpdate(host.Services) { CurrentVersion = "1.6.2", IsOfficialBuild = true, InstallMethod = "tool", HandOffGrace = TimeSpan.FromMilliseconds(300) };
        WriteBitJson(host.WorkingDirectory, "1.7.0", null);
        WriteCopy(host, "1.7.0", "bit");
        var started = new TaskCompletionSource();
        var ends = new TaskCompletionSource<int>();
        host.Runner.OnAsync("bit", "", async (_, stopped) =>
        {
            started.TrySetResult();
            return new ProcessResult { ExitCode = await ends.Task.WaitAsync(stopped) };
        });

        using var ctrlC = new CancellationTokenSource();
        var handOff = host.Services.SelfUpdate.HandOffToProjectBitAsync(host.WorkingDirectory, ["clean"], ctrlC.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(20));
        ctrlC.Cancel();
        ends.SetResult(130);

        Assert.AreEqual(130, await handOff.WaitAsync(TimeSpan.FromSeconds(20)), "The other bit got the Ctrl+C too and ended by itself, so its exit code is the run's.");

        using var deaf = new TestHost(HostOs.Linux);
        deaf.Services.SelfUpdate = new SelfUpdate(deaf.Services) { CurrentVersion = "1.6.2", IsOfficialBuild = true, InstallMethod = "tool", HandOffGrace = TimeSpan.FromMilliseconds(300) };
        WriteBitJson(deaf.WorkingDirectory, "1.7.0", null);
        WriteCopy(deaf, "1.7.0", "bit");
        var running = new TaskCompletionSource();
        deaf.Runner.OnAsync("bit", "", async (_, stopped) =>
        {
            running.TrySetResult();
            await Task.Delay(Timeout.Infinite, stopped);
            return new ProcessResult { ExitCode = 0 };
        });

        using var terminated = new CancellationTokenSource();
        var orphan = deaf.Services.SelfUpdate.HandOffToProjectBitAsync(deaf.WorkingDirectory, ["clean"], terminated.Token);
        await running.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await Task.Delay(600);
        Assert.IsFalse(orphan.IsCompleted, "Nobody stopped it yet.");
        terminated.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => orphan.WaitAsync(TimeSpan.FromSeconds(20)));
    }

    [TestMethod]
    public async Task BitNew_Should_KeepTheCopiesProjectsUsedLately()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("dotnet", "package search", 0, Search("1.6.2"));
        var used = WriteCopy(host, "1.5.0", "bit");
        var forgotten = WriteCopy(host, "1.4.0", "bit");
        var unused = WriteCopy(host, "1.3.0", "bit");
        var newer = WriteCopy(host, "1.7.0", "bit");
        File.WriteAllText(Path.Combine(used, SelfUpdate.ProjectMarker), "");
        File.WriteAllText(Path.Combine(forgotten, SelfUpdate.ProjectMarker), "");
        File.SetLastWriteTimeUtc(Path.Combine(forgotten, SelfUpdate.ProjectMarker), DateTime.UtcNow - SelfUpdate.ProjectCopyLifetime - TimeSpan.FromHours(1));

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);

        Assert.IsTrue(Directory.Exists(used));
        Assert.IsTrue(Directory.Exists(newer));
        Assert.IsFalse(Directory.Exists(forgotten));
        Assert.IsFalse(Directory.Exists(unused));
        CollectionAssert.AreEquivalent(new[] { "1.5.0", "1.7.0" }, Directory.GetDirectories(Path.Combine(host.Environment.BitDirectory, "cli")).Select(Path.GetFileName).ToArray());
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

        Assert.IsNull(await SelfUpdate.LockAsync(file, TimeSpan.FromMilliseconds(300), CancellationToken.None), "It gives up when its patience runs out, and bit goes on without it.");

        File.WriteAllText(Path.Combine(host.Root, "a-file"), "");
        Assert.IsNull(await SelfUpdate.LockAsync(Path.Combine(host.Root, "a-file", ".lock"), TimeSpan.FromSeconds(30), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)), "A folder that can't be made isn't waited for.");
    }

    private static SelfUpdate Official(TestHost host, string version = "1.6.2", string install = "tool")
    {
        return new SelfUpdate(host.Services) { CurrentVersion = version, IsOfficialBuild = true, InstallMethod = install };
    }

    private static void WriteBitJson(string directory, string? min, string? max)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "bit.json"), JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["template"] = "bit-bp",
            ["version"] = "1.6.0",
            ["cli"] = new Dictionary<string, string?> { ["minVersion"] = min, ["maxVersion"] = max }.Where(v => v.Value is not null).ToDictionary(),
            ["name"] = "Contoso"
        }));
    }

    private static void WriteProject(string directory, string min)
    {
        WriteBitJson(directory, min, null);
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
