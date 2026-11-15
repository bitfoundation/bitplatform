using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class CiTests
{
    [TestMethod]
    [DataRow(HostOs.Windows)]
    [DataRow(HostOs.MacOS)]
    [DataRow(HostOs.Linux)]
    public async Task Ci_Should_RunTheWorkflowWithActOnThisMachine(HostOs os)
    {
        using var host = new TestHost(os);
        var directory = CreateProject(host, "    runs-on: ubuntu-24.04\n  other:\n    runs-on: 'ubuntu-26.04' # the next one\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci", "--", "--job", "build_and_test"), host.Output);

        var act = host.Runner.Calls.Single(c => c.FileName == "act");

        Assert.AreEqual(directory, act.WorkingDirectory);
        Assert.IsTrue(act.Interactive, "act's log is the run's output.");
        Assert.AreEqual("act workflow_dispatch --workflows .github/workflows/ci.yml --platform ubuntu-24.04=-self-hosted --platform ubuntu-26.04=-self-hosted --action-offline-mode --job build_and_test", act.CommandLine);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "docker" or "podman"), "Nothing runs in a container.");
    }

    [TestMethod]
    public async Task TheWorkflowsSteps_Should_RunTheBitThatRunsThem()
    {
        using var host = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["PATH"] = "/usr/bin" });
        var directory = CreateProject(host, "    runs-on: ubuntu-26.04\n");

        Assert.AreEqual(CliApp.ExitOk, await new LocalCi(host.Services, directory) { Self = "/opt/bit-dev/bit" }.RunAsync([], CancellationToken.None), host.Output);

        Assert.AreEqual($"{Path.GetDirectoryName("/opt/bit-dev/bit")}{Path.PathSeparator}/usr/bin", host.Runner.Calls.Single(c => c.FileName == "act").Environment!["PATH"]);
    }

    [TestMethod]
    public async Task AListOfLabels_Should_RunEachOnThisMachine()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: [ubuntu-24.04, 'ubuntu-26.04']\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci"), host.Output);

        StringAssert.Contains(host.Runner.Calls.Single(c => c.FileName == "act").CommandLine, "--platform ubuntu-24.04=-self-hosted --platform ubuntu-26.04=-self-hosted");
    }

    [TestMethod]
    [DataRow("    runs-on: ${{ matrix.os }}\n")]
    [DataRow("    runs-on:\n      - ubuntu-24.04\n")]
    public async Task ARunsOnThatIsntALabel_Should_StopRatherThanPassWithoutTheJob(string jobs)
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, jobs);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("ci"), host.Output);

        StringAssert.Contains(host.FlatOutput, "runs-on has to be a runner label");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName == "act"));
    }

    [TestMethod]
    public async Task ASecretForAct_Should_StayOutOfTheLog()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: ubuntu-26.04\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("ci", "--", "-s", "GITHUB_TOKEN=ghp_example", "--secret=NUGET_KEY=nk_example", "-s=A_KEY=a_example", "-sB_KEY=b_example", "-s", "FROM_ENV"), host.Output);

        var act = host.Runner.Calls.Single(c => c.FileName == "act");
        CollectionAssert.IsSubsetOf(new[] { "GITHUB_TOKEN=ghp_example", "--secret=NUGET_KEY=nk_example", "-s=A_KEY=a_example", "-sB_KEY=b_example" }, act.Arguments.ToArray(), "act gets them as they are.");
        foreach (var value in new[] { "ghp_example", "nk_example", "a_example", "b_example" })
        {
            Assert.IsFalse(act.CommandLine.Contains(value, StringComparison.Ordinal), act.CommandLine);
        }
        foreach (var masked in new[] { "GITHUB_TOKEN=***", "--secret=NUGET_KEY=***", "-s=A_KEY=***", "-sB_KEY=***" })
        {
            StringAssert.Contains(act.CommandLine, masked);
        }
        StringAssert.EndsWith(act.CommandLine, "-s FROM_ENV");
    }

    [TestMethod]
    public async Task Ci_Should_NeedTheProjectsWorkflowActAndPowerShell()
    {
        using var host = new TestHost(HostOs.Linux);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "isn't a bit Boilerplate project folder");

        var directory = CreateProject(host, "    runs-on: ubuntu-26.04\n");
        File.Delete(Path.Combine(directory, ".github", "workflows", "ci.yml"));

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "ci.yml");

        CreateProject(host, "    runs-on: ubuntu-26.04\n");
        host.Runner.Executables.Remove("act");
        host.Runner.Executables.Remove("pwsh");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("ci"));
        StringAssert.Contains(host.FlatOutput, "bit setup --tools act");
        StringAssert.Contains(host.FlatOutput, "bit setup --tools pwsh");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName == "act"));
    }

    [TestMethod]
    public async Task ActsExitCode_Should_FailTheRun()
    {
        using var host = new TestHost(HostOs.Linux);
        CreateProject(host, "    runs-on: ubuntu-26.04\n");
        host.Runner.On("act", "workflow_dispatch", 1);

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("ci"), host.Output);

        StringAssert.Contains(host.FlatOutput, "CI failed");
        StringAssert.Contains(host.FlatOutput, "act's log above says which step failed");
    }

    private static string CreateProject(TestHost host, string jobs)
    {
        var directory = host.WorkingDirectory;
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, "Contoso.Web.slnf"), "{}");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(directory, ".github", "workflows")).FullName, "ci.yml"), $"on:\n  workflow_dispatch:\njobs:\n  build_and_test:\n{jobs}");
        host.Runner.Executables["act"] = "act";
        host.Runner.Executables["pwsh"] = "pwsh";
        return directory;
    }
}
