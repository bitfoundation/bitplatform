using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;

namespace Bit.Cli.Tools;

public sealed record HardwareFacts
{
    public long? MemoryBytes { get; init; }

    public bool? VirtualizationEnabled { get; init; }

    public bool? OnHardDisk { get; init; }
}

public sealed record HardwareWarning(string Id, string Text, string? Link = null);

public static partial class Hardware
{
    public const string VirtualizationVideo = "https://www.youtube.com/watch?v=ZDeje9wgDp4";

    private const long Gibibyte = 1024L * 1024 * 1024;

    private const long RecommendedMemoryBytes = 24 * Gibibyte;

    private const long ReportedMemoryShortfall = Gibibyte;

    public static async Task<HardwareFacts> ProbeAsync(CliEnvironment environment, IProcessRunner runner, string directory, CancellationToken cancellationToken, string fileSystemRoot = "/")
    {
        var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var facts = new HardwareFacts { MemoryBytes = memory > 0 ? memory : null };

        try
        {
            return environment.Os switch
            {
                HostOs.Windows => ParseWindows(await RunAsync(runner, "powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", WindowsScript(directory)], cancellationToken), facts),
                HostOs.MacOS => await ProbeMacAsync(runner, directory, facts, cancellationToken),
                _ => await ProbeLinuxAsync(runner, directory, facts, fileSystemRoot, cancellationToken)
            };
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or FormatException)
        {
            return facts;
        }
    }

    public static IReadOnlyList<HardwareWarning> Evaluate(HardwareFacts facts, ToolNeeds needs, HostOs os)
    {
        var warnings = new List<HardwareWarning>();
        var emulator = needs.Platforms.Contains(Platform.Android);
        var dockerDesktop = needs.Aspire && os is not HostOs.Linux;

        if (facts.VirtualizationEnabled is false && (emulator || dockerDesktop))
        {
            var users = (dockerDesktop, emulator) switch
            {
                (true, true) => "Docker Desktop and the Android emulator need it",
                (true, false) => "Docker Desktop needs it",
                _ => "the Android emulator needs it"
            };

            warnings.Add(os is HostOs.Linux
                ? new("virtualization", "KVM isn't available, so the Android emulator would run without hardware acceleration. Turn on virtualization in the BIOS or UEFI settings.", VirtualizationVideo)
                : new("virtualization", $"Virtualization is turned off in this machine's BIOS or UEFI settings, and {users}.", VirtualizationVideo));
        }

        if (facts.MemoryBytes is { } memory && memory < RecommendedMemoryBytes - ReportedMemoryShortfall)
        {
            warnings.Add(new("memory", $"This machine has {Math.Round((double)memory / Gibibyte)} GB of memory. With Aspire's containers, the IDE, a browser and an emulator running, 24 GB or more keeps things smooth."));
        }

        if (facts.OnHardDisk is true)
        {
            warnings.Add(new("disk", "The project's drive is a hard disk. Restores, builds and containers are much faster on an SSD."));
        }

        return warnings;
    }

    public static string Describe(HardwareFacts facts)
    {
        var parts = new List<string>();

        if (facts.MemoryBytes is { } memory)
        {
            parts.Add($"{Math.Round((double)memory / Gibibyte)} GB of memory");
        }

        if (facts.VirtualizationEnabled is { } virtualization)
        {
            parts.Add(virtualization ? "virtualization on" : "virtualization off");
        }

        if (facts.OnHardDisk is { } hardDisk)
        {
            parts.Add(hardDisk ? "a hard disk" : "an SSD");
        }

        return string.Join(", ", parts);
    }

    public static string WindowsScript(string directory)
    {
        var drive = Path.GetPathRoot(Path.GetFullPath(directory)) is { Length: >= 2 } root && root[1] == ':' ? char.ToUpperInvariant(root[0]).ToString() : "";

        return string.Join("; ",
            "$ErrorActionPreference = 'SilentlyContinue'",
            "$system = Get-CimInstance Win32_ComputerSystem -Property HypervisorPresent,Manufacturer,Model",
            "$processor = Get-CimInstance Win32_Processor -Property VirtualizationFirmwareEnabled | Select-Object -First 1",
            "'system=' + $system.Manufacturer + ' ' + $system.Model",
            "'hypervisor=' + $system.HypervisorPresent",
            "'firmware=' + $processor.VirtualizationFirmwareEnabled",
            $"$partition = Get-CimInstance -Namespace root/Microsoft/Windows/Storage -ClassName MSFT_Partition -Filter \"DriveLetter='{drive}'\"",
            "if ($partition) { $disk = Get-CimAssociatedInstance -InputObject $partition -ResultClassName MSFT_Disk }",
            "if ($disk) { $physical = Get-CimInstance -Namespace root/Microsoft/Windows/Storage -ClassName MSFT_PhysicalDisk -Filter \"DeviceId='$($disk.Number)'\" }",
            "'media=' + $physical.MediaType",
            "'bus=' + $physical.BusType",
            "'disk=' + $physical.FriendlyName");
    }

    public static HardwareFacts ParseWindows(string output, HardwareFacts facts)
    {
        var values = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .GroupBy(pair => pair[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last()[1], StringComparer.OrdinalIgnoreCase);

        var hypervisor = values.GetValueOrDefault("hypervisor");
        var firmware = values.GetValueOrDefault("firmware");
        var virtualMachine = VirtualMachineRegex().IsMatch(values.GetValueOrDefault("system") ?? "");
        var internalDisk = virtualMachine is false
            && values.GetValueOrDefault("bus") is not ("7" or "9" or "14" or "15")
            && VirtualMachineRegex().IsMatch(values.GetValueOrDefault("disk") ?? "") is false;

        bool? virtualization = (virtualMachine, hypervisor, firmware) switch
        {
            (true, _, "True") => true,
            (true, _, _) => null,
            (false, "True", _) or (false, _, "True") => true,
            (false, "False", "False") => false,
            _ => null
        };

        bool? hardDisk = (internalDisk, values.GetValueOrDefault("media")) switch
        {
            (true, "3") => true,
            (true, "4" or "5") => false,
            _ => null
        };

        return facts with { VirtualizationEnabled = virtualization, OnHardDisk = hardDisk };
    }

    private static async Task<HardwareFacts> ProbeMacAsync(IProcessRunner runner, string directory, HardwareFacts facts, CancellationToken cancellationToken)
    {
        var virtualMachine = (await RunAsync(runner, "sysctl", ["-n", "kern.hv_vmm_present"], cancellationToken)).Trim() == "1";

        bool? virtualization = (await RunAsync(runner, "sysctl", ["-n", "kern.hv_support"], cancellationToken)).Trim() switch
        {
            "1" => true,
            "0" when virtualMachine is false => false,
            _ => null
        };

        bool? hardDisk = null;

        if (virtualMachine is false && MountPoint(await RunAsync(runner, "df", ["-P", directory], cancellationToken)) is { } mountPoint)
        {
            var info = await RunAsync(runner, "diskutil", ["info", mountPoint], cancellationToken);

            hardDisk = ExternalDriveRegex().IsMatch(info) is false && SolidStateRegex().Match(info) is { Success: true } match
                ? match.Groups["answer"].Value.Equals("No", StringComparison.OrdinalIgnoreCase)
                : null;
        }

        return facts with { VirtualizationEnabled = virtualization, OnHardDisk = hardDisk };
    }

    private static async Task<HardwareFacts> ProbeLinuxAsync(IProcessRunner runner, string directory, HardwareFacts facts, string root, CancellationToken cancellationToken)
    {
        var bareMetal = (await RunAsync(runner, "systemd-detect-virt", [], cancellationToken)).Trim() == "none";
        bool? virtualization = File.Exists(Path.Combine(root, "dev", "kvm")) ? true : bareMetal ? false : null;
        bool? hardDisk = null;

        if (bareMetal && DeviceName(await RunAsync(runner, "df", ["-P", directory], cancellationToken)) is { } device && DiskOf(device) is { } disk)
        {
            var rotational = Path.Combine(root, "sys", "block", disk, "queue", "rotational");

            if (File.Exists(rotational))
            {
                hardDisk = File.ReadAllText(rotational).Trim() switch
                {
                    "0" => false,
                    "1" when (await RunAsync(runner, "lsblk", ["-dno", "TRAN", $"/dev/{disk}"], cancellationToken)).Trim() is "sata" or "ata" or "sas" => true,
                    _ => null
                };
            }
        }

        return facts with { VirtualizationEnabled = virtualization, OnHardDisk = hardDisk };
    }

    public static string? DiskOf(string device)
    {
        if (DiskWithPartitionSuffixRegex().Match(device) is { Success: true } numbered)
            return numbered.Groups["disk"].Value;

        if (DiskWithNumberedPartitionRegex().Match(device) is { Success: true } lettered)
            return lettered.Groups["disk"].Value;

        return WholeDiskRegex().IsMatch(device) ? device : null;
    }

    private static string? DeviceName(string dfOutput)
    {
        var line = dfOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
        var source = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return source is not null && source.StartsWith("/dev/", StringComparison.Ordinal) && source.StartsWith("/dev/mapper/", StringComparison.Ordinal) is false
            ? source["/dev/".Length..]
            : null;
    }

    private static string? MountPoint(string dfOutput)
    {
        var line = dfOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
        var match = line is null ? null : DfLineRegex().Match(line);
        return match is { Success: true } ? match.Groups["mount"].Value : null;
    }

    private static async Task<string> RunAsync(IProcessRunner runner, string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(new ProcessSpec { FileName = fileName, Arguments = arguments, Timeout = TimeSpan.FromSeconds(20) }, cancellationToken);
        return result.NotFound || result.TimedOut ? "" : result.Output;
    }

    [GeneratedRegex(@"^(?<disk>nvme\d+n\d+|mmcblk\d+)p\d+$")]
    private static partial Regex DiskWithPartitionSuffixRegex();

    [GeneratedRegex(@"^(?<disk>(?:sd|vd|hd|xvd)[a-z]+)\d+$")]
    private static partial Regex DiskWithNumberedPartitionRegex();

    [GeneratedRegex(@"^(?:nvme\d+n\d+|mmcblk\d+|(?:sd|vd|hd|xvd)[a-z]+)$")]
    private static partial Regex WholeDiskRegex();

    [GeneratedRegex(@"Solid State:\s*(?<answer>Yes|No)", RegexOptions.IgnoreCase)]
    private static partial Regex SolidStateRegex();

    [GeneratedRegex(@"Device Location:\s*External|Protocol:\s*USB", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalDriveRegex();

    [GeneratedRegex(@"virtual|vmware|vbox|qemu|parallels|xen|kvm|bochs", RegexOptions.IgnoreCase)]
    private static partial Regex VirtualMachineRegex();

    [GeneratedRegex(@"^\S+\s+\d+\s+\d+\s+\d+\s+\d+%\s+(?<mount>.+)$")]
    private static partial Regex DfLineRegex();
}
