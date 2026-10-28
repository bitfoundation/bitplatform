using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tools;

namespace Bit.Cli.Projects;

public sealed record AndroidDevice(string Serial, string State, string? Model)
{
    public bool IsReady => State is "device";

    public bool IsEmulator => Serial.StartsWith("emulator-", StringComparison.Ordinal);
}

public sealed record VirtualDevice(string Name, string? Target);

public sealed class NativeRun(CliServices cli, ProjectContext project)
{
    public const string DeviceProfile = "pixel_7";

    public const long StorageBytes = 8L * 1024 * 1024 * 1024;

    public static readonly IReadOnlyList<(string Name, string Value)> SystemSheetSettings = [("autofill_service", "null"), ("stylus_handwriting_enabled", "0")];

    private IProcessRunner Runner => cli.Runner;

    private string? sdk;

    private string? javaHome;

    private string? targetPlatformVersion;

    private string? serial;

    private string? virtualDevice;

    public TimeSpan BootTimeout { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(3);

    public async Task RunAsync(StepRunner steps, Platform platform, CancellationToken cancellationToken)
    {
        var title = Platforms.Title(platform);

        if (platform is Platform.Windows)
        {
            await steps.RunAsync("run", "Building and starting the Windows app", WindowsAppAsync, cancellationToken);
            return;
        }

        if (await StepAsync(steps, "build-support", $"Checking {title} build support", (progress, ct) => BuildSupportAsync(platform, progress, ct), cancellationToken) is false)
            return;

        if (platform is Platform.Android && await AndroidDeviceAsync(steps, cancellationToken) is false)
            return;

        await steps.RunAsync("run", $"Building and starting the {title} app", (progress, ct) => MauiAppAsync(platform, progress, ct), cancellationToken);
    }

    private async Task<bool> AndroidDeviceAsync(StepRunner steps, CancellationToken cancellationToken)
    {
        if (await StepAsync(steps, "android-device", "Looking for an Android device", FindDeviceAsync, cancellationToken) is false)
            return false;

        if (serial is not null)
            return true;

        virtualDevice = PickVirtualDevice(ListVirtualDevices(VirtualDevicesDirectory), OwnVirtualDevice, SystemImageApis(targetPlatformVersion));

        if (File.Exists(EmulatorPath) is false || (virtualDevice is null && InstalledSystemImage() is null))
        {
            await AdministratorNeeds.StartAsync(cli, NeedsAdministrator && ToolInstaller.CanRunAsAdministrator(cli) ? ["the Android emulator"] : [], cancellationToken);

            try
            {
                if (await StepAsync(steps, "android-emulator", "Installing the Android emulator", InstallEmulatorAsync, cancellationToken) is false)
                    return false;
            }
            finally
            {
                await AdministratorNeeds.EndAsync(cli);
            }
        }

        if (virtualDevice is null && await StepAsync(steps, "android-virtual-device", "Creating an Android virtual device", CreateVirtualDeviceAsync, cancellationToken) is false)
            return false;

        return await StepAsync(steps, "android-boot", "Starting the Android emulator", StartEmulatorAsync, cancellationToken);
    }

    private static async Task<bool> StepAsync(StepRunner steps, string id, string title, Func<Action<string>, CancellationToken, Task<StepResult>> work, CancellationToken cancellationToken)
    {
        return (await steps.RunAsync(id, title, work, cancellationToken)).Status is not StepStatus.Failed;
    }

    public async Task<StepResult> BuildSupportAsync(Platform platform, Action<string> progress, CancellationToken cancellationToken)
    {
        var title = Platforms.Title(platform);
        var missing = $"This machine can't build the {title} app yet";
        var followUp = $"cd {ProcessSpec.Quote(project.Directory)} && bit setup --platforms {Platforms.Name(platform)}";
        var list = await Dotnet(["workload", "list"], progress, TimeSpan.FromMinutes(2), cancellationToken);

        if (list.Succeeded && ProjectSteps.ParseInstalledWorkloads(list.Output).Contains(Platforms.MauiWorkload(cli.Environment.Os)) is false)
            return StepResult.Failed(missing, $"its {title} build support isn't installed", followUp, resultCode: "build-support.missing");

        if (platform is not Platform.Android)
            return StepResult.Succeeded($"{title} build support is installed");

        string[] arguments = ["build", Platforms.MauiProject(project.Name), "-t:GetAndroidDependencies", "-f", TargetFramework(platform), "-getProperty:AndroidSdkDirectory", "-getProperty:JavaSdkDirectory", "-getProperty:TargetPlatformVersion"];
        var probe = await Dotnet(arguments, progress, TimeSpan.FromMinutes(30), cancellationToken);

        if (probe.Succeeded is false)
            return StepResult.FromProcess(probe, "", missing, followUp);

        var properties = ReadMSBuildProperties(probe.Output);

        sdk = ExistingDirectory(properties.GetValueOrDefault("AndroidSdkDirectory")) ?? AdministratorNeeds.AndroidSdkDirectory(cli.Environment);
        javaHome = ExistingDirectory(properties.GetValueOrDefault("JavaSdkDirectory"));
        targetPlatformVersion = properties.GetValueOrDefault("TargetPlatformVersion");

        if (sdk is null || File.Exists(AdbPath) is false)
            return StepResult.Failed(missing, "the Android SDK isn't installed", followUp, resultCode: "android-sdk.missing");

        return StepResult.Succeeded("Android build support is installed", ApiName(targetPlatformVersion) is { } api ? $"API level {api}, the SDK in {sdk}" : $"the SDK in {sdk}");
    }

    public async Task<StepResult> FindDeviceAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var devices = await Adb(["devices", "-l"], cancellationToken);

        if (devices.Succeeded is false)
            return StepResult.FromProcess(devices, "", "Couldn't list the Android devices", $"{ProcessSpec.Quote(AdbPath)} devices -l");

        if (ParseDevices(devices.Output).FirstOrDefault(d => d.IsReady) is not { } device)
            return StepResult.Skipped("No Android device is connected or running", "bit starts the Android emulator");

        serial = device.Serial;
        return StepResult.Succeeded($"Using {device.Serial}", device.Model?.Replace('_', ' '));
    }

