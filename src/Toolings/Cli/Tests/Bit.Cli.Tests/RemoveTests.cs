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
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "dotnet" && c.Arguments.FirstOrDefault() is "build-server"));
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
            StringAssert.Contains(host.FlatOutput, $"stays, empty, while cmd ({terminal.Id}) is in it");
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

    [TestMethod]
    public async Task Remove_Should_StopTheBuildProcessThatIsInTheFolder()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var buildProcess = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "sdk")).FullName, "MSBuild.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), buildProcess);

        using var node = Process.Start(new ProcessStartInfo(buildProcess) { WorkingDirectory = folder, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsTrue(node.WaitForExit(TimeSpan.FromSeconds(10)));
            StringAssert.Contains(host.FlatOutput, $"Stopped MSBuild process {node.Id}");
        }
        finally
        {
            Stop(node);
        }
    }

    [TestMethod]
    public async Task Remove_Should_DeleteTheFolderOnceTheAppInItLeaves()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var prompter = new ScriptedPrompter().On("Delete", true);
        using var host = new TestHost(HostOs.Windows, prompter: prompter);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        using var terminal = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe")) { WorkingDirectory = folder, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        var leaving = Task.Run(async () =>
        {
            while (File.Exists(Path.Combine(folder, "Contoso.Web.slnf")))
            {
                await Task.Delay(100);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
            Stop(terminal);
        });

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder), host.Output);
            await leaving;

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsFalse(host.FlatOutput.Contains("stays, empty"), host.Output);
        }
        finally
        {
            Stop(terminal);
        }
    }

    [TestMethod]
    [DataRow("dotnet")]
    [DataRow("XamlCompiler")]
    public async Task Remove_Should_StopOnlyTheBuildProcessThatHoldsAFile(string name)
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var buildProcess = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "sdk")).FullName, $"{name}.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), buildProcess);

        using var holder = HoldFile(buildProcess, Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "obj", "held.txt"));

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsTrue(holder.WaitForExit(TimeSpan.FromSeconds(10)));
            StringAssert.Contains(host.FlatOutput, $"Stopped {name} process {holder.Id}");
        }
        finally
        {
            Stop(holder);
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task Remove_Should_StopWhatABuildStartedWhenItHoldsAFile(int depth)
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var buildProcess = Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "sdk")).FullName, "dotnet.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), buildProcess);

        using var build = HoldFile(buildProcess, Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "bin", "Contoso.Client.Core.dll"), depth);

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsTrue(build.WaitForExit(TimeSpan.FromSeconds(10)));
            StringAssert.Contains(host.FlatOutput, "after stopping cmd (");
        }
        finally
        {
            Stop(build);
        }
    }

    [TestMethod]
    public async Task Remove_Should_NameTheAppThatHoldsAFileAndLeaveItRunning()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var held = Path.Combine(folder, "src", "Client", "Contoso.Client.Core", "held.txt");

        using var holder = HoldFile(Path.Combine(Environment.SystemDirectory, "cmd.exe"), held);

        try
        {
            Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("remove", folder, "--yes"));

            Assert.IsTrue(File.Exists(held));
            Assert.IsFalse(holder.HasExited);
            StringAssert.Contains(host.FlatOutput, $"In use by cmd ({holder.Id})");
        }
        finally
        {
            Stop(holder);
        }
    }

    [TestMethod]
    public async Task Remove_Should_StopWhatRunsTheProjectFromItsCommandLineOrItsFolder()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var sdk = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "sdk")).FullName;
        var appHost = Directory.CreateDirectory(Path.Combine(folder, "src", "Server", "Contoso.Server.AppHost")).FullName;
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), Path.Combine(sdk, "aspire.exe"));
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), Path.Combine(sdk, "dotnet.exe"));

        using var aspire = Process.Start(new ProcessStartInfo(Path.Combine(sdk, "aspire.exe"), $"/k rem run --apphost \"{Path.Combine(appHost, "Contoso.Server.AppHost.csproj")}\"") { WorkingDirectory = host.WorkingDirectory, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;
        using var watch = Process.Start(new ProcessStartInfo(Path.Combine(sdk, "dotnet.exe"), "/k rem watch") { WorkingDirectory = appHost, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsTrue(aspire.WaitForExit(TimeSpan.FromSeconds(10)));
            Assert.IsTrue(watch.WaitForExit(TimeSpan.FromSeconds(10)));
            StringAssert.Contains(host.FlatOutput, $"Stopped aspire process {aspire.Id}");
            StringAssert.Contains(host.FlatOutput, $"Stopped dotnet process {watch.Id}");
        }
        finally
        {
            Stop(aspire);
            Stop(watch);
        }
    }

    [TestMethod]
    public async Task Remove_Should_DeleteAFileWhoseNameWindowsReadsAsADevice()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var knownHosts = Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "src", "Tests", "bin", "Debug", "net10.0")).FullName, "NUL");
        File.WriteAllText($@"\\?\{knownHosts}", "[127.0.0.1]:52045 ssh-ed25519 AAAA");

        Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

        Assert.IsFalse(Directory.Exists(folder));
    }

    private static Process HoldFile(string shell, string file, int depth = 0)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var arguments = $"/c {string.Concat(Enumerable.Repeat("cmd /c ", depth))}pause {new string('^', (1 << depth) - 1)}> \"{file}\"";
        var process = Process.Start(new ProcessStartInfo(shell, arguments) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        for (var i = 0; i < 50 && File.Exists(file) is false; i++)
        {
            Thread.Sleep(100);
        }

        return process;
    }

    private static void Stop(Process process)
    {
        if (process.HasExited is false)
        {
            process.Kill();
            process.WaitForExit();
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
