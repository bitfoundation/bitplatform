using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class CiTests
{
    private const string Dockerfile = "ARG UBUNTU_VERSION=26.04\nFROM ghcr.io/catthehacker/ubuntu:pwsh-${UBUNTU_VERSION}\n";

    private const string Pipe = @"\\.\pipe\podman-machine-default";

    [TestMethod]
    public async Task Ci_Should_RunTheWorkflowWithActOnPodmansPipe()
    {
        using var host = new TestHost(HostOs.Windows);
        var directory = CreateProject(host, "    runs-on: ubuntu-24.04\n");
        Podman(host);
        host.Runner.On("podman", "image inspect", 0);

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci", "--", "--job", "build_and_test"), host.Output);

        var act = host.Runner.Calls.Single(c => c.FileName == "act");
        var image = LocalCi.ImageFor(Dockerfile, "ubuntu-24.04");

        Assert.AreEqual(directory, act.WorkingDirectory);
        Assert.AreEqual("npipe:////./pipe/podman-machine-default", act.Environment!["DOCKER_HOST"]);
        Assert.IsTrue(act.Interactive, "act's log is the run's output.");
        StringAssert.StartsWith(act.CommandLine, "act workflow_dispatch --workflows .github/workflows/ci.yml");
        StringAssert.Contains(act.CommandLine, $"--platform ubuntu-24.04={image}");
        StringAssert.Contains(act.CommandLine, "--container-daemon-socket /run/user/1000/podman/podman.sock");
        StringAssert.EndsWith(act.CommandLine, "--job build_and_test");
        StringAssert.StartsWith(image, "localhost/bit-ci:24.04-");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "build"), "The image was there already.");
    }

    [TestMethod]
    public async Task Ci_Should_BuildTheImageForEachUbuntuVersionOnlyWhenItsMissing()
    {
        using var host = new TestHost(HostOs.Windows);
        CreateProject(host, "    runs-on: ubuntu-24.04\n  other:\n    runs-on: 'ubuntu-26.04' # the next one\n  mac:\n    runs-on: macos-15\n");
        Podman(host);
        host.Runner.On("podman", "image inspect", 1, "image not known");
        host.Runner.On("podman", $"image inspect {LocalCi.ImageFor(Dockerfile, "ubuntu-26.04")}", 0);

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci"), host.Output);

        var build = host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "build");

        CollectionAssert.IsSubsetOf(new[] { "--platform", "linux/amd64", "--build-arg", "UBUNTU_VERSION=24.04", "--tag", LocalCi.ImageFor(Dockerfile, "ubuntu-24.04") }, build.Arguments.ToArray());
        StringAssert.Contains(host.Runner.Calls.Single(c => c.FileName == "act").CommandLine, $"--platform ubuntu-26.04={LocalCi.ImageFor(Dockerfile, "ubuntu-26.04")}");
    }

    [TestMethod]
    public async Task Ci_Should_SayWhenPodmansDockerApiIsDown()
    {
        using var host = new TestHost(HostOs.Windows);
        CreateProject(host, "    runs-on: ubuntu-24.04\n");
        Podman(host);
        host.Environment.NamedPipeExists = _ => false;

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("ci"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Podman's Docker API is down");
        StringAssert.Contains(host.FlatOutput, "bit doctor");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName == "act"));
    }

    [TestMethod]
    public async Task Ci_Should_PointActAtDockersSocketInItsVm()
    {
        using var mac = new TestHost(HostOs.MacOS);
        CreateProject(mac, "    runs-on: ubuntu-26.04\n");
        mac.Runner.Executables["act"] = "act";
        mac.Runner.On("docker", "version", 0, "28.5.1");

        Assert.AreEqual(CliApp.ExitOk, await mac.RunAsync("ci"), mac.Output);

        var act = mac.Runner.Calls.Single(c => c.FileName == "act");
        Assert.IsNull(act.Environment, "act finds Docker itself.");
        StringAssert.Contains(act.CommandLine, "--container-daemon-socket /var/run/docker.sock");
    }

    [TestMethod]
    public async Task Ci_Should_ConnectActToPodmansOwnSocketOnLinux()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: ubuntu-26.04\n");
        Podman(host);
        host.Runner.On("podman", "info", 0, "/run/user/1000/podman/podman.sock");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci"), host.Output);

        var act = host.Runner.Calls.Single(c => c.FileName == "act");
        Assert.AreEqual("unix:///run/user/1000/podman/podman.sock", act.Environment!["DOCKER_HOST"]);
        StringAssert.Contains(act.CommandLine, "--container-daemon-socket /run/user/1000/podman/podman.sock");
    }

    [TestMethod]
    public async Task ASecretForAct_Should_StayOutOfTheLog()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: ubuntu-26.04\n");
        host.Runner.On("docker", "version", 0, "28.5.1");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci", "--", "-s", "GITHUB_TOKEN=ghp_example", "--secret=NUGET_KEY=nk_example", "-s", "FROM_ENV"), host.Output);

        var act = host.Runner.Calls.Single(c => c.FileName == "act");
        CollectionAssert.IsSubsetOf(new[] { "GITHUB_TOKEN=ghp_example", "--secret=NUGET_KEY=nk_example" }, act.Arguments.ToArray(), "act gets them as they are.");
        Assert.IsFalse(act.CommandLine.Contains("ghp_example", StringComparison.Ordinal) || act.CommandLine.Contains("nk_example", StringComparison.Ordinal), act.CommandLine);
        StringAssert.Contains(act.CommandLine, "GITHUB_TOKEN=***");
        StringAssert.EndsWith(act.CommandLine, "-s FROM_ENV");
    }

    [TestMethod]
    public async Task Ci_Should_NeedActAndTheProjectsWorkflowAndDockerfile()
    {
        using var host = new TestHost(HostOs.Linux);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");

        var directory = CreateProject(host, "    runs-on: ubuntu-26.04\n");
        File.Delete(Path.Combine(directory, ".github", "act", "Dockerfile"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "Dockerfile");

        File.WriteAllText(Path.Combine(directory, ".github", "act", "Dockerfile"), Dockerfile);
        host.Runner.Executables.Remove("act");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "bit setup --tools act");
    }

    [TestMethod]
    public async Task ActsExitCode_Should_FailTheRun()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: ubuntu-26.04\n");
        host.Runner.On("docker", "version", 0, "28.5.1");
        host.Runner.On("act", "workflow_dispatch", 1);

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("ci"), host.Output);

        StringAssert.Contains(host.FlatOutput, "CI failed");
        StringAssert.Contains(host.FlatOutput, "act's log above says which step failed");
        Assert.IsFalse(host.Runner.Calls.Single(c => c.FileName == "act").CommandLine.Contains("--container-daemon-socket", StringComparison.Ordinal), "On Linux, act mounts the socket it connects to.");
    }

    [TestMethod]
    public void TheImage_Should_ChangeWithTheDockerfileButNotItsLineEndings()
    {
        Assert.AreEqual(LocalCi.ImageFor(Dockerfile, "ubuntu-24.04"), LocalCi.ImageFor(Dockerfile.ReplaceLineEndings("\r\n"), "ubuntu-24.04"));
        Assert.AreNotEqual(LocalCi.ImageFor(Dockerfile, "ubuntu-24.04"), LocalCi.ImageFor(Dockerfile + "RUN true\n", "ubuntu-24.04"));
        CollectionAssert.AreEqual(new[] { "ubuntu-latest" }, LocalCi.UbuntuLabels("jobs:\n  a:\n    runs-on: windows-2025\n").ToArray());
    }

    private static void Podman(TestHost host)
    {
        host.Runner.Executables["act"] = "act";
        host.Runner.Executables["podman"] = "podman";
        host.Runner.NotFound("docker");
        host.Runner.On("podman", "info", 0, "unix:///run/user/1000/podman/podman.sock");
        host.Runner.On("podman", "machine inspect --format", 0, Pipe);
    }

    private static string CreateProject(TestHost host, string jobs)
    {
        var directory = host.WorkingDirectory;
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, "Contoso.Web.slnf"), "{}");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(directory, ".github", "workflows")).FullName, "ci.yml"), $"on:\n  workflow_dispatch:\njobs:\n  build_and_test:\n{jobs}");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(directory, ".github", "act")).FullName, "Dockerfile"), Dockerfile);
        host.Runner.Executables["act"] = "act";
        return directory;
    }
}