    public async Task<StepResult> InstallEmulatorAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string failed = "Couldn't install the Android emulator";
        var setup = $"cd {ProcessSpec.Quote(project.Directory)} && bit setup --platforms android";

        if (CommandLineTool("sdkmanager") is not { } sdkManager)
            return StepResult.Failed(failed, "the Android SDK has no command-line tools", setup, resultCode: "android-sdk.tools-missing");

        var packages = new List<string>();

        if (File.Exists(EmulatorPath) is false)
        {
            packages.Add("emulator");
        }

        if (virtualDevice is null && InstalledSystemImage() is null)
        {
            var apis = SystemImageApis(targetPlatformVersion);

            if (apis.Count == 0)
                return StepResult.Failed(failed, "the Android version the app targets is unknown", setup);

            var listed = await Runner.RunAsync(SdkTool(sdkManager, [$"--sdk_root={sdk}", "--list"], TimeSpan.FromMinutes(5)) with { OnOutputLine = progress }, cancellationToken);
            var available = ParseSdkPackages(listed.Output);

            packages.Add(apis.Select(SystemImage).FirstOrDefault(available.Contains) ?? SystemImage(apis[0]));
        }

        var install = SdkTool(sdkManager, [$"--sdk_root={sdk}", "--install", .. packages], TimeSpan.FromMinutes(60)) with { StandardInput = string.Concat(Enumerable.Repeat("y\n", 20)), OnOutputLine = progress };
        var followUp = install.CommandLine;
        ProcessResult result;

        if (NeedsAdministrator)
        {
            if (cli.Administrator is not { } administrator)
                return cli.AdministratorDeclined
                    ? StepResult.Failed("The Android emulator wasn't installed", "administrator permission was declined", followUp)
                    : StepResult.Failed("The Android emulator needs administrator rights", $"{sdk} is read-only for your user; run it in an elevated terminal", followUp);

            result = await administrator.RunAsync(install, cancellationToken);
        }
        else
        {
            result = await Runner.RunAsync(install, cancellationToken);
        }

        if (result.Succeeded && (File.Exists(EmulatorPath) is false || (virtualDevice is null && InstalledSystemImage() is null)))
            return StepResult.Failed(failed, "sdkmanager ended without installing it; a license may not be accepted yet", $"{ProcessSpec.Quote(sdkManager)} --licenses");

