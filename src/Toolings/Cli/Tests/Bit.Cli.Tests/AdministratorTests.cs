using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Tools;

namespace Bit.Cli.Tests;

[TestClass]
public class AdministratorTests
{
    [TestMethod]
    public async Task TheHelper_Should_RunWhatItIsSent_StreamItsOutput_AndStopOnQuit()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        host.Runner.Executables["dotnet"] = @"C:\Program Files\dotnet\dotnet.exe";
        host.Runner.On("dotnet", "workload install", spec =>
        {
            spec.OnOutputLine?.Invoke("Installing wasm-tools");
            return new ProcessResult { ExitCode = 3 };
        });

        var (session, serving) = await ConnectAsync(host);
        var lines = new List<string>();

        var result = await session.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = ["workload", "install", "wasm-tools"], WorkingDirectory = host.WorkingDirectory, OnOutputLine = lines.Add }, CancellationToken.None);

        Assert.AreEqual(3, result.ExitCode);
        CollectionAssert.AreEqual(new[] { "Installing wasm-tools" }, lines);
        var call = host.Runner.Calls.Single();
        Assert.AreEqual(@"C:\Program Files\dotnet\dotnet.exe", call.FileName);
        Assert.AreEqual(host.WorkingDirectory, call.WorkingDirectory);
        Assert.IsTrue(call.Environment!.ContainsKey("PATH"));

        await session.DisposeAsync();
        Assert.AreEqual(CliApp.ExitOk, await serving.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    public async Task TheHelper_Should_CancelTheRunningCommand_AndTakeTheNextOne()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        host.Runner.OnAsync("dotnet", "build", async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new ProcessResult { ExitCode = 0 };
        });
        host.Runner.On("dotnet", "--version", 0, "10.0.403");

        var (session, serving) = await ConnectAsync(host);

        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = ["build"] }, cancel.Token));

        var next = await session.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = ["--version"] }, CancellationToken.None);
        Assert.AreEqual(0, next.ExitCode);

        await session.DisposeAsync();
        Assert.AreEqual(CliApp.ExitOk, await serving.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task TheAndroidSdk_Should_BeInstalledAsAdministratorFromBitsOwnProject()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var sdk = Directory.CreateTempSubdirectory("bit-cli-android-sdk-");
        var security = sdk.GetAccessControl();
        var denied = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.CreateFiles, AccessControlType.Deny);
        security.AddAccessRule(denied);
        sdk.SetAccessControl(security);

        try
        {
            using var host = new TestHost(HostOs.Windows, new Dictionary<string, string> { ["ANDROID_HOME"] = sdk.FullName });
            var directory = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
            File.WriteAllText(Path.Combine(directory, "global.json"), """{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature", "paths": [ "sdk" ] } }""");
            var project = new ProjectContext { Name = "Contoso", Directory = directory, Platforms = new HashSet<Platform> { Platform.Android } };
            string? installer = null;
            string? globalJson = null;
            host.Runner.On("dotnet", "build", spec =>
            {
                installer = File.ReadAllText(Path.Combine(spec.WorkingDirectory!, "android.csproj"));
                globalJson = File.ReadAllText(Path.Combine(spec.WorkingDirectory!, "global.json"));
                return new ProcessResult { ExitCode = 0 };
            });

            var (session, serving) = await ConnectAsync(host);
            host.Services.Administrator = session;

            var result = await new ProjectSteps(host.Services, project).AndroidDependenciesAsync(_ => { }, CancellationToken.None);

            Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
            var call = host.Runner.Calls.Single();
            Assert.IsFalse(call.WorkingDirectory!.StartsWith(directory, StringComparison.OrdinalIgnoreCase), "The project's own build files would run as administrator.");
            CollectionAssert.Contains(call.Arguments.ToArray(), "-t:InstallAndroidDependencies");
            CollectionAssert.Contains(call.Arguments.ToArray(), "-noAutoResponse");
            StringAssert.Contains(installer, "-android</TargetFramework>");
            StringAssert.Contains(globalJson, "\"10.0.100\"");
            Assert.IsFalse(globalJson!.Contains("paths", StringComparison.Ordinal), globalJson);
            Assert.IsFalse(Directory.Exists(call.WorkingDirectory));

            await session.DisposeAsync();
            Assert.AreEqual(CliApp.ExitOk, await serving.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            security.RemoveAccessRule(denied);
            sdk.SetAccessControl(security);
            sdk.Delete();
        }
    }

    [TestMethod]
    public async Task TheUpfrontPrompt_Should_ListTheAdministratorTools_OnlyOnWindowsWithATerminal()
    {
        var git = new ToolCheck(ToolCatalog.Find("git")!, ToolStatus.Missing(), true, "", new ToolAction { ToolId = "git", Title = "Install Git", Elevation = Elevation.Admin, Commands = [new ProcessSpec { FileName = "winget" }] }, null);
        var needs = new ToolNeeds { Platforms = new HashSet<Platform> { Platform.Web } };

        using var windows = new TestHost(HostOs.Windows, prompter: new ScriptedPrompter());
        CollectionAssert.AreEqual(new[] { git.Tool.Name }, (await AdministratorNeeds.FindAsync(windows.Services, [git], needs, setup: false, CancellationToken.None)).ToArray());

        using var noTerminal = new TestHost(HostOs.Windows);
        Assert.IsEmpty(await AdministratorNeeds.FindAsync(noTerminal.Services, [git], needs, setup: false, CancellationToken.None));

        using var linux = new TestHost(HostOs.Linux, prompter: new ScriptedPrompter());
        Assert.IsEmpty(await AdministratorNeeds.FindAsync(linux.Services, [git], needs, setup: false, CancellationToken.None));
    }

    private static async Task<(AdministratorSession Session, Task<int> Serving)> ConnectAsync(TestHost host)
    {
        var name = $"bit-cli-test-{Guid.NewGuid():N}";
        var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var serving = AdministratorSession.ServeAsync(host.Services, name, Environment.ProcessId, CancellationToken.None);
        await pipe.WaitForConnectionAsync();

        var exited = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/c exit 0") { UseShellExecute = false, CreateNoWindow = true })!;
        await exited.WaitForExitAsync();

        return (AdministratorSession.Attach(host.Services, pipe, exited), serving);
    }
}
