using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Tools;

namespace Bit.Cli.Tests;

[TestClass]
public class ToolTests
{
    [TestMethod]
    public async Task InstalledTools_Should_NotBeOffered()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);

        var checks = await CheckAsync(host, new ToolNeeds { Aspire = true });

        Assert.IsTrue(checks.All(c => c.Status.IsSatisfied), string.Join(", ", checks.Where(c => c.Status.IsSatisfied is false).Select(c => c.Tool.Id)));
    }

    [TestMethod]
    public async Task NodeVersions_Should_BeJudgedByMajor()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);

        host.Runner.On("node", "--version", 0, "v18.19.0");
        var old = (await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "node");
        Assert.AreEqual(ToolState.Outdated, old.Status.State);

        host.Runner.NotFound("node");
        var missing = (await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "node");
        Assert.AreEqual(ToolState.Missing, missing.Status.State);
        Assert.IsTrue(missing.Needed);
    }

    [TestMethod]
    public async Task Docker_Should_TellNotRunningFromMissing()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);

        host.Runner.On("docker", "version", 1, "Cannot connect to the Docker daemon");
        var notRunning = (await CheckAsync(host, new ToolNeeds { Aspire = true })).Single(c => c.Tool.Id == "docker");
        Assert.AreEqual(ToolState.NotRunning, notRunning.Status.State);
        Assert.AreEqual("Start Docker", notRunning.Action!.Title);

        host.Runner.NotFound("docker");
        var missing = (await CheckAsync(host, new ToolNeeds { Aspire = true })).Single(c => c.Tool.Id == "docker");
        Assert.AreEqual(ToolState.Missing, missing.Status.State);
    }

    [TestMethod]
    public async Task DockerAndTheAspireCli_Should_BeNeededOnlyWithAspire()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.NotFound("docker");
        host.Runner.NotFound("aspire");

        var withAspire = await CheckAsync(host, new ToolNeeds { Aspire = true });
        Assert.IsTrue(withAspire.Single(c => c.Tool.Id == "docker").Needed);
        Assert.IsTrue(withAspire.Any(c => c.Tool.Id == "aspire"));

        var withoutAspire = await CheckAsync(host, new ToolNeeds { Aspire = false });
        Assert.IsFalse(withoutAspire.Single(c => c.Tool.Id == "docker").Needed);
        Assert.IsFalse(withoutAspire.Any(c => c.Tool.Id == "aspire"));
    }

    [TestMethod]
    public async Task WindowsOnlyTools_Should_NotShowUpElsewhere()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.NotFound("wsl.exe");

        var checks = await CheckAsync(host, new ToolNeeds { Aspire = true, Platforms = new HashSet<Platform> { Platform.Web, Platform.Android } });

        Assert.IsFalse(checks.Any(c => c.Tool.Id is "long-paths" or "wsl" or "visual-studio" or "xcode" or "homebrew"));
    }

    [TestMethod]
    public async Task Wsl_Should_OnlyBeOfferedWhenDockerIsMissingToo()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);
        host.Runner.On("wsl", "--status", 1);

        var dockerInstalled = await CheckAsync(host, new ToolNeeds { Aspire = true });
        Assert.IsFalse(dockerInstalled.Any(c => c.Tool.Id == "wsl"));

        host.Runner.NotFound("docker");
        var dockerMissing = await CheckAsync(host, new ToolNeeds { Aspire = true });
        Assert.IsTrue(dockerMissing.Single(c => c.Tool.Id == "wsl").Needed);
    }

    [TestMethod]
    public async Task TheDotnetSdk_Should_MatchTheFeatureBandOfGlobalJson()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        var needs = new ToolNeeds { MinimumSdk = new Version(10, 0, 100) };

        host.Runner.On("dotnet", "--list-sdks", 0, "9.0.300 [/usr/share/dotnet/sdk]\n11.0.100-rc.1.26425.128 [/usr/share/dotnet/sdk]");
        Assert.AreEqual(ToolState.Missing, (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dotnet-sdk").Status.State);

        host.Runner.On("dotnet", "--list-sdks", 0, "10.0.401 [/usr/share/dotnet/sdk]");
        Assert.AreEqual("10.0.401", (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dotnet-sdk").Status.Version);
    }

    [TestMethod]
    public async Task LinuxInstalls_Should_UseTheDistributionsPackageManagerWithSudo()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        host.Runner.Executables["sudo"] = "/usr/bin/sudo";
        host.Runner.NotFound("node");
        host.Runner.NotFound("git");

        var checks = await CheckAsync(host, new ToolNeeds());
        var node = checks.Single(c => c.Tool.Id == "node").Action!;
        var git = checks.Single(c => c.Tool.Id == "git").Action!;

        Assert.AreEqual(Elevation.Sudo, node.Elevation);
        StringAssert.Contains(node.Commands[0].CommandLine, "deb.nodesource.com");
        Assert.AreEqual("apt-get install -y git", git.Commands[1].CommandLine);
    }

    [TestMethod]
    public async Task WindowsInstalls_Should_UseWinget()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);
        host.Runner.Executables["winget"] = @"C:\winget.exe";
        host.Runner.NotFound("node");
        host.Runner.NotFound("docker");

        var checks = await CheckAsync(host, new ToolNeeds { Aspire = true });
        var node = checks.Single(c => c.Tool.Id == "node").Action!;

        Assert.AreEqual(Elevation.Admin, node.Elevation);
        CollectionAssert.IsSubsetOf(new[] { "install", "--id", "OpenJS.NodeJS.LTS", "--exact" }, node.Commands[0].Arguments.ToArray());
        Assert.AreEqual("Install Docker Desktop", checks.Single(c => c.Tool.Id == "docker").Action!.Title);
    }

    [TestMethod]
    public void TheAdministratorScript_Should_QuoteEverythingAndReportEachTool()
    {
        var actions = new[]
        {
            new ToolAction
            {
                ToolId = "long-paths",
                Title = "Enable Windows long paths",
                Elevation = Elevation.Admin,
                Commands = [new ProcessSpec { FileName = "reg.exe", Arguments = ["add", @"HKLM\SYSTEM\x", "/d", "it's 1"] }]
            },
            new ToolAction
            {
                ToolId = "node",
                Title = "Install Node.js LTS",
                Elevation = Elevation.Admin,
                SuccessExitCodes = [0, 3010],
                Commands = [new ProcessSpec { FileName = "winget", Arguments = ["install", "--id", "OpenJS.NodeJS.LTS"] }]
            }
        };

        var script = ToolInstaller.BuildWindowsScript(actions, @"C:\Temp\install.log", @"C:\Temp\results.json");

        StringAssert.Contains(script, "& 'reg.exe' 'add' 'HKLM\\SYSTEM\\x' '/d' 'it''s 1'");
        StringAssert.Contains(script, "$results['long-paths'] = $code");
        StringAssert.Contains(script, "$results['node'] = $code");
        StringAssert.Contains(script, "@(0, 3010) -contains $code");
        StringAssert.Contains(script, "Set-Content -LiteralPath 'C:\\Temp\\results.json'");
    }

    [TestMethod]
    public async Task TheHypervisorPlatform_Should_BeCheckedOnWindowsForTheAndroidEmulator()
    {
        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);
        var android = new ToolNeeds { Platforms = new HashSet<Platform> { Platform.Web, Platform.Android } };

        host.Runner.On("powershell", "-NoProfile -NonInteractive -Command (Get-CimInstance Win32_OptionalFeature", 0, "2\r\n");
        var hypervisor = (await CheckAsync(host, android)).Single(c => c.Tool.Id == "hypervisor-platform");
        Assert.IsTrue(hypervisor.Needed);
        Assert.AreEqual(Elevation.Admin, hypervisor.Action!.Elevation);
        CollectionAssert.Contains(hypervisor.Action.Commands[0].Arguments.ToArray(), "/featurename:HypervisorPlatform");

        host.Runner.On("powershell", "-NoProfile -NonInteractive -Command (Get-CimInstance Win32_OptionalFeature", 0, "1\r\n");
        Assert.IsTrue((await CheckAsync(host, android)).Single(c => c.Tool.Id == "hypervisor-platform").Status.IsSatisfied);

        Assert.IsFalse((await CheckAsync(host, new ToolNeeds())).Any(c => c.Tool.Id == "hypervisor-platform"));
    }

    [TestMethod]
    public async Task DeveloperMode_Should_BeOfferedForMauiWithoutBeingNeeded()
    {
        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);

        var maui = await CheckAsync(host, new ToolNeeds { Platforms = new HashSet<Platform> { Platform.Web, Platform.Android } });
        Assert.IsFalse(maui.Single(c => c.Tool.Id == "developer-mode").Needed);

        Assert.IsFalse((await CheckAsync(host, new ToolNeeds())).Any(c => c.Tool.Id == "developer-mode"));
    }

    [TestMethod]
    public async Task Python_Should_BeNeededOnLinuxForTheNativeWebAssemblyBuild()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        host.Runner.NotFound("python3");

        var python = (await CheckAsync(host, new ToolNeeds { NativeWebAssembly = true })).Single(c => c.Tool.Id == "python");
        Assert.IsTrue(python.Needed);
        Assert.AreEqual("apt-get install -y python3", python.Action!.Commands[1].CommandLine);

        Assert.IsFalse((await CheckAsync(host, new ToolNeeds())).Any(c => c.Tool.Id == "python"));
    }

    [TestMethod]
    public async Task VsCode_Should_BeNeededUnlessAnotherIdeIsChosenOrItRunsInCi()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);

        Assert.IsTrue((await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "vscode").Needed);
        Assert.IsFalse((await CheckAsync(host, new ToolNeeds { Ide = IdeLocator.Rider })).Single(c => c.Tool.Id == "vscode").Needed);

        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        AllInstalled(ci.Runner);

        Assert.IsFalse((await CheckAsync(ci, new ToolNeeds())).Single(c => c.Tool.Id == "vscode").Needed);
    }

    [TestMethod]
    public void DescribeTool_Should_SayWhyAndHow()
    {
        var check = new ToolCheck(ToolCatalog.Find("node")!, new ToolStatus(ToolState.Outdated, "18.0.0", "20 or later is needed"), true, "the build runs npm",
            new ToolAction { ToolId = "node", Title = "Update Node.js to the LTS version", Elevation = Elevation.Sudo, Commands = [] }, null);

        Assert.AreEqual("Update Node.js to the LTS version (found 18.0.0, 20 or later is needed): the build runs npm, needs sudo", Projects.NewWorkflow.DescribeTool(check));
    }

    private static async Task<IReadOnlyList<ToolCheck>> CheckAsync(TestHost host, ToolNeeds needs)
    {
        var context = new ToolContext(host.Environment, host.Runner, needs, PackageManagers.Detect(host.Environment, host.Runner));
        return await ToolCatalog.CheckAsync(context, CancellationToken.None);
    }

    private static void AllInstalled(FakeProcessRunner runner)
    {
        runner.On("git", "--version", 0, "git version 2.47.1");
        runner.On("node", "--version", 0, "v24.9.0");
        runner.On("docker", "version", 0, "28.5.1");
        runner.On("docker", "--version", 0, "Docker version 28.5.1");
        runner.On("aspire", "--version", 0, "13.6.0+abc");
        runner.On("dotnet", "dev-certs", 0);
        runner.On("dotnet", "--list-sdks", 0, "10.0.401 [/usr/share/dotnet/sdk]");
        runner.On("wsl", "--status", 0);
        runner.On("xcodebuild", "-version", 0, "Xcode 26.0");
        runner.Executables["code"] = typeof(ToolTests).Assembly.Location;
        runner.Executables["brew"] = "/opt/homebrew/bin/brew";
    }
}