        return StepResult.FromProcess(result, "Installed the Android emulator", failed, followUp, string.Join(", ", packages));
    }

    public async Task<StepResult> CreateVirtualDeviceAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        const string failed = "Couldn't create an Android virtual device";

        if (CommandLineTool("avdmanager") is not { } avdManager)
            return StepResult.Failed(failed, "the Android SDK has no command-line tools", $"cd {ProcessSpec.Quote(project.Directory)} && bit setup --platforms android", resultCode: "android-sdk.tools-missing");

        if (InstalledSystemImage() is not { } image)
            return StepResult.Failed(failed, "its system image isn't installed");

        var name = OwnVirtualDevice;
        var folder = Path.Combine(VirtualDevicesDirectory, $"{name}.avd");
        var create = SdkTool(avdManager, ["create", "avd", "--name", name, "--package", image, "--device", DeviceProfile, "--path", folder, "--force"], TimeSpan.FromMinutes(5)) with { StandardInput = "no\n", OnOutputLine = progress };
        var result = await Runner.RunAsync(create, cancellationToken);

        if (result.Succeeded is false)
            return StepResult.FromProcess(result, "", failed, create.CommandLine);

        SetStorage(Path.Combine(folder, "config.ini"), StorageBytes);
        virtualDevice = name;

        return StepResult.Succeeded($"Created the {name} virtual device", $"{image}, {StorageBytes / (1024 * 1024 * 1024)} GB of storage");
    }

    public async Task<StepResult> StartEmulatorAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var name = virtualDevice!;
        string[] arguments = ["-avd", name, "-no-boot-anim"];
        var followUp = $"{ProcessSpec.Quote(EmulatorPath)} {string.Join(' ', arguments)}";
        var start = cli.Environment.IsWindows
            ? new ProcessSpec { FileName = EmulatorPath, Arguments = arguments, Environment = SdkEnvironment }
            : new ProcessSpec { FileName = "/bin/sh", Arguments = ["-c", "nohup \"$0\" \"$@\" >/dev/null 2>&1 &", EmulatorPath, .. arguments], Environment = SdkEnvironment };

        if (Runner.StartDetached(start) is false)
            return StepResult.Failed("Couldn't start the Android emulator", $"{EmulatorPath} didn't start", followUp);

        progress($"Waiting for {name} to boot");

        if (await WaitForBootAsync(cancellationToken) is not { } booted)
            return StepResult.Failed("The Android emulator didn't start", $"{name} hadn't booted after {BootTimeout.TotalMinutes:0} minutes", followUp, hint: "Run that command to see what the emulator says.");

        serial = booted;
        var settings = SystemSheetSettings.Select(s => new[] { "-s", booted, "shell", "settings", "put", "secure", s.Name, s.Value }).ToList();
        var turnedOff = true;

        foreach (var setting in settings)
        {
            turnedOff &= (await Adb(setting, cancellationToken)).Succeeded;
        }

        return turnedOff
            ? StepResult.Succeeded($"Started {name}", $"{booted}, autofill and stylus handwriting turned off")
            : StepResult.Warning($"Started {name}", $"{booted}, but autofill and stylus handwriting are still on",
                string.Join(" && ", settings.Select(s => $"{ProcessSpec.Quote(AdbPath)} {string.Join(' ', s)}")),
                "Google's \"Save password?\" and the keyboard's stylus sheets can cover the app and take Back presses.");
    }

    private async Task<string?> WaitForBootAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + BootTimeout;

        while (true)
        {
            var devices = await Adb(["devices", "-l"], cancellationToken);

            foreach (var device in ParseDevices(devices.Output).Where(d => d.IsEmulator && d.IsReady))
            {
                var booted = await Adb(["-s", device.Serial, "shell", "getprop", "sys.boot_completed"], cancellationToken);

                if (booted.Succeeded && booted.Output.Trim() == "1")
                    return device.Serial;
            }

            if (DateTime.UtcNow >= deadline)
                return null;

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    public async Task<StepResult> MauiAppAsync(Platform platform, Action<string> progress, CancellationToken cancellationToken)
    {
        var title = Platforms.Title(platform);
        string[] arguments = ["build", Platforms.MauiProject(project.Name), "-t:Run", "-f", TargetFramework(platform), .. serial is null ? Array.Empty<string>() : [$"-p:AdbTarget=-s {serial}"], .. project.BuildProperties.Select(p => $"-p:{p}")];
        var result = await Dotnet(arguments, progress, TimeSpan.FromMinutes(60), cancellationToken);

        return StepResult.FromProcess(result, $"Started the {title} app", $"Couldn't build and start the {title} app",
            $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}", serial is null ? null : $"on {serial}");
    }

    public async Task<StepResult> WindowsAppAsync(Action<string> progress, CancellationToken cancellationToken)
    {
        var windowsProject = Platforms.WindowsProject(project.Name);
        string[] arguments = ["build", windowsProject, .. project.BuildProperties.Select(p => $"-p:{p}")];
        var built = await Dotnet(arguments, progress, TimeSpan.FromMinutes(60), cancellationToken);

        if (built.Succeeded is false)
            return StepResult.FromProcess(built, "", "Couldn't build the Windows app", $"cd {ProcessSpec.Quote(project.Directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}");

        var run = new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["run", "--project", windowsProject, "--no-build", .. project.BuildProperties.Select(p => $"--property:{p}")],
            WorkingDirectory = project.Directory,
            Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1" }
        };

        return Runner.StartDetached(run)
            ? StepResult.Succeeded("Started the Windows app")
            : StepResult.Failed("Couldn't start the Windows app", "dotnet didn't start", $"cd {ProcessSpec.Quote(project.Directory)} && {run.CommandLine}");
    }

    private string TargetFramework(Platform platform) => $"{project.TargetFrameworkVersion}-{Platforms.MauiTarget(platform)}";

    private string Abi => cli.Environment.Architecture is Architecture.Arm64 ? "arm64-v8a" : "x86_64";

    private string OwnVirtualDevice => $"bit_android_{ApiName(targetPlatformVersion) ?? "app"}";

    private string VirtualDevicesDirectory => cli.Environment.GetVariable("ANDROID_AVD_HOME")
        ?? Path.Combine(cli.Environment.GetVariable("ANDROID_USER_HOME") ?? cli.Environment.GetVariable("ANDROID_EMULATOR_HOME") ?? Path.Combine(cli.Environment.HomeDirectory, ".android"), "avd");

    private bool NeedsAdministrator => cli.Environment.IsWindows && cli.Environment.IsElevated is false && AdministratorNeeds.CanWrite(sdk!) is false;

    private string AdbPath => Executable(Path.Combine(sdk!, "platform-tools"), "adb", ".exe");

    private string EmulatorPath => Executable(Path.Combine(sdk!, "emulator"), "emulator", ".exe");

    private string Executable(string directory, string name, string windowsExtension) => Path.Combine(directory, cli.Environment.IsWindows ? name + windowsExtension : name);

    private string? CommandLineTool(string name)
    {
        var tools = Path.Combine(sdk!, "cmdline-tools");
        var versions = Directory.Exists(tools)
            ? Directory.EnumerateDirectories(tools).Where(d => Path.GetFileName(d) is not "latest").OrderByDescending(d => Version.TryParse(Path.GetFileName(d), out var version) ? version : new Version(0, 0))
            : Enumerable.Empty<string>();

        return new[] { Path.Combine(tools, "latest") }.Concat(versions).Select(d => Path.Combine(d, "bin")).Append(Path.Combine(sdk!, "tools", "bin"))
            .Select(bin => Executable(bin, name, ".bat"))
            .FirstOrDefault(File.Exists);
    }

    private string SystemImage(string api) => $"system-images;{api};google_apis;{Abi}";

    private string? InstalledSystemImage()
    {
        return SystemImageApis(targetPlatformVersion)
            .Where(api => File.Exists(Path.Combine(sdk!, "system-images", api, "google_apis", Abi, "package.xml")) || File.Exists(Path.Combine(sdk!, "system-images", api, "google_apis", Abi, "source.properties")))
            .Select(SystemImage)
            .FirstOrDefault();
    }

    private Dictionary<string, string?> SdkEnvironment
    {
        get
        {
            var environment = new Dictionary<string, string?> { ["ANDROID_HOME"] = sdk, ["ANDROID_SDK_ROOT"] = sdk };

            if (javaHome is not null)
            {
                environment["JAVA_HOME"] = javaHome;
            }

            return environment;
        }
    }

    private ProcessSpec SdkTool(string fileName, string[] arguments, TimeSpan timeout) => new() { FileName = fileName, Arguments = arguments, Timeout = timeout, Environment = SdkEnvironment };

    private Task<ProcessResult> Adb(string[] arguments, CancellationToken cancellationToken)
    {
        return Runner.RunAsync(new ProcessSpec { FileName = AdbPath, Arguments = arguments, Timeout = TimeSpan.FromMinutes(1) }, cancellationToken);
    }

    private Task<ProcessResult> Dotnet(string[] arguments, Action<string> progress, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = project.Directory,
            OnOutputLine = progress,
            Timeout = timeout,
            Environment = new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["MSBUILDTERMINALLOGGER"] = "off", ["DOTNET_CLI_UI_LANGUAGE"] = "en" }
        }, cancellationToken);
    }

    private static string? ExistingDirectory(string? path) => string.IsNullOrWhiteSpace(path) || Directory.Exists(path) is false ? null : Path.TrimEndingDirectorySeparator(path);

    public static string? ApiName(string? targetPlatformVersion) => Version.TryParse(targetPlatformVersion, out var version)
        ? version.Minor is 0 or -1 ? $"{version.Major}" : $"{version.Major}.{version.Minor}"
        : null;

    public static IReadOnlyList<string> SystemImageApis(string? targetPlatformVersion) => Version.TryParse(targetPlatformVersion, out var version)
        ? version.Minor is not (0 or -1) ? [$"android-{version.Major}.{version.Minor}"]
            : version.Major >= 37 ? [$"android-{version.Major}.0", $"android-{version.Major}"] : [$"android-{version.Major}", $"android-{version.Major}.0"]
        : [];

    public static string? PickVirtualDevice(IReadOnlyList<VirtualDevice> devices, string own, IReadOnlyList<string> apis)
    {
        return devices.FirstOrDefault(d => d.Name == own)?.Name
            ?? devices.FirstOrDefault(d => d.Target is { } target && apis.Contains(target))?.Name
            ?? devices.FirstOrDefault()?.Name;
    }

    public static IReadOnlyList<VirtualDevice> ListVirtualDevices(string directory)
    {
        if (Directory.Exists(directory) is false)
            return [];

        return [.. Directory.EnumerateFiles(directory, "*.ini")
            .Select(ini => (Name: Path.GetFileNameWithoutExtension(ini), Values: ReadIni(ini)))
            .Where(avd => Directory.Exists(avd.Values.GetValueOrDefault("path") ?? Path.Combine(directory, $"{avd.Name}.avd")))
            .OrderBy(avd => avd.Name, StringComparer.OrdinalIgnoreCase)
            .Select(avd => new VirtualDevice(avd.Name, avd.Values.GetValueOrDefault("target")))];
    }

    public static IReadOnlyList<AndroidDevice> ParseDevices(string output)
    {
        return [.. output.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && line.StartsWith('*') is false && line.StartsWith("List of devices", StringComparison.Ordinal) is false)
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2)
            .Select(parts => new AndroidDevice(parts[0], parts[1], parts.Skip(2).FirstOrDefault(p => p.StartsWith("model:", StringComparison.Ordinal))?["model:".Length..]))];
    }

    public static IReadOnlySet<string> ParseSdkPackages(string output)
    {
        return output.Split('\n')
            .Where(line => line.Contains('|'))
            .Select(line => line.Split('|')[0].Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, string> ReadMSBuildProperties(string output)
    {
        var properties = output.IndexOf("\"Properties\"", StringComparison.Ordinal);
        var start = properties < 0 ? -1 : output.LastIndexOf('{', properties);

        if (start < 0)
            return new Dictionary<string, string>();

        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(output[start..]));
            using var document = JsonDocument.ParseValue(ref reader);

            return document.RootElement.TryGetProperty("Properties", out var values) && values.ValueKind is JsonValueKind.Object
                ? values.EnumerateObject().Where(p => p.Value.ValueKind is JsonValueKind.String).ToDictionary(p => p.Name, p => p.Value.GetString()!)
                : new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    public static void SetStorage(string configIni, long bytes)
    {
        var lines = File.Exists(configIni) ? File.ReadAllLines(configIni) : [];
        AtomicFile.WriteAllText(configIni, string.Join('\n', lines.Where(line => IniKey(line) is not "disk.dataPartition.size").Append($"disk.dataPartition.size={bytes}")) + "\n");
    }

    private static Dictionary<string, string> ReadIni(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var separator = line.IndexOf('=');

                if (separator > 0)
                {
                    values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
                }
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }

        return values;
    }

    private static string IniKey(string line) => line.IndexOf('=') is var separator and > 0 ? line[..separator].Trim() : line.Trim();
}
