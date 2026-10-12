using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class RemoveTests
{
    [TestMethod]
    public async Task Remove_Should_ChangeNothingWhenTheAnswerIsNo()
    {
        var prompter = new ScriptedPrompter().On("Delete", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await host.RunAsync("remove", folder));

        Assert.AreEqual($"Delete {folder} and everything in it?", prompter.Prompts.Single().Question);
        Assert.AreEqual("no", prompter.Prompts.Single().Preselected.Single());
        StringAssert.Contains(host.Output, "Nothing was changed.");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Web.slnf")));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "dotnet"));
    }

    [TestMethod]
    public async Task Remove_Should_NeedYesWithoutATerminal()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", folder));

        StringAssert.Contains(host.FlatOutput, "Pass --yes");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Web.slnf")));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "dotnet"));
    }

    [TestMethod]
    public async Task Remove_Should_DeleteEverythingWithYes()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

        Assert.IsFalse(Directory.Exists(folder));
        Assert.IsTrue(Directory.Exists(host.WorkingDirectory));
        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName is "dotnet" && string.Join(' ', c.Arguments) is "build-server shutdown"));
        StringAssert.Contains(host.FlatOutput, "Removed Contoso");
    }

    [TestMethod]
    public async Task Remove_Should_SayWhatWouldBeLost()
    {
        var prompter = new ScriptedPrompter().On("Delete", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder.Replace('\\', '/'));
        host.Runner.On("git", "status --porcelain", 0, "?? notes.txt\n");
        host.Runner.On("git", "rev-list --count", 0, "2\n");

        Assert.AreEqual(0, await host.RunAsync("remove", folder));
        Assert.AreEqual($"It has changes that aren't committed.\nIts branch has 2 commits that aren't pushed.\nDelete {folder} and everything in it?", prompter.Prompts[^1].Question);

        host.Runner.On("git", "status --porcelain", 0, "");
        host.Runner.On("git", "rev-list --count", 128, "fatal: no upstream configured for branch 'develop'");

        Assert.AreEqual(0, await host.RunAsync("remove", folder));
        Assert.AreEqual($"Its branch has no upstream, so its commits may exist only here.\nDelete {folder} and everything in it?", prompter.Prompts[^1].Question);

        foreach (var file in Directory.EnumerateFiles(Path.Combine(folder, ".git"), "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(Path.Combine(folder, ".git"), recursive: true);
        host.Runner.Calls.Clear();

        Assert.AreEqual(0, await host.RunAsync("remove", folder));
        Assert.AreEqual($"Delete {folder} and everything in it?", prompter.Prompts[^1].Question);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "rev-list"));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Web.slnf")));
    }

    [TestMethod]
    public async Task Remove_Should_RequireAProjectFolder()
    {
        using var host = new TestHost(HostOs.Linux);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", host.WorkingDirectory, "--yes"));
        StringAssert.Contains(host.Output, "isn't a bit Boilerplate project folder");

        var solution = Path.Combine(host.WorkingDirectory, "Bit.slnx");
        File.WriteAllText(solution, "<Solution />");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", host.WorkingDirectory, "--yes"));
        Assert.IsTrue(File.Exists(solution));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Remove_Should_KeepTheEmptyFolderATerminalIsIn()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        using var terminal = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe")) { WorkingDirectory = folder, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

            Assert.IsTrue(Directory.Exists(folder));
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(folder));
            StringAssert.Contains(host.FlatOutput, "stays, empty, while a terminal is in it");
            Assert.IsFalse(terminal.HasExited);
        }
        finally
        {
            if (terminal.HasExited is false)
            {
                terminal.Kill();
                terminal.WaitForExit();
            }
        }
    }

    private static string CreateProject(TestHost host)
    {
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;

        Write(Path.Combine(folder, "Contoso.slnx"), "<Solution />");
        Write(Path.Combine(folder, "Contoso.Web.slnf"), "{}");
        Write(Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "Program.cs"), "namespace Contoso.Client.Core;");
        Write(Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "node_modules", "sass", "index.js"), "");
        Write(Path.Combine(folder, ".git", "HEAD"), "ref: refs/heads/develop\n");
        ReadOnly(Path.Combine(folder, ".git", "objects", "ab", "cdef0123456789"), "x");
        ReadOnly(Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "locked.txt"), "x");

        return folder;
    }

    private static void ReadOnly(string path, string content)
    {
        Write(path, content);
        File.SetAttributes(path, FileAttributes.ReadOnly);
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
