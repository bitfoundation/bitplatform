using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class RunTests
{
    private const string Maui = "src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj";

    [TestMethod]
    [DataRow(HostOs.Windows, "ios", "The iOS app runs in the iOS simulator, which needs macOS.")]
    [DataRow(HostOs.Linux, "ios", "The iOS app runs in the iOS simulator, which needs macOS.")]
    [DataRow(HostOs.Windows, "macos", "The macOS app runs on macOS only.")]
    [DataRow(HostOs.MacOS, "windows", "The Windows app runs on Windows only.")]
    [DataRow(HostOs.Linux, "windows", "The Windows app runs on Windows only.")]
    public async Task APlatformThisOsCantRun_Should_BeRefused(HostOs os, string platform, string reason)
    {
        using var host = new TestHost(os);
        CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", platform), host.Output);

        StringAssert.Contains(host.FlatOutput, reason);
        Assert.IsEmpty(host.Runner.Calls);
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task Run_Should_NeedAProjectFolderAndAKnownPlatform()
    {
        using var host = new TestHost(HostOs.Windows);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", "android"));
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");

        CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", "tizen"));
        StringAssert.Contains(host.FlatOutput, "'tizen' isn't a platform");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Run_Should_BuildAndStartTheWebAppTheWayBitNewDoes()
    {
        using var host = new TestHost(HostOs.Windows);
        var directory = CreateProject(host);
        var serverWeb = Directory.CreateDirectory(Path.Combine(directory, "src", "Server", "Contoso.Server.Web", "Properties")).Parent!.FullName;
        File.WriteAllText(Path.Combine(serverWeb, "Properties", "launchSettings.json"), """{ "profiles": { "Contoso.Server.Web": { "commandName": "Project", "applicationUrl": "http://localhost:5030" } } }""");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("run", "-p:Environment=Staging"), host.Output);

        var commands = host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments)).ToList();
        CollectionAssert.AreEqual(new[] { "build Contoso.Web.slnf -p:Environment=Staging", $"run --project {serverWeb} --no-build --property:Environment=Staging" }, commands);
    }

    [TestMethod]
    [DataRow(HostOs.Windows, "x64", "36.0", "system-images;android-36;google_apis;x86_64", "bit_android_36")]
    [DataRow(HostOs.Linux, "arm64", "37.0", "system-images;android-37.0;google_apis;arm64-v8a", "bit_android_37")]
    public async Task WithoutADevice_TheAndroidEmulator_Should_BeInstalledCreatedAndStartedWithoutAutofillOrStylusHandwriting(HostOs os, string architecture, string targetPlatformVersion, string image, string name)
    {
        using var host = new TestHost(os);
        var cli = WithArchitecture(host, architecture is "arm64" ? Architecture.Arm64 : Architecture.X64);
        var project = Project(host, CreateProject(host));
        var sdk = CreateAndroidSdk(host, os, emulator: false);
        var jdk = Directory.CreateDirectory(Path.Combine(host.Root, "jdk")).FullName;
        var emulator = Path.Combine(sdk, "emulator", Exe("emulator", ".exe", os));
        var folder = Path.Combine(host.Home, ".android", "avd", $"{name}.avd");
        AndroidBuildSupport(host, os, sdk, jdk, targetPlatformVersion);
        EmulatorBootsOnceStarted(host);
        host.Runner.On("sdkmanager", $"--sdk_root={sdk} --list", 0, $"""
            Installed packages:
              Path                 | Version | Description                | Location
              -------              | ------- | -------                    | -------
              platform-tools       | 36.0.0  | Android SDK Platform-Tools | platform-tools

            Available Packages:
              Path                 | Version | Description
              -------              | ------- | -------
              emulator             | 36.1.9  | Android Emulator
              {image} | 7       | Google APIs System Image
            """);
        host.Runner.On("sdkmanager", $"--sdk_root={sdk} --install", _ =>
        {
            Touch(emulator);
            Touch(Path.Combine([sdk, "system-images", .. image.Split(';')[1..], "package.xml"]));
            return new ProcessResult { ExitCode = 0 };
        });
        host.Runner.On("avdmanager", "create avd", spec =>
        {
            var path = spec.Arguments[spec.Arguments.ToList().IndexOf("--path") + 1];
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "config.ini"), "hw.device.name=pixel_7\ndisk.dataPartition.size=800M\n");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, $"{name}.ini"), $"path={path}\ntarget={image.Split(';')[1]}\n");
            return new ProcessResult { ExitCode = 0 };
        });
        var steps = new StepRunner(cli);

        await new NativeRun(cli, project) { PollInterval = TimeSpan.Zero }.RunAsync(steps, Platform.Android, CancellationToken.None);

        Assert.IsFalse(steps.AnyFailed, host.Output);

        var install = host.Runner.Calls.Single(c => Tool(c) is "sdkmanager" && c.Arguments.Contains("--install"));
        CollectionAssert.AreEqual(new[] { $"--sdk_root={sdk}", "--install", "emulator", image }, install.Arguments.ToArray());
        Assert.AreEqual(jdk, install.Environment!["JAVA_HOME"]);
        Assert.AreEqual(sdk, install.Environment!["ANDROID_HOME"]);
        StringAssert.StartsWith(install.StandardInput, "y\n");

        var create = host.Runner.Calls.Single(c => Tool(c) is "avdmanager");
        CollectionAssert.AreEqual(new[] { "create", "avd", "--name", name, "--package", image, "--device", "pixel_7", "--path", folder, "--force" }, create.Arguments.ToArray());
        Assert.AreEqual("no\n", create.StandardInput);

        var config = File.ReadAllLines(Path.Combine(folder, "config.ini"));
        CollectionAssert.Contains(config, "hw.device.name=pixel_7");
        CollectionAssert.Contains(config, $"disk.dataPartition.size={8L * 1024 * 1024 * 1024}");
        Assert.IsFalse(config.Contains("disk.dataPartition.size=800M"));

        var started = host.Runner.Detached.Single();
        string[] emulatorArguments = ["-avd", name, "-no-boot-anim"];
        CollectionAssert.AreEqual(os is HostOs.Windows ? emulatorArguments : ["-c", "nohup \"$0\" \"$@\" >/dev/null 2>&1 &", emulator, .. emulatorArguments], started.Arguments.ToArray());
        Assert.AreEqual(os is HostOs.Windows ? emulator : "/bin/sh", started.FileName);

        var adb = host.Runner.Calls.Where(c => Tool(c) is "adb").Select(c => string.Join(' ', c.Arguments)).ToList();
        CollectionAssert.Contains(adb, "-s emulator-5554 shell settings put secure autofill_service null");
        CollectionAssert.Contains(adb, "-s emulator-5554 shell settings put secure stylus_handwriting_enabled 0");

        var run = host.Runner.Calls.Last();
        CollectionAssert.AreEqual(new[] { "build", Maui, "-t:Run", "-f", "net10.0-android", "-p:AdbTarget=-s emulator-5554" }, run.Arguments.ToArray());
        Assert.IsLessThan(host.Runner.Calls.IndexOf(run), host.Runner.Calls.FindLastIndex(c => string.Join(' ', c.Arguments).Contains("stylus_handwriting_enabled", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow("R58M123ABC          device usb:1-1 product:o1sxeea model:SM_G991B device:o1s transport_id:2", "R58M123ABC")]
    [DataRow("emulator-5556       device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:3", "emulator-5556")]
    public async Task AConnectedDeviceOrARunningEmulator_Should_RunTheAppAsItIs(string device, string serial)
    {
        using var host = new TestHost(HostOs.Windows);
        CreateProject(host);
        var sdk = CreateAndroidSdk(host, HostOs.Windows, emulator: true);
        AndroidBuildSupport(host, HostOs.Windows, sdk, null, "36.0");
        host.Runner.On("adb", "devices", 0, $"* daemon not running; starting now at tcp:5037\n* daemon started successfully\nList of devices attached\nemulator-5554          offline\n{device}\n\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("run", "android", "-p:Environment=Staging"), host.Output);

        Assert.IsFalse(host.Runner.Calls.Any(c => Tool(c) is "sdkmanager" or "avdmanager" or "emulator"));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("settings")), "the settings of a device bit didn't start are left alone");
        Assert.IsEmpty(host.Runner.Detached);
        CollectionAssert.AreEqual(new[] { "build", Maui, "-t:Run", "-f", "net10.0-android", $"-p:AdbTarget=-s {serial}", "-p:Environment=Staging" }, host.Runner.Calls.Last().Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, $"Using {serial}");
    }

    [TestMethod]
    public async Task AnExistingVirtualDevice_Should_BeStartedWithoutInstallingOrCreatingAnything()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = Project(host, CreateProject(host));
        var sdk = CreateAndroidSdk(host, HostOs.Windows, emulator: true);
        var avd = Directory.CreateDirectory(Path.Combine(host.Home, ".android", "avd")).FullName;
        WriteVirtualDevice(avd, "Medium_Phone_API_26", "android-26");
        WriteVirtualDevice(avd, "Pixel_9", "android-36");
        AndroidBuildSupport(host, HostOs.Windows, sdk, null, "36.0");
        EmulatorBootsOnceStarted(host);
        var steps = new StepRunner(host.Services);

        await new NativeRun(host.Services, project) { PollInterval = TimeSpan.Zero }.RunAsync(steps, Platform.Android, CancellationToken.None);

        Assert.IsFalse(steps.AnyFailed, host.Output);
        Assert.IsFalse(host.Runner.Calls.Any(c => Tool(c) is "sdkmanager" or "avdmanager"));
        CollectionAssert.AreEqual(new[] { "-avd", "Pixel_9", "-no-boot-anim" }, host.Runner.Detached.Single().Arguments.ToArray());
        Assert.IsTrue(host.Runner.Calls.Any(c => string.Join(' ', c.Arguments) == "-s emulator-5554 shell settings put secure autofill_service null"));
        CollectionAssert.Contains(host.Runner.Calls.Last().Arguments.ToArray(), "-p:AdbTarget=-s emulator-5554");
    }

    [TestMethod]
    public void AVirtualDevice_Should_BeBitsOwnFirstThenOneForTheAppsAndroidVersion()
    {
        var apis = NativeRun.SystemImageApis("36.0");
        VirtualDevice[] devices = [new("Old", "android-26"), new("Pixel_9", "android-36"), new("bit_android_36", "android-35")];

        Assert.AreEqual("bit_android_36", NativeRun.PickVirtualDevice(devices, "bit_android_36", apis));
        Assert.AreEqual("Pixel_9", NativeRun.PickVirtualDevice(devices[..2], "bit_android_36", apis));
        Assert.AreEqual("Old", NativeRun.PickVirtualDevice(devices[..1], "bit_android_36", apis));
        Assert.IsNull(NativeRun.PickVirtualDevice([], "bit_android_36", apis));
        CollectionAssert.AreEqual(new[] { "android-36", "android-36.0" }, apis.ToArray());
        CollectionAssert.AreEqual(new[] { "android-36.1" }, NativeRun.SystemImageApis("36.1").ToArray());
        CollectionAssert.AreEqual(new[] { "android-37.0", "android-37" }, NativeRun.SystemImageApis("37.0").ToArray());
    }

    [TestMethod]
    public async Task AnEmulatorThatDoesntBoot_Should_StopTheWaitAtItsTimeLimit()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = Project(host, CreateProject(host));
        var sdk = CreateAndroidSdk(host, HostOs.Windows, emulator: true);
        WriteVirtualDevice(Directory.CreateDirectory(Path.Combine(host.Home, ".android", "avd")).FullName, "Pixel_9", "android-36");
        AndroidBuildSupport(host, HostOs.Windows, sdk, null, "36.0");
        host.Runner.On("adb", "devices", _ => new ProcessResult { ExitCode = 0, Output = host.Runner.Detached.Count == 0 ? "List of devices attached\n" : "List of devices attached\nemulator-5554\toffline\n" });
        var steps = new StepRunner(host.Services);

        await new NativeRun(host.Services, project) { BootTimeout = TimeSpan.Zero, PollInterval = TimeSpan.Zero }.RunAsync(steps, Platform.Android, CancellationToken.None);

        var boot = steps.Reports.Single(r => r.Id == "android-boot").Result;
        Assert.AreEqual(StepStatus.Failed, boot.Status);
        Assert.AreEqual("The Android emulator didn't start", boot.Title);
        StringAssert.Contains(boot.FollowUp, "-avd Pixel_9");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("-t:Run")));
    }

    [TestMethod]
    [DataRow(HostOs.Windows, "android")]
    [DataRow(HostOs.MacOS, "ios")]
    [DataRow(HostOs.MacOS, "macos")]
    public async Task MissingBuildSupport_Should_LeaveTheInstallingToBitSetup(HostOs os, string platform)
    {
        using var host = new TestHost(os);
        var directory = CreateProject(host);
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.100\n");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("run", platform), host.Output);

        StringAssert.Contains(host.FlatOutput, "This machine can't build the");
        StringAssert.Contains(host.FlatOutput, $"cd {directory} && bit setup --platforms {platform}");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("-t:Run") || c.Arguments.Contains("-t:GetAndroidDependencies") || c.Arguments.Contains("install")));
    }

    [TestMethod]
    public async Task AnAndroidSdkWithoutAdb_Should_LeaveTheInstallingToBitSetup()
    {
        var empty = Directory.CreateTempSubdirectory("bit-cli-tests-sdk-").FullName;

        try
        {
            using var host = new TestHost(HostOs.Windows, new Dictionary<string, string> { ["ANDROID_HOME"] = empty });
            CreateProject(host);
            host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nmaui   10.0.1/10.0.100   SDK 10.0.100\n");
            host.Runner.On("dotnet", $"build {Maui} -t:GetAndroidDependencies", 0, """{ "Properties": { "AndroidSdkDirectory": "", "JavaSdkDirectory": "", "TargetPlatformVersion": "36.0" } }""");

            Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("run", "android"), host.Output);

            StringAssert.Contains(host.FlatOutput, "the Android SDK isn't installed");
            StringAssert.Contains(host.FlatOutput, "bit setup --platforms android");
            Assert.IsFalse(host.Runner.Calls.Any(c => Tool(c) is "adb"));
        }
        finally
        {
            Directory.Delete(empty, recursive: true);
        }
    }

    [TestMethod]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task AnAndroidSdkOnlyAnAdministratorCanChange_Should_AskForAnElevatedTerminalWithoutOne()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Windows);
        var project = Project(host, CreateProject(host));
        var sdk = CreateAndroidSdk(host, HostOs.Windows, emulator: false);
        AndroidBuildSupport(host, HostOs.Windows, sdk, null, "36.0");
        host.Runner.On("adb", "devices", 0, "List of devices attached\n");
        var directory = new DirectoryInfo(sdk);
        var security = directory.GetAccessControl();
        var denied = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.CreateFiles, AccessControlType.Deny);
        security.AddAccessRule(denied);
        directory.SetAccessControl(security);
        var steps = new StepRunner(host.Services);

        try
        {
            await new NativeRun(host.Services, project) { PollInterval = TimeSpan.Zero }.RunAsync(steps, Platform.Android, CancellationToken.None);
        }
        finally
        {
            security.RemoveAccessRule(denied);
            directory.SetAccessControl(security);
        }

        var install = steps.Reports.Single(r => r.Id == "android-emulator").Result;
        Assert.AreEqual("The Android emulator needs administrator rights", install.Title);
        StringAssert.Contains(install.FollowUp, "--install emulator");
        Assert.IsFalse(host.Runner.Calls.Any(c => Tool(c) is "sdkmanager" && c.Arguments.Contains("--install")));
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    [DataRow("ios", "net10.0-ios")]
    [DataRow("macos", "net10.0-maccatalyst")]
    public async Task TheAppleApps_Should_BeBuiltAndStartedByTheMauiProject(string platform, string targetFramework)
    {
        using var host = new TestHost(HostOs.MacOS);
        CreateProject(host);
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nmaui   10.0.1/10.0.100   SDK 10.0.100\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("run", platform, "-p:Environment=Staging"), host.Output);

        CollectionAssert.AreEqual(new[] { "build", Maui, "-t:Run", "-f", targetFramework, "-p:Environment=Staging" }, host.Runner.Calls.Last().Arguments.ToArray());
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task TheWindowsApp_Should_BeBuiltThenStartedOnItsOwn()
    {
        using var host = new TestHost(HostOs.Windows);
        CreateProject(host);
        const string windows = "src/Client/Contoso.Client.Windows/Contoso.Client.Windows.csproj";

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("run", "windows", "-p:Environment=Staging"), host.Output);

        CollectionAssert.AreEqual(new[] { "build", windows, "-p:Environment=Staging" }, host.Runner.Calls.Single(c => c.FileName is "dotnet").Arguments.ToArray());
        var started = host.Runner.Detached.Single();
        Assert.AreEqual("dotnet", started.FileName);
        CollectionAssert.AreEqual(new[] { "run", "--project", windows, "--no-build", "--property:Environment=Staging" }, started.Arguments.ToArray());
        Assert.AreEqual(host.WorkingDirectory, started.WorkingDirectory);
    }

    private static string CreateProject(TestHost host)
    {
        var directory = host.WorkingDirectory;
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, "Contoso.Web.slnf"), "{}");

        foreach (var (app, sdk) in new[] { ("Core", "Microsoft.NET.Sdk.Razor"), ("Maui", "Microsoft.NET.Sdk.Razor"), ("Windows", "Microsoft.NET.Sdk.Razor") })
        {
            var folder = Directory.CreateDirectory(Path.Combine(directory, "src", "Client", $"Contoso.Client.{app}")).FullName;
            File.WriteAllText(Path.Combine(folder, $"Contoso.Client.{app}.csproj"), $"<Project Sdk=\"{sdk}\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        }

        return directory;
    }

    private static ProjectContext Project(TestHost host, string directory) => new() { Name = "Contoso", Directory = directory, Platforms = new HashSet<Platform> { Platform.Web, Platform.Android } };

    private static string CreateAndroidSdk(TestHost host, HostOs os, bool emulator)
    {
        var sdk = Path.Combine(host.Root, "android-sdk");
        Touch(Path.Combine(sdk, "platform-tools", Exe("adb", ".exe", os)));
        Touch(Path.Combine(sdk, "cmdline-tools", "latest", "bin", Exe("sdkmanager", ".bat", os)));
        Touch(Path.Combine(sdk, "cmdline-tools", "latest", "bin", Exe("avdmanager", ".bat", os)));

        if (emulator)
        {
            Touch(Path.Combine(sdk, "emulator", Exe("emulator", ".exe", os)));
        }

        return sdk;
    }

    private static void AndroidBuildSupport(TestHost host, HostOs os, string sdk, string? jdk, string targetPlatformVersion)
    {
        host.Runner.On("dotnet", "workload list", 0, $"Installed Workload Id\n---\n{Platforms.MauiWorkload(os)}   10.0.1/10.0.100   SDK 10.0.100\n");
        host.Runner.On("dotnet", $"build {Maui} -t:GetAndroidDependencies", 0, JsonSerializer.Serialize(new { Properties = new { AndroidSdkDirectory = sdk + Path.DirectorySeparatorChar, JavaSdkDirectory = jdk ?? "", TargetPlatformVersion = targetPlatformVersion } }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void EmulatorBootsOnceStarted(TestHost host)
    {
        host.Runner.On("adb", "devices", _ => new ProcessResult
        {
            ExitCode = 0,
            Output = host.Runner.Detached.Count == 0
                ? "List of devices attached\n\n"
                : "List of devices attached\nemulator-5554          device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:1\n\n"
        });
        host.Runner.On("adb", "-s emulator-5554 shell getprop sys.boot_completed", 0, "1\n");
    }

    private static void WriteVirtualDevice(string avd, string name, string target)
    {
        var folder = Directory.CreateDirectory(Path.Combine(avd, $"{name}.avd")).FullName;
        File.WriteAllText(Path.Combine(avd, $"{name}.ini"), $"avd.ini.encoding=UTF-8\npath={folder}\npath.rel=avd\\{name}.avd\ntarget={target}\n");
    }

    private static CliServices WithArchitecture(TestHost host, Architecture architecture) => new(new CliEnvironment
    {
        Os = host.Environment.Os,
        Architecture = architecture,
        HomeDirectory = host.Home,
        CurrentDirectory = host.WorkingDirectory,
        Variables = host.Environment.Variables,
        IsInputRedirected = true,
        IsOutputRedirected = true
    }, host.Console, runner: host.Runner);

    private static string Exe(string name, string windowsExtension, HostOs os) => os is HostOs.Windows ? name + windowsExtension : name;

    private static string Tool(ProcessSpec spec) => Path.GetFileNameWithoutExtension(spec.FileName);

    private static void Touch(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
    }
}
