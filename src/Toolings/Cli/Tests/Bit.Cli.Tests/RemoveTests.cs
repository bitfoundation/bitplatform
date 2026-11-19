using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class RemoveTests
{
    private const string TerminalFolderVariable = "BIT_CLI_TESTS_TERMINAL_FOLDER";

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
        InARepository(host, folder);
        host.Runner.On("git", "status --porcelain", 0, "?? notes.txt\n");
        host.Runner.On("git", "rev-list --count @{u}", 0, "2\n");

        Assert.AreEqual(0, await host.RunAsync("remove", folder));
        Assert.AreEqual($"It has changes that aren't committed.\nIts branch has 2 commits that aren't pushed.\nDelete {folder} and everything in it?", prompter.Prompts[^1].Question);

        host.Runner.On("git", "status --porcelain", 0, "");
        host.Runner.On("git", "rev-list --count @{u}", 128, "fatal: no upstream configured for branch 'develop'");

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
    public async Task Remove_Should_SayWhatOtherBranchesAndStashesWouldLose()
    {
        var prompter = new ScriptedPrompter().On("Delete", false);
        using var host = new TestHost(HostOs.Linux, prompter: prompter);
        var folder = CreateProject(host);
        var cart = Path.Combine(host.WorkingDirectory, "Contoso-cart");
        InARepository(host, folder);
        host.Runner.On("git", "rev-list --count @{u}", 0, "0\n");
        host.Runner.On("git", "rev-list --count --branches", 0, "3\n");
        host.Runner.On("git", "stash list", 0, "stash@{0}: WIP on develop: 1a2b3c4 Add the cart\n");
        host.Runner.On("git", "worktree list --porcelain", 0, $"""
            worktree {folder.Replace('\\', '/')}
            HEAD 1a2b3c4d
            branch refs/heads/develop

            worktree {cart.Replace('\\', '/')}
            HEAD 5e6f7a8b
            branch refs/heads/cart

            worktree {Path.Combine(host.WorkingDirectory, "Contoso-gone").Replace('\\', '/')}
            HEAD 9c0d1e2f
            branch refs/heads/gone
            prunable gitdir file points to non-existent location

            """);

        Assert.AreEqual(0, await host.RunAsync("remove", folder));

        Assert.AreEqual($"Other branches have 3 commits that aren't pushed.\nIts repository has stashed changes.\nIts repository has another worktree, {cart}, which stops working without it.\nDelete {folder} and everything in it?", prompter.Prompts.Single().Question);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Web.slnf")));
    }

    [TestMethod]
    public async Task Remove_Should_DeleteAnyFolder()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = Path.Combine(host.WorkingDirectory, "notes");
        Write(Path.Combine(folder, "2026", "11", "meeting.md"), "# Meeting");
        ReadOnly(Path.Combine(folder, "read-only.txt"), "x");
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

        Assert.IsFalse(Directory.Exists(folder));
        Assert.IsTrue(Directory.Exists(host.WorkingDirectory));
        StringAssert.Contains(host.FlatOutput, "Deleted the folder");
        StringAssert.Contains(host.FlatOutput, "Removed notes");
    }

    [TestMethod]
    public async Task Remove_Should_NeedAFolderThatExists()
    {
        using var host = new TestHost(HostOs.Linux);
        var file = Path.Combine(host.WorkingDirectory, "notes.txt");
        File.WriteAllText(file, "x");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", file, "--yes"));
        StringAssert.Contains(host.FlatOutput, "is a file, not a folder.");
        Assert.IsTrue(File.Exists(file));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", Path.Combine(host.WorkingDirectory, "missing"), "--yes"));
        StringAssert.Contains(host.FlatOutput, "doesn't exist.");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    [DataRow("home", "is your home folder")]
    [DataRow("above home", "holds your home folder")]
    [DataRow("drive", "is a drive's root")]
    [DataRow("documents", "is one of your account's own folders")]
    [DataRow("program files", "is a system folder")]
    [DataRow("above program files", "holds a system folder")]
    [DataRow("bit", "is bit's own folder")]
    [DataRow("unix top level", "is a system folder")]
    public async Task Remove_Should_KeepTheFoldersItMustNeverDelete(string which, string says)
    {
        if (which is "unix top level" && OperatingSystem.IsWindows())
            return;

        var system = Directory.CreateTempSubdirectory("bit-cli-tests-system-").FullName;

        try
        {
            var programFiles = Path.Combine(system, "Program Files");
            using var host = new TestHost(HostOs.Windows, variables: new Dictionary<string, string> { ["ProgramFiles"] = programFiles });

            var folder = which switch
            {
                "home" => host.Home,
                "above home" => host.Root,
                "drive" => Path.GetPathRoot(host.Root)!,
                "documents" => Directory.CreateDirectory(Path.Combine(host.Home, "Documents")).FullName,
                "program files" => Directory.CreateDirectory(programFiles).FullName,
                "above program files" => Directory.CreateDirectory(programFiles).Parent!.FullName,
                "bit" => AppContext.BaseDirectory,
                _ => "/usr"
            };

            // Without --yes, so that nothing would be deleted even if the folder weren't kept.
            Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", folder));

            StringAssert.Contains(host.FlatOutput, $"{says}, so bit remove won't delete it.");
            Assert.IsTrue(Directory.Exists(folder));
            Assert.IsEmpty(host.Runner.Calls);
        }
        finally
        {
            Directory.Delete(system, recursive: true);
        }
    }

    [TestMethod]
    public async Task Remove_Should_KeepTheRepositoryOfTheFolderItRunsIn()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse --git-common-dir", 0, $"{folder.Replace('\\', '/')}/.git\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", folder, "--yes"));

        StringAssert.Contains(host.FlatOutput, "the folder you're in, so bit remove won't delete it from there.");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Web.slnf")));
        Assert.AreEqual(host.WorkingDirectory, host.Runner.Calls.Single().WorkingDirectory);
    }

    [TestMethod]
    public async Task Remove_Should_RemoveALinkedWorktreeFromItsRepository()
    {
        if (await GitIdentity.GitAvailableAsync() is false)
            return;

        using var host = new TestHost(variables: GitIdentity.RealPath(), realProcesses: true);
        using var identity = new GitIdentity(host.Root);
        var (main, worktrees) = await CreateRepositoryAsync(host, "feature", "kept", "stale");
        var feature = worktrees[0];
        Write(Path.Combine(feature, "notes.txt"), "not committed");
        ReadOnly(Path.Combine(feature, "bin", "Contoso.dll"), "x");
        await GitAsync(main, "worktree", "lock", "--reason", "on a removable drive", feature);
        Directory.Delete(worktrees[2], recursive: true);

        Assert.AreEqual(0, await host.RunAsync("remove", feature, "--yes"), host.Output);

        Assert.IsFalse(Directory.Exists(feature));
        StringAssert.Contains(host.FlatOutput, "It has changes that aren't committed.");
        StringAssert.Contains(host.FlatOutput, "It's a locked git worktree: on a removable drive.");
        StringAssert.Contains(host.FlatOutput, "Removed it from its repository's worktrees");
        CollectionAssert.AreEquivalent(new[] { "main", "kept", "stale" }, await WorktreesAsync(main), "Only the removed worktree leaves the list: the stale one someone else left stays.");
        await GitAsync(main, "branch", "-D", "feature");
    }

    [TestMethod]
    public async Task Remove_Should_SayWhatAWorktreeLeavesUnpushed()
    {
        if (await GitIdentity.GitAvailableAsync() is false)
            return;

        var prompter = new ScriptedPrompter().On("Delete", false);
        using var host = new TestHost(variables: GitIdentity.RealPath(), prompter: prompter, realProcesses: true);
        using var identity = new GitIdentity(host.Root);
        var (main, worktrees) = await CreateRepositoryAsync(host, "local");
        var ahead = Path.Combine(host.Root, "ahead");
        await GitAsync(main, "worktree", "add", "--track", "-b", "ahead", ahead, "origin/main");
        Write(Path.Combine(ahead, "cart.md"), "# Cart");
        await GitAsync(ahead, "add", ".");
        await GitAsync(ahead, "commit", "-m", "Add the cart");

        Assert.AreEqual(0, await host.RunAsync("remove", ahead), host.Output);

        var question = prompter.Prompts[^1].Question;
        StringAssert.StartsWith(question, "Its branch ahead has 1 commit that isn't pushed. The branch stays in the repository in ");
        StringAssert.EndsWith(question, $"\nDelete {ahead} and everything in it, and remove it from its repository's worktrees?");

        Assert.AreEqual(0, await host.RunAsync("remove", worktrees[0]), host.Output);

        question = prompter.Prompts[^1].Question;
        StringAssert.StartsWith(question, "Its branch local has no upstream, so its commits may exist only in the repository in ");
        StringAssert.Contains(question, ", where the branch stays.\n");

        Assert.IsTrue(Directory.Exists(ahead));
        Assert.IsTrue(Directory.Exists(worktrees[0]));
        CollectionAssert.AreEquivalent(new[] { "main", "local", "ahead" }, await WorktreesAsync(main));
    }

    [TestMethod]
    public async Task Remove_Should_KeepTheMainWorktreeOfTheWorktreeItRunsIn()
    {
        if (await GitIdentity.GitAvailableAsync() is false)
            return;

        using var host = new TestHost(variables: GitIdentity.RealPath(), realProcesses: true);
        using var identity = new GitIdentity(host.Root);
        var (main, _) = await CreateRepositoryAsync(host);
        await GitAsync(main, "worktree", "add", "-b", "work", host.WorkingDirectory);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("remove", main, "--yes"));

        StringAssert.Contains(host.FlatOutput, "the folder you're in, so bit remove won't delete it from there.");
        Assert.IsTrue(File.Exists(Path.Combine(main, "README.md")));
        CollectionAssert.AreEquivalent(new[] { "main", "work" }, await WorktreesAsync(main));
    }

    [TestMethod]
    public async Task Remove_Should_RemoveAWorktreeWhoseEmptyFolderATerminalKeeps()
    {
        if (OperatingSystem.IsWindows() is false || await GitIdentity.GitAvailableAsync() is false)
            return;

        using var host = new TestHost(HostOs.Windows, variables: GitIdentity.RealPath(), realProcesses: true);
        using var identity = new GitIdentity(host.Root);
        var (main, worktrees) = await CreateRepositoryAsync(host, "feature");

        using var terminal = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe")) { WorkingDirectory = worktrees[0], UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        try
        {
            Assert.AreEqual(0, await host.RunAsync("remove", worktrees[0], "--yes"), host.Output);

            Assert.IsTrue(Directory.Exists(worktrees[0]));
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(worktrees[0]));
            StringAssert.Contains(host.FlatOutput, $"stays, empty, while cmd ({terminal.Id}) is in it");
            StringAssert.Contains(host.FlatOutput, "Removed it from its repository's worktrees");
            CollectionAssert.AreEqual(new[] { "main" }, await WorktreesAsync(main));
        }
        finally
        {
            Stop(terminal);
        }
    }

    [TestMethod]
    public async Task Remove_Should_FinishWhatAFailedRunLeft()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        File.Delete(Path.Combine(folder, "Contoso.slnx"));
        File.Delete(Path.Combine(folder, "Contoso.Web.slnf"));

        Assert.AreEqual(0, await host.RunAsync("remove", folder, "--yes"), host.Output);

        Assert.IsFalse(Directory.Exists(folder));
        StringAssert.Contains(host.FlatOutput, "Removed Contoso");
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
    public async Task Remove_Should_NotWaitForTheTerminalItRunsIn()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        if (Environment.GetEnvironmentVariable(TerminalFolderVariable) is { Length: > 0 } folder)
        {
            using var inTerminal = new TestHost(HostOs.Windows, prompter: new ScriptedPrompter());
            inTerminal.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

            Assert.AreEqual(0, await inTerminal.RunAsync("remove", folder, "--yes"), inTerminal.Output);
            StringAssert.Contains(inTerminal.FlatOutput, "this terminal is still in it");
            StringAssert.Contains(inTerminal.FlatOutput, $"stays, empty, while this terminal is in it. Run cd {ProcessSpec.ShellQuote(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder))!, HostOs.Windows)}, then delete the folder.");
            return;
        }

        using var host = new TestHost(HostOs.Windows);
        var project = CreateProject(host);
        var tests = Path.ChangeExtension(typeof(RemoveTests).Assembly.Location, ".exe");

        using var terminal = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), $"/c cd /d \"{project}\" && \"{tests}\" --filter FullyQualifiedName~{nameof(Remove_Should_NotWaitForTheTerminalItRunsIn)}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            Environment = { [TerminalFolderVariable] = project }
        })!;

        var output = terminal.StandardOutput.ReadToEndAsync();

        try
        {
            Assert.IsTrue(terminal.WaitForExit(TimeSpan.FromMinutes(1)), "bit remove still waits for the terminal it runs in.");
            Assert.AreEqual(0, terminal.ExitCode, await output);
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(project));
        }
        finally
        {
            if (terminal.HasExited is false)
            {
                terminal.Kill(entireProcessTree: true);
                terminal.WaitForExit();
            }
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

    /// <summary>Answers git as if the folder were the main working tree of a repository, and the folder bit runs in weren't in one.</summary>
    private static void InARepository(TestHost host, string folder)
    {
        var topLevel = folder.Replace('\\', '/');

        host.Runner.On("git", "rev-parse --git-common-dir", 128, "fatal: not a git repository");
        host.Runner.On("git", "rev-parse --show-toplevel", 0, $"{topLevel}\n{topLevel}/.git\n.git\n");
    }

    /// <summary>A repository in a folder named main, pushed to an origin of its own, with a linked worktree on a new branch for each name.</summary>
    private static async Task<(string Main, string[] Worktrees)> CreateRepositoryAsync(TestHost host, params string[] worktrees)
    {
        var origin = Path.Combine(host.Root, "origin.git");
        var main = Path.Combine(host.Root, "main");

        await GitAsync(host.Root, "init", "--bare", "--initial-branch=main", origin);
        await GitAsync(host.Root, "init", "--initial-branch=main", main);
        Write(Path.Combine(main, ".gitignore"), "bin/\n");
        Write(Path.Combine(main, "README.md"), "# Contoso\n");
        await GitAsync(main, "add", ".");
        await GitAsync(main, "commit", "-m", "Start Contoso");
        await GitAsync(main, "remote", "add", "origin", origin);
        await GitAsync(main, "push", "--set-upstream", "origin", "main");

        var paths = worktrees.Select(name => Path.Combine(host.Root, name)).ToArray();

        foreach (var (name, path) in worktrees.Zip(paths))
        {
            await GitAsync(main, "worktree", "add", "-b", name, path);
        }

        return (main, paths);
    }

    private static async Task<string[]> WorktreesAsync(string main)
    {
        var list = await GitAsync(main, "worktree", "list", "--porcelain");

        return [.. list.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.StartsWith("worktree ", StringComparison.Ordinal)).Select(line => Path.GetFileName(line["worktree ".Length..]))];
    }

    private static async Task<string> GitAsync(string folder, params string[] arguments)
    {
        var result = await new ProcessRunner(CliEnvironment.FromProcess(), CliLog.None).RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = folder, Timeout = TimeSpan.FromMinutes(1) });

        Assert.IsTrue(result.Succeeded, $"git {string.Join(' ', arguments)} failed: {result.Output}");
        return result.Output;
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
