using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;

namespace Bit.Cli.Tests;

[TestClass]
public class DiffTests
{
    [TestMethod]
    public void BitJson_Should_GiveTheVersionNameAndOptions()
    {
        using var host = new Infrastructure.TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "bit.json"), """
            {
                "template": "bit-bp",
                "version": "10.8.0",
                "name": "Contoso",
                "options": { "database": "PostgreSQL", "aspire": "False", "sample": true, "url": 5 }
            }
            """);

        var record = TemplateDiff.ReadProject(host.WorkingDirectory);

        Assert.AreEqual("10.8.0", record.Version);
        Assert.AreEqual("Contoso", record.Name);
        CollectionAssert.AreEquivalent(new Dictionary<string, string> { ["database"] = "PostgreSQL", ["aspire"] = "False", ["sample"] = "true" }, record.Options.ToDictionary());

        var (arguments, _) = TemplateDiff.TemplateArguments(TemplateManifest.Embedded, record.Options);
        CollectionAssert.AreEqual(new[] { "--database", "PostgreSQL", "--aspire", "false", "--sample", "true" }, arguments);

        File.Delete(Path.Combine(host.WorkingDirectory, "bit.json"));
        Assert.IsNull(TemplateDiff.ReadProject(host.WorkingDirectory).Version);
    }

    [TestMethod]
    public void TheSummary_Should_ShowThePasswordTheReadmeNames()
    {
        Assert.AreEqual("40817263", NewWorkflow.ReadSeedPassword("pre-confirmed accounts whose password is **`40817263`**:"));
        Assert.IsNull(NewWorkflow.ReadSeedPassword("## Welcome!"));
    }

    [TestMethod]
    public void EachVersion_Should_GetOnlyTheOptionsItHas()
    {
        var options = new Dictionary<string, string> { ["database"] = "postgresql", ["madeUp"] = "true", ["module"] = "Nope" };

        var (arguments, unknown) = TemplateDiff.TemplateArguments(TemplateManifest.Embedded, options);

        CollectionAssert.AreEqual(new[] { "--database", "PostgreSQL" }, arguments);
        CollectionAssert.AreEquivalent(new[] { "--madeUp true", "--module Nope" }, unknown);
    }

    [TestMethod]
    public async Task TheDiff_Should_GoToAFolderOfTheUsersOwn()
    {
        using var host = new Infrastructure.TestHost(HostOs.Linux);

        await host.RunAsync("diff", "10.6.0", "10.7.0", "--name", "Shop");

        Assert.AreEqual(Path.Combine(host.Environment.BitDirectory, "diff", "Shop-10.6.0-10.7.0"), host.Runner.Calls.First(c => c.FileName is "git").WorkingDirectory, "Others can plant a link in the shared temp folder.");
    }

    [TestMethod]
    public async Task AnOptionDiffDoesntHave_Should_StopBeforeItCreatesAnything()
    {
        using var host = new Infrastructure.TestHost(HostOs.Linux);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("diff", "--yes", "--name", "Shop"), host.Output);
        StringAssert.Contains(host.FlatOutput, "'--yes' isn't a bit Boilerplate version");
        Assert.IsEmpty(host.Runner.Calls);
        Assert.IsFalse(Directory.Exists(Path.Combine(host.Environment.BitDirectory, "diff")));
    }

    [TestMethod]
    public void FolderRemover_Should_RemoveALinkedFolderButNotWhatItLinksTo()
    {
        using var host = new Infrastructure.TestHost();
        var target = Directory.CreateDirectory(Path.Combine(host.Root, "projects")).FullName;
        File.WriteAllText(Path.Combine(target, "Program.cs"), "keep");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(target, "src")).FullName, "App.cs"), "keep");
        var link = Path.Combine(Directory.CreateDirectory(Path.Combine(host.Root, "bit-diff")).FullName, "Shop-10.6.0-10.7.0");
        Link(link, target);

        var (failures, _) = FolderRemover.Delete(link);

        Assert.IsEmpty(failures);
        Assert.IsFalse(Path.Exists(link));
        Assert.IsTrue(File.Exists(Path.Combine(target, "Program.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(target, "src", "App.cs")));
    }

    private static void Link(string link, string target)
    {
        if (OperatingSystem.IsWindows() is false)
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        using var junction = Process.Start(new ProcessStartInfo("cmd.exe", ["/c", "mklink", "/J", link, target]) { RedirectStandardOutput = true, UseShellExecute = false })!;
        junction.WaitForExit();
        Assert.AreEqual(0, junction.ExitCode);
    }
}
