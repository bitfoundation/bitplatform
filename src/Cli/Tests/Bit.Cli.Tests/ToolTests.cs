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
    public async Task NodeJs_Should_FollowTheVersionTheTemplateAsksFor()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.On("node", "--version", 0, "v22.11.0");

        var node = (await CheckAsync(host, new ToolNeeds { NodeMajor = 24 })).Single(c => c.Tool.Id == "node");

        Assert.AreEqual(ToolState.Outdated, node.Status.State);
        Assert.AreEqual("24 or later is needed", node.Status.Detail);
        Assert.AreEqual(ToolState.Installed, (await CheckAsync(host, new ToolNeeds { NodeMajor = 22 })).Single(c => c.Tool.Id == "node").Status.State);
    }

    [TestMethod]
    public async Task AnOlderAspireCli_Should_BeUpdatedToTheAppHostsVersion()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        var needs = new ToolNeeds { Aspire = true, AspireVersion = new Version(13, 6, 0) };

        host.Runner.On("aspire", "--version", 0, "13.5.2+5b4c1f0");
        var older = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "aspire");
        Assert.AreEqual(ToolState.Outdated, older.Status.State);
        Assert.IsTrue(older.Needed);
        Assert.AreEqual("Update the Aspire CLI to 13.6.0", older.Action!.Title);
        CollectionAssert.AreEqual(new[] { "tool", "update", "--global", "Aspire.Cli", "--version", "13.6.0" }, older.Action.Commands[0].Arguments.ToArray());

        host.Runner.NotFound("aspire");
        var missing = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "aspire");
        CollectionAssert.AreEqual(new[] { "tool", "install", "--global", "Aspire.Cli", "--version", "13.6.0" }, missing.Action!.Commands[0].Arguments.ToArray());

        host.Runner.On("aspire", "--version", 0, "13.7.0+1a2b3c4");
        Assert.AreEqual(ToolState.Installed, (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "aspire").Status.State);
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
    public async Task TheDotnetSdk_Should_BeWhatDotnetResolvesForTheGlobalJson()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        var needs = new ToolNeeds { Sdk = new SdkRequirement("10.0.100") };
        string? globalJson = null;

        host.Runner.On("dotnet", "--version", spec =>
        {
            globalJson = File.ReadAllText(Path.Combine(spec.WorkingDirectory!, "global.json"));
            return new ProcessResult { ExitCode = 145, Output = "A compatible .NET SDK was not found." };
        });

        var missing = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dotnet-sdk").Status;

        Assert.AreEqual(ToolState.Missing, missing.State);
        Assert.AreEqual("found 10.0.401", missing.Detail);
        Assert.AreEqual("""{"sdk":{"version":"10.0.100"}}""", globalJson);

        host.Runner.On("dotnet", "--version", 0, "10.0.104");
        Assert.AreEqual("10.0.104", (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dotnet-sdk").Status.Version);
    }

    [TestMethod]
    public void AGlobalJson_Should_KeepItsRollForwardRules()
    {
        var sdk = SdkRequirement.FromGlobalJson("{ \"sdk\": { \"version\": \"11.0.100-rc.1.26425.128\", \"rollForward\": \"disable\", \"allowPrerelease\": true } }")!;

        Assert.AreEqual(new SdkRequirement("11.0.100-rc.1.26425.128", "disable", true), sdk);
        Assert.IsTrue(sdk.Pinned);
        Assert.IsTrue(sdk.Preview);
        Assert.AreEqual("11.0", sdk.Channel);
        Assert.AreEqual("""{"sdk":{"version":"11.0.100-rc.1.26425.128","rollForward":"disable","allowPrerelease":true}}""", sdk.GlobalJson);
        Assert.IsNull(SdkRequirement.FromGlobalJson("{ \"sdk\": { } }"));
        Assert.IsNull(SdkRequirement.FromGlobalJson(null));
    }

    [TestMethod]
    public void TheEmbeddedTemplate_Should_CarryTheSdkOfTheTemplatesGlobalJson()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate", "global.json")) is false)
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory);
        var expected = SdkRequirement.FromGlobalJson(File.ReadAllText(Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate", "global.json")));

        Assert.IsNotNull(expected);
        Assert.AreEqual(expected, TemplateRequirements.Embedded.Sdk);
    }

    [TestMethod]
    public async Task TheSdk_Should_InstallItsExactVersionOnMacOSAndLinux()
    {
        var sdk = new SdkRequirement("11.0.100-rc.1.26425.128", "disable");

        using var mac = new TestHost(HostOs.MacOS);
        AllInstalled(mac.Runner);
        mac.Runner.On("dotnet", "--version", 145);
        mac.Runner.Executables["dotnet"] = "/usr/local/share/dotnet/dotnet";
        var package = (await CheckAsync(mac, new ToolNeeds { Sdk = sdk })).Single(c => c.Tool.Id == "dotnet-sdk").Action!;

        Assert.AreEqual(Elevation.Sudo, package.Elevation);
        StringAssert.Contains(package.Commands[0].CommandLine, "https://builds.dotnet.microsoft.com/dotnet/Sdk/11.0.100-rc.1.26425.128/dotnet-sdk-11.0.100-rc.1.26425.128-osx-x64.pkg");
        StringAssert.Contains(package.Commands[0].CommandLine, "pkgutil --check-signature");
        StringAssert.Contains(package.Commands[0].CommandLine, "installer -pkg");

        using var linux = new TestHost(HostOs.Linux);
        AllInstalled(linux.Runner);
        linux.Runner.On("dotnet", "--version", 145);
        linux.Runner.Executables["dotnet"] = "/usr/lib/dotnet/dotnet";
        var system = (await CheckAsync(linux, new ToolNeeds { Sdk = sdk })).Single(c => c.Tool.Id == "dotnet-sdk").Action!;

        Assert.AreEqual(Elevation.Sudo, system.Elevation);
        StringAssert.Contains(system.Commands[0].CommandLine, "https://dot.net/v1/dotnet-install.sh");
        StringAssert.Contains(system.Commands[0].CommandLine, "--version '11.0.100-rc.1.26425.128' --install-dir '/usr/lib/dotnet'");

        linux.Runner.Executables["dotnet"] = Path.Combine(linux.Home, ".dotnet", "dotnet");
        var user = (await CheckAsync(linux, new ToolNeeds { Sdk = sdk })).Single(c => c.Tool.Id == "dotnet-sdk").Action!;

        Assert.AreEqual(Elevation.None, user.Elevation);
        StringAssert.Contains(user.Commands[0].CommandLine, $"--install-dir '{Path.Combine(linux.Home, ".dotnet")}'");
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
    public async Task NodeFromAVersionManager_Should_BeUpdatedThroughIt()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.On("node", "--version", 0, "v22.11.0");
        var needs = new ToolNeeds { NodeMajor = 24 };

        host.Runner.Executables["node"] = "/home/me/.nvm/versions/node/v22.11.0/bin/node";
        var nvm = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "node").Action!;

        Assert.AreEqual("Update Node.js to the LTS version with nvm", nvm.Title);
        Assert.AreEqual(Elevation.None, nvm.Elevation);
        StringAssert.Contains(nvm.Commands.Single().CommandLine, "/nvm.sh' >/dev/null 2>&1; nvm install --lts && nvm alias default 'lts/*'");
        StringAssert.Contains(nvm.PathProbe!.CommandLine, "nvm which default");

        host.Runner.Executables["node"] = "/home/me/.volta/bin/node";
        var volta = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "node").Action!;

        Assert.AreEqual("volta install node@lts", volta.Commands.Single().CommandLine);

        host.Runner.Executables["node"] = "/home/me/.local/state/fnm_multishells/123_456/bin/node";
        var fnm = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "node").Action!;

        CollectionAssert.AreEqual(new[] { "fnm install --lts", "fnm default lts-latest" }, fnm.Commands.Select(c => c.CommandLine).ToArray());
        StringAssert.Contains(fnm.PathProbe!.CommandLine, "--using=lts-latest");
    }

    [TestMethod]
    public async Task NvmWindows_Should_SwitchNodeInTheAdministratorBatch()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows, new Dictionary<string, string> { ["NVM_HOME"] = @"C:\Users\me\AppData\Roaming\nvm", ["NVM_SYMLINK"] = @"C:\Program Files\nodejs" });
        AllInstalled(host.Runner);
        host.Runner.On("node", "--version", 0, "v22.11.0");
        host.Runner.Executables["winget"] = @"C:\winget.exe";
        host.Runner.Executables["node"] = @"C:\Program Files\nodejs\node.exe";

        var action = (await CheckAsync(host, new ToolNeeds { NodeMajor = 24 })).Single(c => c.Tool.Id == "node").Action!;

        Assert.AreEqual(Elevation.Admin, action.Elevation);
        CollectionAssert.AreEqual(new[] { "nvm install lts", "nvm use lts" }, action.Commands.Select(c => c.CommandLine).ToArray());
    }

    [TestMethod]
    public async Task AMissingNode_Should_ComeFromTheVersionManagerTheUserHas()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.NotFound("node");
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        host.Runner.Executables["volta"] = "/home/me/.volta/bin/volta";

        var action = (await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "node").Action!;

        Assert.AreEqual("Install Node.js LTS with Volta", action.Title);
    }

    [TestMethod]
    public async Task TheNodeAVersionManagerInstalls_Should_BeOnThePathRightAway()
    {
        using var host = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "" });
        AllInstalled(host.Runner);
        var installed = Directory.CreateDirectory(Path.Combine(host.Root, "nvm", "v24.9.0", "bin")).FullName;
        host.Runner.On("node", "--version", 0, "v22.11.0");
        host.Runner.Executables["node"] = "/home/me/.nvm/versions/node/v22.11.0/bin/node";
        host.Runner.On("bash", "-c", spec => new ProcessResult { ExitCode = 0, Output = spec.Arguments[1].Contains("nvm which default", StringComparison.Ordinal) ? installed : "" });
        var needs = new ToolNeeds { NodeMajor = 24 };
        var node = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "node");
        var steps = new StepRunner(host.Services);

        await new ToolInstaller(host.Services, steps).InstallAsync([node], new ToolContext(host.Environment, host.Runner, needs, PackageManagers.Detect(host.Environment, host.Runner)), CancellationToken.None);

        Assert.IsFalse(steps.AnyFailed);
        CollectionAssert.Contains(host.Environment.GetVariable("PATH")!.Split(Path.PathSeparator), installed);
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
        var docker = checks.Single(c => c.Tool.Id == "docker").Action!;
        Assert.AreEqual("Install Docker Desktop", docker.Title);
        CollectionAssert.IsSubsetOf(new[] { "Docker.DockerDesktop", "--override", "install --quiet --accept-license" }, docker.Commands[0].Arguments.ToArray());
        CollectionAssert.DoesNotContain(docker.Commands[0].Arguments.ToArray(), "--silent");

        host.Runner.On("dotnet", "--version", 145);
        host.Runner.Executables["dotnet"] = @"C:\Program Files\dotnet\dotnet.exe";
        var stable = (await CheckAsync(host, new ToolNeeds { Sdk = new SdkRequirement("11.0.100") })).Single(c => c.Tool.Id == "dotnet-sdk").Action!;
        var preview = (await CheckAsync(host, new ToolNeeds { Sdk = new SdkRequirement("11.0.100-rc.1.26425.128", "disable") })).Single(c => c.Tool.Id == "dotnet-sdk").Action!;

        Assert.AreEqual(Elevation.Admin, preview.Elevation);
        StringAssert.Contains(stable.Commands[0].CommandLine, "winget install --id Microsoft.DotNet.SDK.11 --exact --version '11.0.100'");
        StringAssert.Contains(preview.Commands[0].CommandLine, "winget install --id Microsoft.DotNet.SDK.Preview --exact --version '11.0.100-rc.1.26425.128'");
        StringAssert.Contains(preview.Commands[0].CommandLine, "dotnet-install.ps1");
        StringAssert.Contains(preview.Commands[0].CommandLine, @"-InstallDir 'C:\Program Files\dotnet'");
    }

    [TestMethod]
    public async Task TheAdministratorLog_Should_KeepEveryLineWhileBitReadsIt()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var directory = Directory.CreateTempSubdirectory("bit-cli-admin-log-").FullName;
        var log = Path.Combine(directory, "install.log");
        var scriptPath = Path.Combine(directory, "install.ps1");
        var action = new ToolAction
        {
            ToolId = "chatty",
            Title = "Print a lot",
            Elevation = Elevation.Admin,
            Commands = [new ProcessSpec { FileName = "cmd.exe", Arguments = ["/d", "/c", "for /L %i in (1,1,200) do @echo line %i"], Timeout = TimeSpan.FromSeconds(60) }]
        };
        var script = System.Text.Encoding.UTF8.GetBytes(ToolInstaller.BuildWindowsScript([action], log, Path.Combine(directory, "results.json"), Path.Combine(directory, "cancel")));
        File.WriteAllBytes(scriptPath, script);

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -EncodedCommand {ToolInstaller.EncodeBootstrap(scriptPath, script)}") { UseShellExecute = false, CreateNoWindow = true })!;

        while (process.HasExited is false)
        {
            if (File.Exists(log))
            {
                using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                await reader.ReadToEndAsync();
            }

            await Task.Delay(5);
        }

        var lines = File.ReadAllLines(log);
        Assert.AreEqual("== Print a lot", lines[0]);
        Assert.AreEqual(201, lines.Length, string.Join(Environment.NewLine, lines.Take(5)));
        Assert.AreEqual("line 200", lines[^1].Trim());
        Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    public void TheAdministratorLog_Should_TellWhichStepRunsAndWhatAFailedOneSaid()
    {
        var log = "== Install Git\r\nFound Git [Git.Git]\r\n  -\b\\\b|\r\nSuccessfully installed\n== Install WSL\nDownloading: 10%\r Downloading: 55%\n";

        Assert.AreEqual("2 of 7: Install WSL · Downloading: 55%", ToolInstaller.AdministratorProgress(log, 7));
        Assert.AreEqual("1 of 3: Install Git", ToolInstaller.AdministratorProgress("== Install Git\n", 3));
        Assert.IsNull(ToolInstaller.AdministratorProgress("", 3));
        Assert.AreEqual("Successfully installed", ToolInstaller.AdministratorOutput(log, "Install Git"));
        Assert.AreEqual("Downloading: 55%", ToolInstaller.AdministratorOutput(log, "Install WSL"));
        Assert.IsNull(ToolInstaller.AdministratorOutput(log, "Install Docker Desktop"));
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

        var script = ToolInstaller.BuildWindowsScript(actions, @"C:\Temp\install.log", @"C:\Temp\results.json", @"C:\Temp\cancel");

        StringAssert.Contains(script, "& 'reg.exe' 'add' 'HKLM\\SYSTEM\\x' '/d' 'it''s 1'");
        StringAssert.Contains(script, "$results['long-paths'] = $code");
        StringAssert.Contains(script, "$results['node'] = $code");
        StringAssert.Contains(script, "@(0, 3010) -contains $code");
        StringAssert.Contains(script, "Start-BitWatchdog 1800");
        StringAssert.Contains(script, "$cancelPath = 'C:\\Temp\\cancel'");
        StringAssert.Contains(script, "Set-Content -LiteralPath 'C:\\Temp\\results.json'");
    }

    [TestMethod]
    public async Task TheAdministratorScript_Should_StopACommandAtItsTimeoutAndHonorCancel()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var directory = Directory.CreateTempSubdirectory("bit-cli-admin-").FullName;
        var log = Path.Combine(directory, "install.log");
        var results = Path.Combine(directory, "results.json");
        var cancel = Path.Combine(directory, "cancel");
        var scriptPath = Path.Combine(directory, "install.ps1");

        ToolAction Action(string id, string arguments, int seconds) => new()
        {
            ToolId = id,
            Title = id,
            Elevation = Elevation.Admin,
            Commands = [new ProcessSpec { FileName = "cmd.exe", Arguments = ["/d", "/c", arguments], Timeout = TimeSpan.FromSeconds(seconds) }]
        };

        async Task<string> RunAsync(params ToolAction[] actions)
        {
            var script = System.Text.Encoding.UTF8.GetBytes(ToolInstaller.BuildWindowsScript(actions, log, results, cancel));
            File.WriteAllBytes(scriptPath, script);

            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -EncodedCommand {ToolInstaller.EncodeBootstrap(scriptPath, script)}") { UseShellExecute = false, CreateNoWindow = true })!;
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
            return File.ReadAllText(results);
        }

        var timed = await RunAsync(Action("quick", "exit 0", 60), Action("slow", "ping -n 60 127.0.0.1 >nul", 3));

        StringAssert.Contains(timed, "\"quick\":  0");
        StringAssert.Contains(timed, $"\"slow\":  {ToolInstaller.TimedOutCode}");

        File.WriteAllText(cancel, "cancel");
        var cancelled = await RunAsync(Action("skipped", "exit 0", 60));

        StringAssert.Contains(cancelled, $"\"skipped\":  {ToolInstaller.CancelledCode}");

        var original = System.Text.Encoding.UTF8.GetBytes(ToolInstaller.BuildWindowsScript([Action("quick", "exit 0", 60)], log, results, cancel));
        File.WriteAllBytes(scriptPath, original);
        var bootstrap = ToolInstaller.EncodeBootstrap(scriptPath, original);
        var marker = Path.Combine(directory, "tampered");
        File.WriteAllText(scriptPath, $"Set-Content -LiteralPath '{marker}' -Value 'ran'");

        using (var tampered = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -EncodedCommand {bootstrap}") { UseShellExecute = false, CreateNoWindow = true })!)
        {
            await tampered.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(1));
            Assert.AreEqual(87, tampered.ExitCode);
        }

        Assert.IsFalse(File.Exists(marker));
        Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    public void TheBootstrap_Should_RunOnlyTheScriptItWasMadeFor()
    {
        var script = System.Text.Encoding.UTF8.GetBytes("Write-Output 'hi'");
        var bootstrap = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(ToolInstaller.EncodeBootstrap(@"C:\Temp\it's\install.ps1", script)));

        StringAssert.Contains(bootstrap, @"[IO.File]::ReadAllBytes('C:\Temp\it''s\install.ps1')");
        StringAssert.Contains(bootstrap, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(script)));
        StringAssert.Contains(bootstrap, "exit 87");
        Assert.DoesNotContain("ExecutionPolicy", bootstrap);
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
    public async Task TheVirtualMachinePlatform_Should_BeTurnedOnUntilDockerRuns()
    {
        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);
        var needs = new ToolNeeds { Aspire = true };
        const string probe = "-NoProfile -NonInteractive -Command (Get-CimInstance Win32_OptionalFeature -Filter \"Name='VirtualMachinePlatform'\")";

        host.Runner.On("powershell", probe, 0, "2\r\n");
        Assert.IsFalse((await CheckAsync(host, needs)).Any(c => c.Tool.Id == "virtual-machine-platform"));

        host.Runner.NotFound("docker");
        var platform = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "virtual-machine-platform");
        Assert.IsTrue(platform.Needed);
        Assert.AreEqual(Elevation.Admin, platform.Action!.Elevation);
        CollectionAssert.Contains(platform.Action.Commands[0].Arguments.ToArray(), "/featurename:VirtualMachinePlatform");

        host.Runner.On("powershell", probe, 0, "1\r\n");
        Assert.IsTrue((await CheckAsync(host, needs)).Single(c => c.Tool.Id == "virtual-machine-platform").Status.IsSatisfied);

        Assert.IsFalse((await CheckAsync(host, new ToolNeeds())).Any(c => c.Tool.Id == "virtual-machine-platform"));
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
    public async Task VsCode_Should_BeNeededUnlessAnotherIdeIsChosenOrItRunsInCiOrADevContainer()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);

        Assert.IsTrue((await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "vscode").Needed);
        Assert.IsFalse((await CheckAsync(host, new ToolNeeds { Ide = IdeLocator.Rider })).Single(c => c.Tool.Id == "vscode").Needed);

        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        AllInstalled(ci.Runner);

        Assert.IsFalse((await CheckAsync(ci, new ToolNeeds())).Single(c => c.Tool.Id == "vscode").Needed);

        foreach (var variable in new[] { "REMOTE_CONTAINERS", "CODESPACES", "DEVCONTAINER" })
        {
            using var container = new TestHost(HostOs.Linux, new Dictionary<string, string> { [variable] = "true" });
            AllInstalled(container.Runner);

            Assert.IsFalse((await CheckAsync(container, new ToolNeeds())).Single(c => c.Tool.Id == "vscode").Needed, variable);
        }
    }

    [TestMethod]
    public async Task Ci_Should_LeaveDockerWslAndTheAspireCliAloneButTrustTheCertificateOnLinux()
    {
        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        AllInstalled(ci.Runner);
        ci.Runner.NotFound("docker");
        ci.Runner.NotFound("aspire");
        ci.Runner.On("dotnet", "dev-certs", 1);

        var inCi = await CheckAsync(ci, new ToolNeeds { Aspire = true });

        Assert.IsFalse(inCi.Any(c => c.Tool.Id is "docker" or "aspire" or "wsl"), string.Join(", ", inCi.Select(c => c.Tool.Id)));
        Assert.IsTrue(inCi.Single(c => c.Tool.Id == "dev-cert").Needed);

        using var local = new TestHost(HostOs.Linux);
        AllInstalled(local.Runner);
        local.Runner.NotFound("docker");
        local.Runner.On("dotnet", "dev-certs", 1);

        var onLinux = await CheckAsync(local, new ToolNeeds { Aspire = true });

        Assert.IsTrue(onLinux.Single(c => c.Tool.Id == "docker").Needed);
        Assert.IsTrue(onLinux.Single(c => c.Tool.Id == "dev-cert").Action!.Optional);

        using var windowsCi = new TestHost(HostOs.Windows, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        AllInstalled(windowsCi.Runner);
        windowsCi.Runner.On("dotnet", "dev-certs", 1);

        Assert.IsFalse((await CheckAsync(windowsCi, new ToolNeeds { Aspire = true })).Any(c => c.Tool.Id is "dev-cert" or "docker" or "wsl" or "aspire"));
    }

    [TestMethod]
    public async Task TheGitHubCli_Should_OnlyBeCheckedForAGitHubRepository()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        host.Runner.Executables["sudo"] = "/usr/bin/sudo";
        host.Runner.NotFound("gh");

        Assert.IsFalse((await CheckAsync(host, new ToolNeeds())).Any(c => c.Tool.Id == "gh"));

        var gh = (await CheckAsync(host, new ToolNeeds { GitHubRepo = true })).Single(c => c.Tool.Id == "gh");

        Assert.IsTrue(gh.Needed);
        CollectionAssert.Contains(gh.Action!.Commands.Select(c => c.CommandLine).ToList(), "apt-get install -y gh");
    }

    [TestMethod]
    public void DescribeTool_Should_SayWhyAndHow()
    {
        var check = new ToolCheck(ToolCatalog.Find("node")!, new ToolStatus(ToolState.Outdated, "18.0.0", "20 or later is needed"), true, "the build runs npm",
            new ToolAction { ToolId = "node", Title = "Update Node.js to the LTS version", Elevation = Elevation.Sudo, Commands = [] }, null);

        Assert.AreEqual("Update Node.js to the LTS version (found 18.0.0, 20 or later is needed): the build runs npm, needs sudo", Projects.NewWorkflow.DescribeTool(check));
    }

    [TestMethod]
    public async Task TheDevelopmentCertificate_Should_BeNeededWithOrWithoutAspire()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.On("dotnet", "dev-certs", 1);

        Assert.IsTrue((await CheckAsync(host, new ToolNeeds { Aspire = false })).Single(c => c.Tool.Id == "dev-cert").Needed);
        Assert.IsTrue((await CheckAsync(host, new ToolNeeds { Aspire = true })).Single(c => c.Tool.Id == "dev-cert").Needed);
    }

    [TestMethod]
    public async Task WindowsLongPaths_Should_BeNeededForEveryProject()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        AllInstalled(host.Runner);

        Assert.IsTrue((await CheckAsync(host, new ToolNeeds())).Single(c => c.Tool.Id == "long-paths").Needed);
    }

    [TestMethod]
    public async Task AnOptionalToolThatFails_Should_OnlyWarn()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.On("dotnet", "dev-certs https --check", 1);
        host.Runner.On("dotnet", "dev-certs https --trust", 1, "There was an error trusting the HTTPS developer certificate.");
        var needs = new ToolNeeds { Aspire = true };
        var devCert = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dev-cert");
        var steps = new StepRunner(host.Services);

        await new ToolInstaller(host.Services, steps).InstallAsync([devCert], new ToolContext(host.Environment, host.Runner, needs, PackageManagers.Detect(host.Environment, host.Runner)), CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, steps.Reports.Single().Result.Status);
        Assert.IsFalse(steps.AnyFailed);
    }

    [TestMethod]
    public async Task ACertificateTrustedForSomeClientsOnLinux_Should_SucceedWithANote()
    {
        using var host = new TestHost(HostOs.Linux);
        AllInstalled(host.Runner);
        host.Runner.On("dotnet", "dev-certs https --check", 1);
        host.Runner.On("dotnet", "dev-certs https --trust", 4, "There was an error trusting the HTTPS developer certificate. It will be trusted by some clients but not by others.");
        var needs = new ToolNeeds { Aspire = true };
        var devCert = (await CheckAsync(host, needs)).Single(c => c.Tool.Id == "dev-cert");
        var steps = new StepRunner(host.Services);

        await new ToolInstaller(host.Services, steps).InstallAsync([devCert], new ToolContext(host.Environment, host.Runner, needs, PackageManagers.Detect(host.Environment, host.Runner)), CancellationToken.None);

        var result = steps.Reports.Single().Result;
        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        Assert.AreEqual("for some clients", result.Detail);
        Assert.IsNull(result.FollowUp);
        StringAssert.Contains(host.Output, "SSL_CERT_DIR");
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
