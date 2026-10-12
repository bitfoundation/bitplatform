using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class CleanTests
{
    [TestMethod]
    public async Task Clean_Should_StopTheBuildServersThenRunTheProjectsScript()
    {
        using var windows = new TestHost(HostOs.Windows);
        var windowsProject = CreateProject(windows, "Clean.bat");

        Assert.AreEqual(0, await windows.RunAsync("clean", windowsProject), windows.Output);
        AssertRanInOrder(windows, windowsProject, "cmd.exe", "/c Clean.bat");

        using var linux = new TestHost(HostOs.Linux);
        var linuxProject = CreateProject(linux, "Clean.sh");

        Assert.AreEqual(0, await linux.RunAsync("clean", linuxProject), linux.Output);
        AssertRanInOrder(linux, linuxProject, "bash", "Clean.sh");
    }

    [TestMethod]
    public async Task Clean_Should_FailWhenTheScriptFails()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host, "Clean.sh");
        host.Runner.On("bash", "Clean.sh", 1, "rm: cannot remove 'bin': Device or resource busy");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("clean", folder));
        StringAssert.Contains(host.FlatOutput, "Clean.sh failed");
    }

    [TestMethod]
    public async Task Clean_Should_NeedTheProjectsScript()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host, "Clean.bat");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("clean", folder));
        StringAssert.Contains(host.Output, "has no Clean.sh");
        Assert.IsEmpty(host.Runner.Calls);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("clean", host.WorkingDirectory));
        StringAssert.Contains(host.Output, "isn't a bit Boilerplate project folder");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Clean_Should_StopAnAppThatRunsFromTheProjectFolder()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var folder = CreateProject(host, "Clean.bat");
        var app = Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "src", "Server", "Contoso.Server.Api", "bin", "Debug")).FullName, "Contoso.Server.Api.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), app);

        using var process = Process.Start(new ProcessStartInfo(app) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true })!;

        try
        {
            Assert.AreEqual(0, await host.RunAsync("clean", folder), host.Output);
            Assert.IsTrue(process.WaitForExit(TimeSpan.FromSeconds(10)));
            StringAssert.Contains(host.FlatOutput, $"Stopped Contoso.Server.Api process {process.Id}");
        }
        finally
        {
            if (process.HasExited is false)
            {
                process.Kill();
            }
        }
    }

    [TestMethod]
    public void IsUnder_Should_MatchOnlyPathsInsideTheFolder()
    {
        Assert.IsTrue(ProjectProcesses.IsUnder(@"C:\Projects\Shop\src\bin\Shop.exe", @"C:\Projects\Shop"));
        Assert.IsTrue(ProjectProcesses.IsUnder(@"c:\projects\shop\node_modules\sass-embedded\dart.exe", @"C:\Projects\Shop\"));
        Assert.IsTrue(ProjectProcesses.IsUnder("/home/me/Shop/bin/Shop", "/home/me/Shop/"));

        Assert.IsFalse(ProjectProcesses.IsUnder(@"C:\Projects\Shop2\bin\Shop.exe", @"C:\Projects\Shop"));
        Assert.IsFalse(ProjectProcesses.IsUnder(@"C:\Projects\Shop.exe", @"C:\Projects\Shop"));
        Assert.IsFalse(ProjectProcesses.IsUnder(@"C:\Projects\Shop", @"C:\Projects\Shop"));
        Assert.IsFalse(ProjectProcesses.IsUnder(@"C:\Projects\Shop\", @"C:\Projects\Shop"));
        Assert.IsFalse(ProjectProcesses.IsUnder(@"C:\Program Files\dotnet\dotnet.exe", @"C:\Projects\Shop"));
        Assert.IsFalse(ProjectProcesses.IsUnder("/home/me/Shopping/bin/Shop", "/home/me/Shop"));
    }

    private static void AssertRanInOrder(TestHost host, string folder, string fileName, string arguments)
    {
        var shutdown = host.Runner.Calls.FindIndex(c => c.FileName is "dotnet" && string.Join(' ', c.Arguments) is "build-server shutdown");
        var script = host.Runner.Calls.FindIndex(c => c.FileName == fileName && string.Join(' ', c.Arguments) == arguments);

        Assert.IsTrue(shutdown >= 0 && script > shutdown, host.Output);
        Assert.AreEqual(folder, host.Runner.Calls[script].WorkingDirectory);
        Assert.AreEqual(script + 1, host.Runner.Calls.Count);
    }

    private static string CreateProject(TestHost host, string script)
    {
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
        File.WriteAllText(Path.Combine(folder, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(folder, script), "");
        return folder;
    }
}
