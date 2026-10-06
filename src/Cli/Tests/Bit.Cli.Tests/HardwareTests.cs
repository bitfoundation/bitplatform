using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Tools;

namespace Bit.Cli.Tests;

[TestClass]
public class HardwareTests
{
    private const long Gibibyte = 1024L * 1024 * 1024;

    [TestMethod]
    public void TurnedOffVirtualization_Should_BeReportedOnlyWhenSomethingNeedsIt()
    {
        var off = new HardwareFacts { VirtualizationEnabled = false };
        var android = new HashSet<Platform> { Platform.Web, Platform.Android };

        var dockerDesktop = Hardware.Evaluate(off, new ToolNeeds { Aspire = true }, HostOs.Windows).Single();
        Assert.AreEqual("virtualization", dockerDesktop.Id);
        StringAssert.Contains(dockerDesktop.Text, "Docker Desktop needs it");
        Assert.AreEqual(Hardware.VirtualizationVideo, dockerDesktop.Link);

        StringAssert.Contains(Hardware.Evaluate(off, new ToolNeeds { Aspire = true, Platforms = android }, HostOs.MacOS).Single().Text, "Docker Desktop and the Android emulator");
        StringAssert.Contains(Hardware.Evaluate(off, new ToolNeeds { Platforms = android }, HostOs.Linux).Single().Text, "KVM");

        Assert.IsEmpty(Hardware.Evaluate(off, new ToolNeeds { Aspire = false }, HostOs.Windows));
        Assert.IsEmpty(Hardware.Evaluate(off, new ToolNeeds { Aspire = true }, HostOs.Linux));
        Assert.IsEmpty(Hardware.Evaluate(new HardwareFacts(), new ToolNeeds { Aspire = true, Platforms = android }, HostOs.Windows));
    }

    [TestMethod]
    [DataRow(15.8, true, "16 GB")]
    [DataRow(23.4, false, null)]
    [DataRow(31.9, false, null)]
    public void LittleMemory_Should_BeReported(double gigabytes, bool reported, string? text)
    {
        var warnings = Hardware.Evaluate(new HardwareFacts { MemoryBytes = (long)(gigabytes * Gibibyte) }, new ToolNeeds(), HostOs.Windows);

        Assert.AreEqual(reported, warnings.Any(w => w.Id is "memory"));

        if (text is not null)
        {
            StringAssert.Contains(warnings.Single().Text, text);
            StringAssert.Contains(warnings.Single().Text, "24 GB");
        }
    }

    [TestMethod]
    public void OnlyACertainHardDisk_Should_BeReported()
    {
        Assert.AreEqual("disk", Hardware.Evaluate(new HardwareFacts { OnHardDisk = true }, new ToolNeeds(), HostOs.Windows).Single().Id);
        Assert.IsEmpty(Hardware.Evaluate(new HardwareFacts { OnHardDisk = false }, new ToolNeeds(), HostOs.Windows));
        Assert.IsEmpty(Hardware.Evaluate(new HardwareFacts { OnHardDisk = null }, new ToolNeeds(), HostOs.Windows));
    }

    [TestMethod]
    [DataRow("hypervisor=True\r\nfirmware=False\r\nmedia=4\r\n", true, false)]
    [DataRow("hypervisor=False\nfirmware=True\nmedia=3", true, true)]
    [DataRow("hypervisor=False\nfirmware=False\nmedia=0", false, null)]
    [DataRow("hypervisor=\nfirmware=\nmedia=", null, null)]
    [DataRow("", null, null)]
    [DataRow("system=LENOVO 21HD\nhypervisor=False\nfirmware=False\nmedia=3\nbus=11\ndisk=ST2000DM008-2FR102", false, true)]
    [DataRow("system=Dell Inc. XPS 15 9530\nhypervisor=True\nfirmware=False\nmedia=3\nbus=7\ndisk=WD Elements 2620", true, null)]
    [DataRow("system=ASUS System Product Name\nhypervisor=True\nfirmware=False\nmedia=3\nbus=15\ndisk=Msft Virtual Disk", true, null)]
    [DataRow("system=Microsoft Corporation Virtual Machine\nhypervisor=True\nfirmware=False\nmedia=3\nbus=1", null, null)]
    [DataRow("system=VMware, Inc. VMware20,1\nhypervisor=False\nfirmware=True\nmedia=3\nbus=10\ndisk=VMware Virtual disk", true, null)]
    [DataRow("system=Parallels International GmbH. Parallels ARM Virtual Machine\nhypervisor=False\nfirmware=False\nmedia=4", null, null)]
    public void WindowsAnswers_Should_BeReadConservatively(string output, bool? virtualization, bool? hardDisk)
    {
        var facts = Hardware.ParseWindows(output, new HardwareFacts());

        Assert.AreEqual(virtualization, facts.VirtualizationEnabled);
        Assert.AreEqual(hardDisk, facts.OnHardDisk);
    }

