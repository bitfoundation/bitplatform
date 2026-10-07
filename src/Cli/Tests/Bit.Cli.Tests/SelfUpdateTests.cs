using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class SelfUpdateTests
{
    [TestMethod]
    [DataRow("10.7.0", "10.6.2", 1)]
    [DataRow("10.10.0", "10.9.3", 1)]
    [DataRow("10.7.0", "10.7.0-pre-01", 1)]
    [DataRow("10.7.0-pre-02", "10.7.0-pre-01", 1)]
    [DataRow("10.6.2", "10.6.2", 0)]
    [DataRow("10.6", "10.6.0", 0)]
    public void Versions_Should_CompareTheWayNuGetDoes(string left, string right, int expected)
    {
        Assert.AreEqual(expected, Math.Sign(SelfUpdate.CompareVersions(left, right)));
        Assert.AreEqual(-expected, Math.Sign(SelfUpdate.CompareVersions(right, left)));
    }

    [TestMethod]
    public void TheSearchOutput_Should_KeepOnlyBitCliVersionsThatLookLikeVersions()
    {
        var output = "Welcome to .NET!\n" + JsonSerializer.Serialize(new
        {
            searchResult = new object[]
            {
                new { sourceName = "nuget.org", packages = new object[] { new { id = "bit.cli", version = "10.7.0" }, new { id = "Bit.Cli", version = "10.7.0; rm -rf ~" }, new { id = "Bit.Boilerplate", version = "11.0.0" } } },
                new { sourceName = "offline" }
            }
        });

        CollectionAssert.AreEqual(new[] { "10.7.0" }, SelfUpdate.ParseVersions(output).ToArray());
        Assert.IsEmpty(SelfUpdate.ParseVersions("error: Unable to load the service index for source https://api.nuget.org/v3/index.json."));
    }

    [TestMethod]
    public async Task TheNewestVersion_Should_ComeFromEveryNuGetSourceAndMatchTheChannel()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.6.2", "10.7.0", "10.8.0-pre-01" }), ("mirror", new[] { "10.6.3" })));

        Assert.AreEqual("10.7.0", await Official(host).LatestVersionAsync(CancellationToken.None));
        CollectionAssert.AreEqual(new[] { "package", "search", "Bit.Cli", "--exact-match", "--format", "json" }, host.Runner.Calls.Single().Arguments.ToArray());

        Assert.AreEqual("10.8.0-pre-01", await Official(host, "10.7.0-pre-01").LatestVersionAsync(CancellationToken.None));
        CollectionAssert.Contains(host.Runner.Calls.Last().Arguments.ToList(), "--prerelease");
    }

    [TestMethod]
    [DataRow(HostOs.Linux, "bit", "/bin/sh")]
    [DataRow(HostOs.Windows, "bit.exe", "powershell.exe")]
    public async Task ANewerBit_Should_CreateTheProjectAndReplaceTheInstalledOneAfterwards(HostOs os, string executable, string updater)
    {
        using var host = new TestHost(os);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.6.2", "10.7.0" })));
        var copy = Path.Combine(host.Environment.BitDirectory, "cli", "10.7.0");
        host.Runner.On("dotnet", "tool install", _ =>
        {
            Directory.CreateDirectory(copy);
            File.WriteAllText(Path.Combine(copy, executable), "");
            return new ProcessResult { ExitCode = 0 };
        });
        host.Runner.On("bit", "", 7);

        Assert.AreEqual(7, await host.RunAsync("new", "Contoso", "--redis", "--yes"), host.Output);

        StringAssert.Contains(host.Output, "bit 10.7.0 is out: this run continues with it");
        CollectionAssert.AreEqual(new[] { "tool", "install", "Bit.Cli", "--version", "10.7.0", "--tool-path", copy }, host.Runner.Calls.Single(c => c.Arguments is ["tool", "install", ..]).Arguments.ToArray());

        var handOff = host.Runner.Calls.Single(c => c.FileName == Path.Combine(copy, executable));
        Assert.IsTrue(handOff.Interactive);
        CollectionAssert.AreEqual(new[] { "new", "Contoso", "--redis", "--yes" }, handOff.Arguments.ToArray());
        Assert.AreEqual("10.6.2", handOff.Environment![SelfUpdate.HandedOffVariable]);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "dotnet" && c.Arguments.FirstOrDefault() is "new"), host.Output);

        var update = host.Runner.Detached.Single();
        Assert.AreEqual(updater, update.FileName);
        StringAssert.Contains(update.Arguments[^1], $"{Environment.ProcessId}");
        StringAssert.Contains(update.Arguments[^1], "tool update --global Bit.Cli --version 10.7.0");
    }

    [TestMethod]
    public async Task APinnedTemplateVersion_Should_UseItsOwnBitAndLeaveTheInstalledOneAlone()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        var pinned = Directory.CreateDirectory(Path.Combine(host.Environment.BitDirectory, "cli", "10.5.0")).FullName;
        File.WriteAllText(Path.Combine(pinned, "bit"), "");
        var stale = Directory.CreateDirectory(Path.Combine(host.Environment.BitDirectory, "cli", "10.6.2")).FullName;
        host.Runner.On("bit", "", 0);

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--template-version", "10.5.0", "--yes"), host.Output);

        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName == Path.Combine(pinned, "bit")), host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["package", "search", ..] or ["tool", "install", ..]));
        Assert.IsEmpty(host.Runner.Detached);
        Assert.IsFalse(Directory.Exists(stale));
    }

    [TestMethod]
    public async Task ABitThatCantBeDownloaded_Should_LeaveTheRunToThisOne()
    {
        var prompter = new ScriptedPrompter().On("Create it?", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.7.0" })));
        host.Runner.On("dotnet", "tool install", 1, "error NU1101: Unable to find package Bit.Cli.");

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso"), host.Output);

        StringAssert.Contains(host.Output, "Couldn't get bit 10.7.0, so bit 10.6.2 goes on.");
        Assert.IsNotNull(prompter.Find("Create it?"));
        Assert.IsEmpty(host.Runner.Detached);
        Assert.IsFalse(Directory.Exists(Path.Combine(host.Environment.BitDirectory, "cli", "10.7.0")));
    }

    [TestMethod]
    public async Task ADryRun_Should_OnlySayANewerBitWouldCreateIt()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.7.0" })));

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--dry-run", "--yes"), host.Output);

        StringAssert.Contains(host.Output, "Without --dry-run, bit 10.7.0 would create it.");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["tool", "install", ..]));
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task BuildsFromSourceCiAndOptOuts_Should_NeverLookForANewerBit()
    {
        await AssertStaysAsync(h => new SelfUpdate(h.Services) { CurrentVersion = "10.6.2", IsOfficialBuild = false, InstallMethod = "tool" });
        await AssertStaysAsync(h => Official(h, install: "dnx"));
        await AssertStaysAsync(h => Official(h), new() { ["GITHUB_ACTIONS"] = "true" });
        await AssertStaysAsync(h => Official(h), new() { [SelfUpdate.OptOutVariable] = "1" });
        await AssertStaysAsync(h => Official(h), new() { [SelfUpdate.HandedOffVariable] = "10.6.1" });
        await AssertStaysAsync(h => Official(h), extra: "--no-update");
        await AssertStaysAsync(h => Official(h), extra: ["--template-package", "Bit.Boilerplate.0.0.0.nupkg"]);
    }

    [TestMethod]
    public async Task BitUpdate_Should_ReplaceTheInstalledBitOnceItEnds()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Services.SelfUpdate = Official(host);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.7.0" })));

        Assert.AreEqual(0, await host.RunAsync("update"), host.Output);

        StringAssert.Contains(host.Output, "bit 10.7.0 replaces 10.6.2 as soon as this command ends.");
        StringAssert.Contains(host.Runner.Detached.Single().Arguments[^1], "tool update --global Bit.Cli --version 10.7.0");

        using var source = new TestHost(HostOs.Linux);
        source.Services.SelfUpdate = new SelfUpdate(source.Services) { CurrentVersion = "10.6.2", IsOfficialBuild = false, InstallMethod = "local" };

        Assert.AreEqual(0, await source.RunAsync("update"), source.Output);

        StringAssert.Contains(source.Output, "built from source");
        Assert.IsEmpty(source.Runner.Calls);
        Assert.IsEmpty(source.Runner.Detached);
    }

    private static async Task AssertStaysAsync(Func<TestHost, SelfUpdate> update, Dictionary<string, string>? variables = null, params string[] extra)
    {
        using var host = new TestHost(HostOs.Linux, variables);
        host.AllToolsInstalled();
        host.Services.SelfUpdate = update(host);
        host.Runner.On("dotnet", "package search", 0, Search(("nuget.org", new[] { "10.7.0" })));

        Assert.AreEqual(0, await host.RunAsync(["new", "Contoso", "--dry-run", "--yes", .. extra]), host.Output);

        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["package", "search", ..]), host.Output);
        Assert.IsFalse(host.Output.Contains("10.7.0", StringComparison.Ordinal), host.Output);
    }

    private static SelfUpdate Official(TestHost host, string version = "10.6.2", string install = "tool")
    {
        return new SelfUpdate(host.Services) { CurrentVersion = version, IsOfficialBuild = true, InstallMethod = install };
    }

    private static string Search(params (string Source, string[] Versions)[] sources)
    {
        return JsonSerializer.Serialize(new
        {
            version = 2,
            problems = Array.Empty<object>(),
            searchResult = sources.Select(s => new { sourceName = s.Source, packages = s.Versions.Select(v => new { id = "Bit.Cli", version = v }) })
        });
    }
}