    [TestMethod]
    public void TheWindowsScript_Should_AskAboutTheProjectsDrive()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        StringAssert.Contains(Hardware.WindowsScript(@"d:\Projects\Contoso"), "DriveLetter='D'");
    }

    [TestMethod]
    [DataRow("sda1", "sda")]
    [DataRow("sda", "sda")]
    [DataRow("vdb3", "vdb")]
    [DataRow("xvda1", "xvda")]
    [DataRow("nvme0n1p2", "nvme0n1")]
    [DataRow("nvme0n1", "nvme0n1")]
    [DataRow("mmcblk0p1", "mmcblk0")]
    [DataRow("dm-0", null)]
    [DataRow("md127", null)]
    [DataRow("loop3", null)]
    public void PartitionNames_Should_LeadToTheirDisk(string device, string? disk)
    {
        Assert.AreEqual(disk, Hardware.DiskOf(device));
    }

    [TestMethod]
    public async Task OnLinux_TheDisk_Should_OnlyBeJudgedOnBareMetal()
    {
        using var host = new TestHost(HostOs.Linux);
        var root = Directory.CreateDirectory(Path.Combine(host.Root, "fs")).FullName;
        Directory.CreateDirectory(Path.Combine(root, "sys", "block", "sda", "queue"));
        File.WriteAllText(Path.Combine(root, "sys", "block", "sda", "queue", "rotational"), "1\n");
        host.Runner.On("df", "-P", 0, "Filesystem 1024-blocks Used Available Capacity Mounted on\n/dev/sda2 103081248 10 99 1% /home\n");
        host.Runner.On("systemd-detect-virt", "", 1, "none\n");
        host.Runner.On("lsblk", "-dno TRAN /dev/sda", 0, "sata\n");

        var bareMetal = await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None, root);

        Assert.IsTrue(bareMetal.OnHardDisk);
        Assert.IsFalse(bareMetal.VirtualizationEnabled);

        host.Runner.On("lsblk", "-dno TRAN /dev/sda", 0, "usb\n");

        Assert.IsNull((await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None, root)).OnHardDisk);

        host.Runner.NotFound("lsblk");

        Assert.IsNull((await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None, root)).OnHardDisk);

        Directory.CreateDirectory(Path.Combine(root, "dev"));
        File.WriteAllText(Path.Combine(root, "dev", "kvm"), "");
        host.Runner.On("systemd-detect-virt", "", 0, "kvm\n");

        var virtualMachine = await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None, root);

        Assert.IsNull(virtualMachine.OnHardDisk);
        Assert.IsTrue(virtualMachine.VirtualizationEnabled);
    }

    [TestMethod]
    public async Task OnMacOs_TheDiskAndVirtualization_Should_ComeFromTheSystemsOwnAnswers()
    {
        using var host = new TestHost(HostOs.MacOS);
        host.Runner.On("sysctl", "-n kern.hv_vmm_present", 0, "0\n");
        host.Runner.On("sysctl", "-n kern.hv_support", 0, "1\n");
        host.Runner.On("df", "-P", 0, "Filesystem 512-blocks Used Available Capacity Mounted on\n/dev/disk3s5 1942700360 2 1 1% /System/Volumes/Data\n");
        host.Runner.On("diskutil", "info /System/Volumes/Data", 0, "   Device Node:               /dev/disk3s5\n   Device Location:           Internal\n   Solid State:               Yes\n");

        var facts = await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None);

        Assert.IsTrue(facts.VirtualizationEnabled);
        Assert.IsFalse(facts.OnHardDisk);

        host.Runner.On("diskutil", "info /System/Volumes/Data", 0, "   Protocol:                  USB\n   Device Location:           External\n   Solid State:               No\n");

        Assert.IsNull((await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None)).OnHardDisk);
    }

    [TestMethod]
    public async Task InAMacVirtualMachine_OnlyWorkingVirtualization_Should_BeReported()
    {
        using var host = new TestHost(HostOs.MacOS);
        host.Runner.On("sysctl", "-n kern.hv_vmm_present", 0, "1\n");
        host.Runner.On("sysctl", "-n kern.hv_support", 0, "0\n");
        host.Runner.On("df", "-P", 0, "Filesystem 512-blocks Used Available Capacity Mounted on\n/dev/disk3s5 1942700360 2 1 1% /System/Volumes/Data\n");
        host.Runner.On("diskutil", "info /System/Volumes/Data", 0, "   Device Location:           Internal\n   Solid State:               No\n");

        var facts = await Hardware.ProbeAsync(host.Environment, host.Runner, host.WorkingDirectory, CancellationToken.None);

        Assert.IsNull(facts.VirtualizationEnabled);
        Assert.IsNull(facts.OnHardDisk);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "diskutil"));
    }
}
