using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ProjectStepTests
{
    [TestMethod]
    public void TheDevelopmentCertificate_Should_BeUniqueLoadableAndMarked()
    {
        using var host = new TestHost();
        var first = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "a")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "b")).FullName;

        ProjectSteps.WriteDevelopmentCertificate(first);
        ProjectSteps.WriteDevelopmentCertificate(second);

        using var certificate = X509Certificate2.CreateFromPemFile(Path.Combine(first, "AppCertificate.crt"), Path.Combine(first, "AppCertificate.key"));
        using var other = X509Certificate2.CreateFromPemFile(Path.Combine(second, "AppCertificate.crt"), Path.Combine(second, "AppCertificate.key"));

        Assert.IsTrue(certificate.HasPrivateKey);
        StringAssert.Contains(certificate.Subject, "OU=Development");
        StringAssert.Contains(certificate.Subject, "CN=AppCertificate");
        Assert.AreEqual(3072, certificate.GetRSAPublicKey()!.KeySize);
        Assert.IsGreaterThan(DateTime.Now.AddYears(4), certificate.NotAfter);
        Assert.AreNotEqual(certificate.Thumbprint, other.Thumbprint);
        StringAssert.StartsWith(File.ReadAllText(Path.Combine(first, "AppCertificate.key")), "-----BEGIN PRIVATE KEY-----\n");
    }

    [TestMethod]
    public void TheTemplatesSampleVapidKeys_Should_GiveWayToOnePairOfTheProjectsOwn()
    {
        using var host = new TestHost();
        var project = Path.Combine(host.WorkingDirectory, "Shop");
        string[] files = [Path.Combine("src", "Client", "Boilerplate.Client.Web", "appsettings.json"), Path.Combine("src", "Server", "Boilerplate.Server.Api", "appsettings.json"), Path.Combine("src", "Server", "Boilerplate.Server.Web", "appsettings.json")];

        foreach (var file in files)
        {
            var template = File.ReadAllText(Path.Combine(TemplateDirectory(), file));
            Assert.IsTrue(template.Contains(Keys.VapidKeys.Sample.PublicKey, StringComparison.Ordinal) || template.Contains(Keys.VapidKeys.Sample.PrivateKey, StringComparison.Ordinal), $"{file} no longer has a key of the sample pair VapidKeys.Sample names");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(project, file))!);
            File.WriteAllText(Path.Combine(project, file), template);
        }

        var serverApi = File.ReadAllText(Path.Combine(project, files[1]));
        StringAssert.Contains(serverApi, Keys.VapidKeys.Sample.PrivateKey);

        var built = Path.Combine(project, "src", "Server", "Boilerplate.Server.Api", "bin", "appsettings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(built)!);
        File.WriteAllText(built, serverApi);

        Assert.IsTrue(ProjectSteps.ReplaceSampleVapidKeys(project));

        var publicKey = Regex.Match(File.ReadAllText(Path.Combine(project, files[0])), @"""PublicKey"":\s*""(?<key>[^""]+)""").Groups["key"].Value;
        var privateKey = Regex.Match(File.ReadAllText(Path.Combine(project, files[1])), @"""PrivateKey"":\s*""(?<key>[^""]+)""").Groups["key"].Value;

        foreach (var file in files)
        {
            var text = File.ReadAllText(Path.Combine(project, file));
            Assert.IsFalse(text.Contains(Keys.VapidKeys.Sample.PublicKey, StringComparison.Ordinal) || text.Contains(Keys.VapidKeys.Sample.PrivateKey, StringComparison.Ordinal), file);
            Assert.AreEqual(File.ReadAllText(Path.Combine(TemplateDirectory(), file)).Replace(Keys.VapidKeys.Sample.PublicKey, publicKey, StringComparison.Ordinal).Replace(Keys.VapidKeys.Sample.PrivateKey, privateKey, StringComparison.Ordinal), text, $"{file} changed beyond its keys");
        }

        // The private key is the public key's own: P-256 derives the same point from it.
        using var key = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = Base64Url.DecodeFromChars(privateKey) });
        var derived = key.ExportParameters(includePrivateParameters: false);
        CollectionAssert.AreEqual(Base64Url.DecodeFromChars(publicKey), (byte[])[0x04, .. derived.Q.X!, .. derived.Q.Y!]);

        Assert.AreEqual(serverApi, File.ReadAllText(built), "a build output was touched");
        Assert.IsFalse(ProjectSteps.ReplaceSampleVapidKeys(project), "nothing is left to replace");
    }

    [TestMethod]
    public void InstalledWorkloads_Should_BeReadFromTheTable()
    {
        var output = """

            Installed Workload Id      Manifest Version       Installation Source
            --------------------------------------------------------------------
            wasm-tools                 10.0.12/10.0.100       SDK 10.0.400
            maui-android               10.0.1/10.0.100        SDK 10.0.400

            Use `dotnet workload search` to find additional workloads to install.
            """;

        CollectionAssert.AreEquivalent(new[] { "wasm-tools", "maui-android" }, ProjectSteps.ParseInstalledWorkloads(output).ToArray());
        Assert.IsEmpty(ProjectSteps.ParseInstalledWorkloads("No workloads are installed."));
    }

    [TestMethod]
    public async Task Git_Should_CreateDevelopAndMainWithTheFirstCommit()
    {
        if (await GitAvailableAsync() is false)
            return;

        using var host = new TestHost(variables: RealPath());
        using var identity = new GitIdentity(host.Root);
        var project = CreateFakeProject(host, "Contoso");
        var steps = new ProjectSteps(new CliServices(host.Environment, host.Console, runner: new ProcessRunner(host.Environment, CliLog.None)), project);

        var result = await steps.GitAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.IsTrue(project.GitReady);

        var runner = new ProcessRunner(host.Environment, CliLog.None);
        var branches = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["branch", "--format=%(refname:short)"], WorkingDirectory = project.Directory });
        var head = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--abbrev-ref", "HEAD"], WorkingDirectory = project.Directory });
        var log = await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["log", "--format=%s"], WorkingDirectory = project.Directory });

        CollectionAssert.AreEquivalent(new[] { "develop", "main" }, branches.OutputLines.ToArray());
        Assert.AreEqual("develop", head.Output.Trim());
        Assert.AreEqual("Create Contoso with bit new", log.Output.Trim());
    }

    [TestMethod]
    public async Task Git_Should_CommitAsTheUserWhenGitDoesntKnowThem()
    {
        using var host = new TestHost(HostOs.Windows, new Dictionary<string, string> { ["USERNAME"] = "Jane Doe" });
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var commits = 0;
        host.Runner.On("git", "commit", _ => ++commits == 1
            ? new ProcessResult { ExitCode = 128, Output = "Author identity unknown\n*** Please tell me who you are." }
            : new ProcessResult { ExitCode = 0 });
        var project = CreateFakeProject(host, "Contoso");

        var result = await new ProjectSteps(host.Services, project).GitAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("develop and main, committed as Jane Doe <Jane.Doe@git.com>", result.Detail);
        Assert.IsTrue(project.GitReady);
        var configs = host.Runner.Calls.Where(c => c.Arguments.FirstOrDefault() is "config" && c.Arguments[1] is not "--global").Select(c => string.Join(' ', c.Arguments)).ToArray();
        CollectionAssert.AreEqual(new[] { "config user.name Jane Doe", "config user.email Jane.Doe@git.com" }, configs);
    }

    [TestMethod]
    [DataRow(HostOs.Windows, "", true)]
    [DataRow(HostOs.Windows, "true", false)]
    [DataRow(HostOs.Linux, "", false)]
    public async Task Git_Should_TurnOnLongPathsOnWindows(HostOs os, string current, bool turnedOn)
    {
        using var host = new TestHost(os);
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        host.Runner.On("git", "config --global --get core.longpaths", current.Length > 0 ? 0 : 1, current);
        var project = CreateFakeProject(host, "Contoso");

        await new ProjectSteps(host.Services, project).GitAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(turnedOn, host.Runner.Calls.Any(c => string.Join(' ', c.Arguments) == "config --global core.longpaths true"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Git_Should_LeaveNoAutomaticMaintenanceRunningInTheNewRepository(bool inheritsGitConfig)
    {
        var variables = inheritsGitConfig
            ? new Dictionary<string, string> { ["GIT_CONFIG_COUNT"] = "1", ["GIT_CONFIG_KEY_0"] = "user.signingkey", ["GIT_CONFIG_VALUE_0"] = "ABC123" }
            : [];
        using var host = new TestHost(HostOs.Linux, variables);
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");
        var project = CreateFakeProject(host, "Contoso");

        await new ProjectSteps(host.Services, project).GitAsync(_ => { }, CancellationToken.None);

        var gitCalls = host.Runner.Calls.Where(c => c.FileName is "git" && c.Arguments.FirstOrDefault() is not "--version").ToArray();
        Assert.IsTrue(gitCalls.Any(c => c.Arguments.FirstOrDefault() is "commit"), string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine)));

        foreach (var call in gitCalls)
        {
            var environment = variables.ToDictionary(variable => variable.Key, variable => (string?)variable.Value);

            foreach (var (name, value) in call.Environment ?? new Dictionary<string, string?>())
            {
                environment[name] = value;
            }

            var settings = Enumerable.Range(0, int.Parse(environment.GetValueOrDefault("GIT_CONFIG_COUNT") ?? "0"))
                .ToDictionary(i => environment[$"GIT_CONFIG_KEY_{i}"]!, i => environment[$"GIT_CONFIG_VALUE_{i}"]);

            Assert.AreEqual("false", settings.GetValueOrDefault("maintenance.auto"), call.CommandLine);
            Assert.AreEqual("0", settings.GetValueOrDefault("gc.auto"), call.CommandLine);
            Assert.AreEqual(inheritsGitConfig ? "ABC123" : null, settings.GetValueOrDefault("user.signingkey"), call.CommandLine);
        }
    }

    [TestMethod]
    public async Task Git_Should_StayOutOfAnExistingRepository()
    {
        if (await GitAvailableAsync() is false)
            return;

        using var host = new TestHost(variables: RealPath());
        using var identity = new GitIdentity(host.Root);
        var runner = new ProcessRunner(host.Environment, CliLog.None);
        await runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["init"], WorkingDirectory = host.WorkingDirectory });
        var project = CreateFakeProject(host, "Contoso");

        var result = await new ProjectSteps(new CliServices(host.Environment, host.Console, runner: runner), project).GitAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Skipped, result.Status);
        Assert.IsFalse(Directory.Exists(Path.Combine(project.Directory, ".git")));
    }

    [TestMethod]
    public async Task TheMigration_Should_BeSkippedForOtherDatabasesAndExistingMigrations()
    {
        using var host = new TestHost();
        var other = new TemplateSelection(TemplateManifest.Embedded);
        other.Set("database", "Other");
        var project = CreateFakeProject(host, "Contoso", other);

        var skipped = await new ProjectSteps(host.Services, project).MigrationAsync(_ => { }, CancellationToken.None);
        Assert.AreEqual(StepStatus.Skipped, skipped.Status);

        var sqlite = CreateFakeProject(host, "Fabrikam", new TemplateSelection(TemplateManifest.Embedded));
        var migrations = Directory.CreateDirectory(Path.Combine(sqlite.ServerApiDirectory, "Infrastructure", "Data", "Migrations")).FullName;
        File.WriteAllText(Path.Combine(migrations, "20260101000000_Initial.cs"), "");

        var existing = await new ProjectSteps(host.Services, sqlite).MigrationAsync(_ => { }, CancellationToken.None);
        Assert.AreEqual(StepStatus.Skipped, existing.Status);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task TheMigration_Should_UseTheEfToolVersionOfTheProject()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso", new TemplateSelection(TemplateManifest.Embedded));

        var result = await new ProjectSteps(host.Services, project).MigrationAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        var call = host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() == "dnx");
        CollectionAssert.AreEqual(new[] { "dnx", "dotnet-ef@10.0.12", "--", "migrations", "add", "Initial", "--output-dir", "Infrastructure/Data/Migrations" }, call.Arguments.ToArray());
        Assert.AreEqual(project.ServerApiDirectory, call.WorkingDirectory);
    }

    [TestMethod]
    public async Task RazorErrorsFromAStaleCompilerServer_Should_BuildAgainWithAFreshOne()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        var builds = 0;
        host.Runner.On("dotnet", "build ", _ => ++builds == 1
            ? new ProcessResult { ExitCode = 1, Output = "HumanFollowUpCard.razor(5,10): error RZ1021: Markup in a code block must start with a tag." }
            : new ProcessResult { ExitCode = 0 });

        var result = await new ProjectSteps(host.Services, project).BuildAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        Assert.AreEqual("on a second try, after restarting the C# compiler server", result.Detail);
        CollectionAssert.AreEqual(new[] { "build", "build-server", "build" }, host.Runner.Calls.Select(c => c.Arguments[0]).ToArray());
        CollectionAssert.AreEqual(new[] { "build-server", "shutdown", "--vbcscompiler" }, host.Runner.Calls[1].Arguments.ToArray());
    }

    [TestMethod]
    public async Task OtherBuildErrors_Should_FailWithoutRestartingTheCompilerServer()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.On("dotnet", "build ", 1, "Program.cs(3,1): error CS0246: The type or namespace name 'Foo' could not be found");

        var result = await new ProjectSteps(host.Services, project).BuildAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Failed, result.Status);
        Assert.AreEqual("CS0246", result.Detail);
        Assert.AreEqual(1, host.Runner.Calls.Count);
    }

    [TestMethod]
    public async Task AMissingSdk_Should_StopTheStepsThatNeedIt()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        File.WriteAllText(Path.Combine(project.Directory, "global.json"), """{ "sdk": { "version": "11.0.100-rc.1.26425.128", "rollForward": "disable" } }""");
        host.Runner.On("dotnet", "--version", 145, "A compatible .NET SDK was not found.");
        var steps = new StepRunner(host.Services);

        var ready = await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), noWorkloads: false, noRestore: false, noBuild: false, noBrowsers: true, runMode: ProjectRunMode.None, CancellationToken.None);

        Assert.IsFalse(ready);
        var sdk = steps.Reports.Single(r => r.Id == "sdk").Result;
        Assert.AreEqual(StepStatus.Failed, sdk.Status);
        Assert.AreEqual("global.json asks for exactly 11.0.100-rc.1.26425.128", sdk.Detail);
        Assert.AreEqual(project.Directory, host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() == "--version").WorkingDirectory);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "workload" or "restore" or "build"));
    }

    [TestMethod]
    public async Task TheSdkCheck_Should_ReportTheSdkDotnetPicks()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        var steps = new ProjectSteps(host.Services, project);

        Assert.AreEqual(StepStatus.Skipped, (await steps.SdkAsync(_ => { }, CancellationToken.None)).Status);

        File.WriteAllText(Path.Combine(project.Directory, "global.json"), """{ "sdk": { "version": "11.0.100", "rollForward": "latestFeature" } }""");
        host.Runner.On("dotnet", "--version", 0, "11.0.104");
        var ready = await steps.SdkAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, ready.Status);
        Assert.AreEqual("11.0.104", ready.Detail);
    }

    [TestMethod]
    public async Task AHungDocker_Should_LeaveAspireOnThePodmanThatAnswers()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.On("docker", "version", 1);
        host.Runner.Executables["podman"] = "/usr/bin/podman";
        host.Runner.On("podman", "container ls", 0);

        var result = await new ProjectSteps(host.Services, project).ContainerRuntimeAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.AreEqual("Aspire runs the containers on Podman", result.Title);
        CollectionAssert.AreEqual(new[] { "user-secrets", "set", "DcpPublisher:ContainerRuntime", "podman", "--project", project.AppHostDirectory },
            host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "user-secrets").Arguments.ToArray());
    }

    [TestMethod]
    public async Task TheContainerRuntime_Should_BeDockerWhenItAnswers_AndLeftAloneWhenNothingDoes()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        var steps = new ProjectSteps(host.Services, project);
        host.Runner.Executables["podman"] = "/usr/bin/podman";
        host.Runner.On("podman", "container ls", 125);
        host.Runner.On("docker", "version", 1);

        Assert.AreEqual(StepStatus.Skipped, (await steps.ContainerRuntimeAsync(_ => { }, CancellationToken.None)).Status);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "user-secrets"));

        using var dockerHost = new TestHost(HostOs.Linux);
        var dockerProject = CreateFakeProject(dockerHost, "Contoso");
        dockerHost.Runner.On("docker", "version", 0, "29.1.3");

        var docker = await new ProjectSteps(dockerHost.Services, dockerProject).ContainerRuntimeAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual("Aspire runs the containers on Docker", docker.Title);
        Assert.AreEqual("docker", dockerHost.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "user-secrets").Arguments[3]);
    }

    [TestMethod]
    public async Task AspireStart_Should_WaitForEveryServerAndStopTheProject()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb", "serverapi");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        var aspire = host.Runner.Calls.Where(c => c.FileName is "aspire").Select(c => string.Join(' ', c.Arguments.Take(2))).ToArray();
        CollectionAssert.AreEqual(new[] { "doctor --format", "ps --format", "start --apphost", "wait serverweb", "wait serverapi", "stop --apphost" }, aspire);
    }

    [TestMethod]
    public async Task AspireStart_Should_GiveTheAppHostTimeForItsFirstBuild()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        var start = host.Runner.Calls.Single(c => c.FileName is "aspire" && c.Arguments.FirstOrDefault() is "start");
        Assert.AreEqual("1200", start.Environment?["ASPIRE_CLI_START_TIMEOUT"]);
    }

    [TestMethod]
    public async Task AspireStart_Should_KeepTheUsersOwnStartTimeout()
    {
        using var host = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["ASPIRE_CLI_START_TIMEOUT"] = "60" });
        host.Runner.Executables["aspire"] = "aspire";
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        var start = host.Runner.Calls.Single(c => c.FileName is "aspire" && c.Arguments.FirstOrDefault() is "start");
        Assert.IsNull(start.Environment);
    }

    [TestMethod]
    public async Task AspireStart_Should_StartNothingWhenAspireCantUseDocker()
    {
        using var host = new TestHost(HostOs.Windows);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "doctor", 1, """{"checks":[{"category":"sdk","name":"dotnet-sdk","status":"pass"},{"category":"container","name":"docker","status":"fail","message":"Docker is running in Windows container mode","fix":"Switch Docker Desktop to Linux containers mode"}]}""");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");
        NeedContainers(project);

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("Docker is running in Windows container mode", result.Detail);
        Assert.AreEqual("Switch Docker Desktop to Linux containers mode", result.Hint);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "docker" || c.Arguments.FirstOrDefault() is "start"));
    }

    [TestMethod]
    public async Task AspireStart_Should_GiveUpWhenDockerCantRunTheContainers()
    {
        using var host = new TestHost(HostOs.Windows);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "wait serverweb", 17, "Timed out waiting for resource 'serverweb' to become healthy.");
        host.Runner.On("aspire", "describe", 0, """{"resources":[{"name":"postgres","displayName":"postgres","state":"RuntimeUnhealthy"},{"name":"serverweb","displayName":"serverweb","state":"Waiting"}]}""");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("Docker can't run postgres: Aspire reports its container runtime as unhealthy", result.Detail);
        Assert.AreEqual(4, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "wait"));
        Assert.AreEqual("stop", host.Runner.Calls.Last().Arguments[0]);
    }

    [TestMethod]
    public async Task AspireStart_Should_StopWaitingOnceAResourceFailsToStart()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "wait serverweb", 17);
        host.Runner.On("aspire", "describe", 0, """{"resources":[{"name":"keycloak","displayName":"keycloak","state":"FailedToStart"},{"name":"serverweb","displayName":"serverweb","state":"Waiting"}]}""");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual("keycloak failed to start", result.Detail);
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "wait"));
    }

    [TestMethod]
    public async Task AspireStart_Should_ShowWhatItIsWaitingFor()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        var waits = 0;
        host.Runner.On("aspire", "wait serverweb", _ => new ProcessResult { ExitCode = ++waits < 3 ? 17 : 0 });
        host.Runner.On("aspire", "describe", 0, """{"resources":[{"name":"postgres","displayName":"postgres","state":"Running","healthStatus":"Unhealthy"},{"name":"mailpit","displayName":"mailpit","state":"Running","healthStatus":"Healthy"},{"name":"serverweb","displayName":"serverweb","state":"Waiting"}]}""");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");
        var progress = new List<string>();

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(progress.Add, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        CollectionAssert.Contains(progress, "Waiting for serverweb: postgres Running (Unhealthy), serverweb Waiting");
    }

    [TestMethod]
    public async Task AspireStart_Should_StartNothingWithoutDocker()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("docker", "info", 1, "Cannot connect to the Docker daemon");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");
        NeedContainers(project);

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(keepRunning: true, _ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("Docker isn't running", result.Detail);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "aspire" && c.Arguments.FirstOrDefault() is "start"));
    }

    [TestMethod]
    public async Task AspireStart_Should_RunWithoutDockerWhenTheProjectDoesntWaitForContainers()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("docker", "info", 1, "Cannot connect to the Docker daemon");
        host.Runner.On("aspire", "start", 0, """{ "appHostPath": "AppHost.csproj", "appHostPid": 1, "cliPid": 2, "dashboardUrl": "https://localhost:2030/login?t=abc", "logFile": "start.log" }""");
        host.Runner.On("aspire", "describe serverweb", 0, """{"resources":[{"name":"serverweb","urls":[{"name":"http","url":"http://localhost:5030","isInternal":false}]}]}""");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");
        var steps = new ProjectSteps(host.Services, project);

        Assert.AreEqual(StepStatus.Skipped, (await steps.AspireStartAsync(_ => { }, CancellationToken.None)).Status, "Without containers, a first start has nothing to get ready.");

        var result = await steps.AspireStartAsync(keepRunning: true, _ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("without its containers", result.Detail);
        Assert.AreEqual("https://localhost:2030/login?t=abc", steps.Running!.DashboardUrl);
        Assert.AreEqual("http://localhost:5030", steps.Running.AppUrl);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "stop"), "A project that keeps running isn't stopped.");

        await steps.Running.StopAsync(true);
        CollectionAssert.AreEqual(new[] { "stop", "--apphost", project.AppHostDirectory, "--non-interactive" }, host.Runner.Detached.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task DotnetRun_Should_StartServerWebAndWaitUntilItIsAlive()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.OnAsync("dotnet", "run", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ProcessResult { ExitCode = 0 };
        });
        var project = CreateFakeProject(host, "Contoso");
        WriteServerWeb(project, "http://localhost:5030;http://*:5030");
        var probes = new List<Uri>();
        var steps = new ProjectSteps(host.Services, project) { AliveProbe = (url, _) => { probes.Add(url); return Task.FromResult(true); } };

        var result = await steps.DotnetRunAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("http://localhost:5030", steps.Running!.AppUrl);
        Assert.AreEqual(new Uri("http://localhost:5030/alive"), probes.Single());
        CollectionAssert.Contains(host.Runner.Calls.Single(c => c.FileName is "dotnet").Arguments.ToArray(), "--no-build");

        await steps.Running.StopAsync(false);
        Assert.IsTrue(steps.Running.Exited!.IsCompleted, "Stopping it ends dotnet run.");
    }

    [TestMethod]
    public async Task DotnetRun_Should_BuildAgainAfterTheMigrationAddedFiles()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.OnAsync("dotnet", "run", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ProcessResult { ExitCode = 0 };
        });
        var project = CreateFakeProject(host, "Contoso");
        WriteServerWeb(project, "http://localhost:5030");
        var steps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(true) };

        Assert.AreEqual(StepStatus.Succeeded, (await steps.MigrationAsync(_ => { }, CancellationToken.None)).Status);
        var result = await steps.DotnetRunAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        CollectionAssert.DoesNotContain(host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "run").Arguments.ToArray(), "--no-build", "The build from before the migration has no Initial migration in it.");

        await steps.Running!.StopAsync(false);
    }

    [TestMethod]
    public async Task DotnetRun_Should_ReportAServerThatExits()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.On("dotnet", "run", 1, "Unhandled exception. System.InvalidOperationException: No database");
        var project = CreateFakeProject(host, "Contoso");
        WriteServerWeb(project, "http://localhost:5030");
        var steps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(false) };

        var result = await steps.DotnetRunAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.IsNull(steps.Running);
    }

    [TestMethod]
    public async Task TheWorkbench_Should_BuildStartAndOpenBeforeTheWebApp()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        WriteWorkbench(project, "http://localhost:6030");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\n");
        host.Runner.OnAsync("dotnet", "run", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ProcessResult { ExitCode = 0 };
        });
        var openedBeforeTheWebBuild = false;
        host.Runner.On("dotnet", "build Contoso.Web.slnf", _ =>
        {
            openedBeforeTheWebBuild = host.Runner.Detached.Any(d => d.Arguments.Contains("http://localhost:6030"));
            return new ProcessResult { ExitCode = 0 };
        });
        var probes = new List<Uri>();
        var projectSteps = new ProjectSteps(host.Services, project) { AliveProbe = (url, _) => { probes.Add(url); return Task.FromResult(true); } };
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, projectSteps, false, false, false, true, ProjectRunMode.UntilClosed, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            "workload list",
            "workload install wasm-tools",
            "build src/Workbench/Contoso.Workbench.csproj",
            $"run --project {project.WorkbenchDirectory} --no-build",
            "restore Contoso.Web.slnf",
            "build Contoso.Web.slnf"
        }, DotnetCommands(host).ToArray());
        Assert.IsTrue(openedBeforeTheWebBuild, "The Workbench opens in the browser as soon as it answers.");
        Assert.AreEqual(new Uri("http://localhost:6030"), probes.Single());
        Assert.AreEqual("Installed WebAssembly build support", steps.Reports.Single(r => r.Id == "workloads").Result.Title);
        Assert.AreEqual("http://localhost:6030", steps.Reports.Single(r => r.Id == "workbench-run").Result.Detail);
        Assert.AreEqual("http://localhost:6030", projectSteps.Workbench?.AppUrl);

        await projectSteps.Workbench!.StopAsync(false);
        Assert.IsTrue(projectSteps.Workbench.Exited!.IsCompleted, "Stopping it ends dotnet run.");
    }

    [TestMethod]
    public async Task AWorkbenchThatDoesntBuild_Should_LeaveTheRestOfTheSetupGoing()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        WriteWorkbench(project, "http://localhost:6030");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        host.Runner.On("dotnet", "build src/Workbench", 1, "Components/Pages/HomePage.razor.cs(12,5): error CS0103: The name 'Foo' does not exist in the current context");
        var projectSteps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(true) };
        var steps = new StepRunner(host.Services);

        Assert.IsTrue(await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, projectSteps, false, false, false, true, ProjectRunMode.UntilClosed, CancellationToken.None));

        var byId = steps.Reports.ToDictionary(r => r.Id, r => r.Result);
        Assert.AreEqual(StepStatus.Failed, byId["workbench-build"].Status);
        Assert.AreEqual("Couldn't build the Workbench", byId["workbench-build"].Title);
        Assert.AreEqual("CS0103", byId["workbench-build"].Detail);
        StringAssert.EndsWith(byId["workbench-build"].FollowUp, "dotnet build src/Workbench/Contoso.Workbench.csproj");
        Assert.AreEqual(StepStatus.Skipped, byId["workbench-run"].Status);
        Assert.AreEqual("its build failed", byId["workbench-run"].Detail);
        Assert.AreEqual(StepStatus.Succeeded, byId["restore"].Status);
        Assert.AreEqual(StepStatus.Succeeded, byId["build"].Status);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "run"));
        Assert.IsNull(projectSteps.Workbench);
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task AWorkbenchThatDoesntStart_Should_LeaveTheRestOfTheSetupGoing()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        WriteWorkbench(project, "http://localhost:6030");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        host.Runner.On("dotnet", "run", 1, "Unhandled exception. System.IO.IOException: Failed to bind to address http://127.0.0.1:6030: address already in use.");
        var projectSteps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(false) };
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, projectSteps, false, false, false, true, ProjectRunMode.UntilClosed, CancellationToken.None);

        var started = steps.Reports.Single(r => r.Id == "workbench-run").Result;
        Assert.AreEqual(StepStatus.Warning, started.Status);
        Assert.AreEqual("Couldn't start the Workbench", started.Title);
        StringAssert.EndsWith(started.FollowUp, $"dotnet run --project {Path.Combine("src", "Workbench")}");
        Assert.IsFalse(steps.AnyFailed);
        Assert.AreEqual(StepStatus.Succeeded, steps.Reports.Single(r => r.Id == "build").Result.Status);
        Assert.IsNull(projectSteps.Workbench);
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task TheWorkbench_Should_OnlyBeBuiltWhenTheProjectIsntRunUntilTheTerminalCloses()
    {
        foreach (var mode in new[] { ProjectRunMode.None, ProjectRunMode.Once })
        {
            using var host = new TestHost(HostOs.Windows);
            var project = CreateFakeProject(host, "Contoso");
            WriteWorkbench(project, "http://localhost:6030");
            host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
            var steps = new StepRunner(host.Services);

            await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, true, mode, CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "workload list", "build src/Workbench/Contoso.Workbench.csproj", "restore Contoso.Web.slnf", "build Contoso.Web.slnf" }, DotnetCommands(host).ToArray(), mode.ToString());
            Assert.IsFalse(steps.Reports.Any(r => r.Id is "workbench-run"), mode.ToString());
            Assert.IsEmpty(host.Runner.Detached, mode.ToString());
        }
    }

    [TestMethod]
    public async Task AProjectWithoutAWorkbench_Should_GoStraightToTheWebApp()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, true, ProjectRunMode.UntilClosed, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "workload list", "restore Contoso.Web.slnf", "build Contoso.Web.slnf" }, DotnetCommands(host).ToArray());
        Assert.IsFalse(steps.Reports.Any(r => r.Id.StartsWith("workbench", StringComparison.Ordinal)));
        Assert.IsFalse(host.Output.Contains("Workbench", StringComparison.Ordinal), host.Output);
        Assert.IsEmpty(host.Runner.Detached);
    }

    [TestMethod]
    public async Task TheProject_Should_OpenInTheBrowserAsSoonAsItStarts()
    {
        using var host = new TestHost(HostOs.Windows);
        host.Runner.OnAsync("dotnet", "run", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ProcessResult { ExitCode = 0 };
        });
        var project = CreateFakeProject(host, "Contoso");
        WriteServerWeb(project, "http://localhost:5030");
        var projectSteps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(true) };

        await ProjectRun.RunProjectAsync(host.Services, new StepRunner(host.Services), project, projectSteps, ProjectRunMode.UntilClosed, aspire: false, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "url.dll,FileProtocolHandler", "http://localhost:5030" }, host.Runner.Detached.Single().Arguments.ToArray());

        await projectSteps.Running!.StopAsync(false);
    }

    [TestMethod]
    public async Task KeepRunning_Should_ListAndStopTheWorkbenchWithTheProject()
    {
        using var host = new TestHost(HostOs.Windows);
        var stopped = new List<(string What, bool Detached)>();
        var project = new RunningProject { DashboardUrl = "https://localhost:17030/login?t=abc", AppUrl = "http://localhost:5030", StopAsync = detached => { stopped.Add(("project", detached)); return Task.CompletedTask; } };
        var workbench = new RunningProject { AppUrl = "http://localhost:6030", Exited = new TaskCompletionSource().Task, StopAsync = detached => { stopped.Add(("workbench", detached)); return Task.CompletedTask; } };
        using var closed = new CancellationTokenSource();
        await closed.CancelAsync();

        await ProjectRun.KeepRunningAsync(host.Services, project, workbench, closed.Token);

        StringAssert.Contains(host.FlatOutput, "Running http://localhost:6030 The Workbench https://localhost:17030/login?t=abc Aspire dashboard http://localhost:5030 The app");
        StringAssert.Contains(host.FlatOutput, "They keep running while this terminal is open. Close it, or press Ctrl+C, to stop the project and the Workbench.");
        StringAssert.Contains(host.FlatOutput, "Stopping the project and the Workbench");
        CollectionAssert.AreEquivalent(new[] { ("workbench", true), ("project", true) }, stopped);
        Assert.IsEmpty(host.Runner.Detached, "Each one opened in the browser as soon as it started.");
    }

    [TestMethod]
    public async Task AWorkbenchThatStops_Should_LeaveTheProjectRunning()
    {
        using var host = new TestHost(HostOs.Windows);
        using var closed = new CancellationTokenSource();
        var stopped = new List<(string What, bool Detached)>();
        var project = new RunningProject { AppUrl = "http://localhost:5030", StopAsync = detached => { stopped.Add(("project", detached)); return Task.CompletedTask; } };
        var workbench = new RunningProject { AppUrl = "http://localhost:6030", Exited = Task.CompletedTask, StopAsync = async detached => { stopped.Add(("workbench", detached)); await closed.CancelAsync(); } };

        await ProjectRun.KeepRunningAsync(host.Services, project, workbench, closed.Token);

        StringAssert.Contains(host.FlatOutput, "The Workbench stopped. Stopping the project");
        CollectionAssert.AreEqual(new[] { ("workbench", false), ("project", true) }, stopped);
    }

    [TestMethod]
    public void TheProject_Should_KeepRunningOnlyForSomeoneAtTheTerminal()
    {
        using var redirected = new TestHost(HostOs.Linux);
        Assert.AreEqual(ProjectRunMode.Once, ProjectRun.Mode(redirected.Services, aspire: true, noBuild: false, noRun: false));
        Assert.AreEqual(ProjectRunMode.None, ProjectRun.Mode(redirected.Services, aspire: false, noBuild: false, noRun: false));

        using var terminal = new TestHost(HostOs.Linux, prompter: new ScriptedPrompter());
        Assert.AreEqual(ProjectRunMode.UntilClosed, ProjectRun.Mode(terminal.Services, aspire: true, noBuild: false, noRun: false));
        Assert.AreEqual(ProjectRunMode.UntilClosed, ProjectRun.Mode(terminal.Services, aspire: false, noBuild: false, noRun: false));
        Assert.AreEqual(ProjectRunMode.None, ProjectRun.Mode(terminal.Services, aspire: true, noBuild: false, noRun: true));
        Assert.AreEqual(ProjectRunMode.None, ProjectRun.Mode(terminal.Services, aspire: true, noBuild: true, noRun: false));

        using var agent = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["CLAUDECODE"] = "1" }, new ScriptedPrompter());
        Assert.AreEqual(ProjectRunMode.Once, ProjectRun.Mode(agent.Services, aspire: true, noBuild: false, noRun: false), "A coding agent's shell would wait for ever.");

        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["CI"] = "true" }, new ScriptedPrompter());
        Assert.AreEqual(ProjectRunMode.None, ProjectRun.Mode(ci.Services, aspire: true, noBuild: false, noRun: false));
    }

    [TestMethod]
    public void TheAppHost_Should_TellWhetherTheProjectWaitsForContainers()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        Assert.IsFalse(ProjectContext.AppHostNeedsContainers(project.AppHostDirectory));

        NeedContainers(project);
        Assert.IsTrue(ProjectContext.AppHostNeedsContainers(project.AppHostDirectory));
    }

    [TestMethod]
    public async Task AspireStart_Should_StopAProjectThatDoesntGetHealthy()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "wait serverweb", 1, "Timed out waiting for resource 'serverweb' to become healthy.");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("Aspire started, serverweb didn't get healthy", result.Title);
        CollectionAssert.AreEqual(new[] { "stop", "--apphost", project.AppHostDirectory, "--non-interactive" }, host.Runner.Calls.Last().Arguments.ToArray());
    }

    [TestMethod]
    public async Task AspireStart_Should_LeaveAnAppHostThatWasAlreadyRunning()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");
        host.Runner.On("aspire", "ps", 0, $$"""[{ "appHostPath": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(project.AppHostDirectory, "Contoso.Server.AppHost.csproj"))}} }]""");
        host.Runner.On("aspire", "start", 1, "The configuration is invalid.");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() == "stop"), string.Join("\n", host.Runner.Calls.Select(c => c.CommandLine)));
    }

    [TestMethod]
    public async Task AspireStart_Should_SayWhenItCouldntStopTheProject()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "stop", 1, "Failed to stop the AppHost.");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status, result.Detail);
        Assert.AreEqual("Started the project with Aspire, but couldn't stop it", result.Title);
        StringAssert.Contains(result.FollowUp, "aspire stop");
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "stop"));
    }

    [TestMethod]
    public async Task AspireStart_Should_StopWhatAFailedStartLeftRunning()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "start", 1, "The AppHost didn't start in time.");
        var project = CreateFakeProject(host, "Contoso");
        WriteAppHost(project, "serverweb");

        var result = await new ProjectSteps(host.Services, project).AspireStartAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        CollectionAssert.AreEqual(new[] { "stop", "--apphost", project.AppHostDirectory, "--non-interactive" }, host.Runner.Calls.Last().Arguments.ToArray());
    }

    [TestMethod]
    public void TheProjectContext_Should_ReadTheGeneratedProject()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");

        Assert.AreEqual("Contoso", ProjectContext.FindProjectName(project.Directory));
        Assert.AreEqual("net10.0", project.TargetFrameworkVersion);
        Assert.AreEqual("10.0.12", project.PackageVersion("Microsoft.EntityFrameworkCore.Design"));
        Assert.AreEqual("PostgreSQL", project.Database);
        Assert.IsTrue(project.Exists);
    }

    [TestMethod]
    public async Task StepsOfAProjectThatWasntCreated_Should_BeSkippedNotFailed()
    {
        using var host = new TestHost();
        var project = new ProjectContext { Name = "Missing", Directory = Path.Combine(host.WorkingDirectory, "Missing"), Platforms = new HashSet<Platform> { Platform.Web, Platform.Android } };
        var steps = new StepRunner(host.Services);
        var projectSteps = new ProjectSteps(host.Services, project);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, projectSteps, false, false, false, false, ProjectRunMode.UntilClosed, CancellationToken.None);
        await NewWorkflow.RunNativeStepsAsync(host.Services, steps, project, projectSteps, false, false, false, CancellationToken.None);

        Assert.IsTrue(steps.Reports.Any(r => r.Id is "native-build"), string.Join(Environment.NewLine, steps.Reports.Select(r => r.Id)));
        Assert.IsTrue(steps.Reports.All(r => r.Result.Status is StepStatus.Skipped));
        Assert.IsFalse(steps.AnyFailed);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task AWorkloadInstallRetriedWithSudo_Should_RunTheDotnetOnThePath()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.Executables["dotnet"] = "/opt/dotnet/dotnet";
        host.Runner.Executables["sudo"] = "/usr/bin/sudo";
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\n");
        host.Runner.On("dotnet", "workload install", 1, "Inadequate permissions. Run the command with elevated privileges.");

        var result = await new ProjectSteps(host.Services, project).WorkloadsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("sudo", host.Runner.Calls.Last().FileName);
        CollectionAssert.AreEqual(new[] { "-n", "/opt/dotnet/dotnet", "workload", "install", "wasm-tools" }, host.Runner.Calls.Last().Arguments.ToArray(), "sudo's secure_path may not have the user's dotnet, or may have another one.");
    }

    [TestMethod]
    public async Task ANativeApp_Should_BuildWhatTheWebAppsBuildLeftOutWithWhatItNeeds()
    {
        using var host = new TestHost(HostOs.Windows);
        var web = CreateFakeProject(host, "Contoso");
        var project = new ProjectContext { Name = web.Name, Directory = web.Directory, Platforms = new HashSet<Platform> { Platform.Web, Platform.Windows } };
        WriteSolution(project);
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);
        var projectSteps = new ProjectSteps(host.Services, project);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, projectSteps, false, false, false, false, ProjectRunMode.None, CancellationToken.None);

        var webCommands = DotnetCommands(host);
        CollectionAssert.AreEqual(new[] { "workload list", "restore Contoso.Web.slnf", "build Contoso.Web.slnf" }, webCommands.ToArray(), "Only the web app's build support, packages and build come before the native apps.");

        await NewWorkflow.RunNativeStepsAsync(host.Services, steps, project, projectSteps, false, false, false, CancellationToken.None);

        var commands = DotnetCommands(host).Skip(webCommands.Count).ToList();
        CollectionAssert.AreEqual(new[]
        {
            "workload list",
            "workload install maui",
            "build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=True",
            "restore Contoso.slnx",
            "build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj",
            "build src/Client/Contoso.Client.Windows/Contoso.Client.Windows.csproj"
        }, commands.ToArray(), string.Join(Environment.NewLine, commands));
        Assert.AreEqual("Built the web app", steps.Reports.Single(r => r.Id == "build").Result.Title);
        CollectionAssert.AreEqual(new[] { "Built Contoso.Client.Maui", "Built Contoso.Client.Windows" }, steps.Reports.Where(r => r.Id == "native-build").Select(r => r.Result.Title).ToArray());
        Assert.AreEqual("Installed Windows build support", steps.Reports.Single(r => r.Id == "native-workloads").Result.Title);
    }

    [TestMethod]
    public void TheNativeBuild_Should_CoverOnlyWhatTheWebFilterAndTheWorkbenchLeaveOut()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        WriteSolution(project);
        WriteWorkbench(project, "http://localhost:6030");

        CollectionAssert.AreEqual(new[] { "src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj", "src/Client/Contoso.Client.Windows/Contoso.Client.Windows.csproj" }, project.ProjectsOutsideWebFilter.ToArray());

        File.Delete(project.Solution);
        Assert.IsEmpty(project.ProjectsOutsideWebFilter);
    }

    [TestMethod]
    [DataRow("Xcode 26.6\nBuild version 17E101", StepStatus.Succeeded)]
    [DataRow("Xcode 26.6.1\nBuild version 17E120", StepStatus.Succeeded)]
    [DataRow("Xcode 26.2\nBuild version 17C52", StepStatus.Warning)]
    public async Task Xcode_Should_MatchTheOneDotnetForIosWants(string xcodeVersion, StepStatus expected)
    {
        using var host = new TestHost(HostOs.MacOS);
        var project = CreateFakeProject(host, "Contoso");
        var pack = Directory.CreateDirectory(Path.Combine(host.Root, "packs", "Microsoft.iOS.Sdk.net10.0_26.5", "26.5.10318")).FullName;
        File.WriteAllText(Path.Combine(pack, "Versions.plist"), "<plist><dict>\n\t<key>RecommendedXcodeVersion</key>\n\t<string>26.6</string>\n</dict></plist>");
        host.Runner.On("dotnet", "msbuild", 0, pack + Path.DirectorySeparatorChar + "\n");
        host.Runner.On("xcodebuild", "-version", 0, xcodeVersion);

        var result = await new ProjectSteps(host.Services, project).XcodeAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(expected, result.Status, result.Detail);
        CollectionAssert.Contains(host.Runner.Calls.First().Arguments.ToList(), "-p:TargetFramework=net10.0-ios");

        if (expected is StepStatus.Warning)
        {
            Assert.AreEqual("26.2 here, 26.6 wanted", result.Detail);
            StringAssert.Contains(result.Hint, "Xcode 26.6");
        }
    }

    [TestMethod]
    public async Task AFailedStep_Should_NotStopTheNextOnes()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.On("dotnet", "restore", 1, "x.csproj : error NU1301: Unable to load the service index for source https://nuget.contoso.local/v3/index.json. [x.csproj]");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, ProjectRunMode.None, CancellationToken.None);

        var byId = steps.Reports.ToDictionary(r => r.Id, r => r.Result);
        Assert.AreEqual(StepStatus.Failed, byId["restore"].Status);
        CollectionAssert.AreEqual(new[] { "NU1301" }, byId["restore"].Codes.ToArray());
        StringAssert.StartsWith(byId["restore"].Hint, "error NU1301");
        Assert.AreEqual(StepStatus.Succeeded, byId["build"].Status);
        StringAssert.Contains(byId["restore"].FollowUp, "dotnet restore Contoso.Web.slnf");
    }

    [TestMethod]
    public async Task AFailedStep_Should_ShowItsWholeFirstError()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        var error = $"error : The SupportedOSPlatformVersion value '15.0' in the project file is lower than the minimum value '17.0'. {string.Join(' ', Enumerable.Repeat("detail", 60))} end-of-error";
        host.Runner.On("dotnet", "restore", 1, $"/x/Xamarin.Shared.Sdk.targets(1,1): {error} [/x/Contoso.Client.Maui.csproj]");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, ProjectRunMode.None, CancellationToken.None);

        Assert.AreEqual(error, steps.Reports.Single(r => r.Id == "restore").Result.Hint);
        StringAssert.Contains(host.Output, "end-of-error");
    }

    [TestMethod]
    public void RecommendedExtensions_Should_BeReadDespiteCommentsAndRepeats()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        WriteRecommendations(project, "ms-dotnettools.csharp", "Anthropic.claude-code", "ms-dotnettools.csharp");

        CollectionAssert.AreEqual(new[] { "ms-dotnettools.csharp", "Anthropic.claude-code" }, ProjectSteps.ReadRecommendedExtensions(project.Directory).ToArray());
    }

    [TestMethod]
    public async Task ExtensionsVsCodeBuildsIn_Should_CountAsInstalled()
    {
        using var host = new TestHost(HostOs.Linux);
        var (project, vsCode) = FakeVsCode(host, "ms-dotnettools.csharp");
        WriteRecommendations(project, "Anthropic.claude-code", "vscode.git");
        host.Runner.On(vsCode.Name, "--install-extension", spec =>
        {
            vsCode.Installed.Add("anthropic.claude-code");
            return new ProcessResult { ExitCode = 1, Output = "Error while installing extension vscode.git: Extension 'vscode.git' is a built-in extension with version '1.112.0' and cannot be downgraded to version '1.0.0'.\nFailed Installing Extensions: vscode.git" };
        });

        var result = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.AreEqual("Installed 1 VS Code extension", result.Title);
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "--install-extension"));
    }

    [TestMethod]
    public async Task AFailedExtensionInstall_Should_BeRetriedOnceAndNamed()
    {
        using var host = new TestHost(HostOs.Linux);
        var (project, vsCode) = FakeVsCode(host);
        WriteRecommendations(project, "Anthropic.claude-code", "EditorConfig.EditorConfig");
        var attempts = 0;
        host.Runner.On(vsCode.Name, "--install-extension", spec =>
        {
            vsCode.Installed.Add("editorconfig.editorconfig");
            return new ProcessResult { ExitCode = 1, Output = ++attempts == 1 ? "error: the marketplace can't be reached" : "Failed Installing Extensions: anthropic.claude-code" };
        });

        var result = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status, "An extension that won't install is a note, not a failed run.");
        Assert.AreEqual("Couldn't install 1 VS Code extension", result.Title);
        Assert.AreEqual("claude-code", result.Detail);
        Assert.AreEqual($"{ProcessSpec.Quote(vsCode.Path)} --install-extension Anthropic.claude-code", result.FollowUp);
        Assert.AreEqual(2, attempts);
        CollectionAssert.AreEqual(new[] { "--install-extension", "Anthropic.claude-code" }, host.Runner.Calls.Where(c => c.Arguments.FirstOrDefault() is "--install-extension").Last().Arguments.ToArray());
    }

    [TestMethod]
    public async Task VsCode_Should_GetOnlyTheExtensionsItLacks()
    {
        using var host = new TestHost(HostOs.Linux);
        var (project, vsCode) = FakeVsCode(host, "ms-dotnettools.csharp", "EditorConfig.EditorConfig");
        WriteRecommendations(project, "EditorConfig.EditorConfig", "Anthropic.claude-code", "ms-dotnettools.csharp");
        host.Runner.On(vsCode.Name, "--install-extension", spec =>
        {
            vsCode.Installed.UnionWith(spec.Arguments.Where(a => a.StartsWith('-') is false));
            return new ProcessResult { ExitCode = 0 };
        });

        var result = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("Installed 1 VS Code extension", result.Title);
        var install = host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "--install-extension");
        CollectionAssert.AreEqual(new[] { "--install-extension", "Anthropic.claude-code" }, install.Arguments.ToArray());

        var done = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual("Extensions already installed", done.Title);
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "--install-extension"));
    }

    private sealed record FakeVsCodeInstall(string Path, string Name, HashSet<string> Installed);

    private static (ProjectContext Project, FakeVsCodeInstall VsCode) FakeVsCode(TestHost host, params string[] installed)
    {
        var code = typeof(ProjectStepTests).Assembly.Location;
        var vsCode = new FakeVsCodeInstall(code, Path.GetFileNameWithoutExtension(code), new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase));
        host.Runner.Executables["code"] = code;
        host.Runner.On(vsCode.Name, "--list-extensions", _ => new ProcessResult { ExitCode = 0, Output = string.Join('\n', vsCode.Installed) });
        return (CreateFakeProject(host, "Contoso"), vsCode);
    }

    [TestMethod]
    public async Task Playwright_Should_InstallChromiumWithTheTestsOwnDriver()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        var playwright = Directory.CreateDirectory(Path.Combine(project.Directory, "src", "Tests", "bin", "Debug", "net10.0", ".playwright")).FullName;
        Directory.CreateDirectory(Path.Combine(playwright, "package"));
        File.WriteAllText(Path.Combine(playwright, "package", "cli.js"), "");
        File.WriteAllText(Path.Combine(playwright, "package", "package.json"), "{ \"name\": \"playwright-core\", \"version\": \"1.57.0\" }");
        var nodeDirectory = Directory.CreateDirectory(Path.Combine(playwright, "node", "win32_x64")).FullName;
        File.WriteAllText(Path.Combine(nodeDirectory, "node.exe"), "");

        var result = await new ProjectSteps(host.Services, project).PlaywrightAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("Playwright 1.57.0", result.Detail);
        var call = host.Runner.Calls.Single();
        Assert.AreEqual(Path.Combine(nodeDirectory, "node.exe"), call.FileName);
        CollectionAssert.AreEqual(new[] { Path.Combine(playwright, "package", "cli.js"), "install", "chromium" }, call.Arguments.ToArray());
    }

    [TestMethod]
    public async Task Playwright_Should_BeSkippedWithoutBuiltTests()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");

        var notBuilt = await new ProjectSteps(host.Services, project).PlaywrightAsync(_ => { }, CancellationToken.None);
        Assert.AreEqual(StepStatus.Skipped, notBuilt.Status);
        StringAssert.Contains(notBuilt.Detail, "weren't built");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Playwright_Should_InstallEveryBrowserWithItsLibrariesInCi()
    {
        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true" });
        ci.Runner.Executables["sudo"] = "/usr/bin/sudo";
        ci.Runner.Executables["apt-get"] = "/usr/bin/apt-get";
        var project = CreateFakeProject(ci, "Contoso");
        var playwright = Directory.CreateDirectory(Path.Combine(project.Directory, "src", "Tests", "bin", "Debug", "net10.0", ".playwright")).FullName;
        Directory.CreateDirectory(Path.Combine(playwright, "package"));
        File.WriteAllText(Path.Combine(playwright, "package", "cli.js"), "");
        var nodeDirectory = Directory.CreateDirectory(Path.Combine(playwright, "node", "linux-x64")).FullName;
        File.WriteAllText(Path.Combine(nodeDirectory, "node"), "");
        var cliScript = Path.Combine(playwright, "package", "cli.js");

        var result = await new ProjectSteps(ci.Services, project).PlaywrightAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.AreEqual("Installed Playwright's browsers", result.Title);
        CollectionAssert.AreEqual(new[] { cliScript, "install" }, ci.Runner.Calls.First().Arguments.ToArray());
        CollectionAssert.AreEqual(new[] { "-n", Path.Combine(nodeDirectory, "node"), cliScript, "install-deps" }, ci.Runner.Calls.Last().Arguments.ToArray());
        Assert.IsNotNull(ci.Runner.Calls.First().Timeout);
        Assert.AreEqual("sudo", ci.Runner.Calls.Last().FileName);
        Assert.IsNull(ci.Runner.Calls.Last().Timeout);
    }

    [TestMethod]
    public async Task Playwright_Should_LeaveTheSystemLibrariesToALinuxWithoutAptGet()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["sudo"] = "/usr/bin/sudo";
        host.Runner.Executables["dnf"] = "/usr/bin/dnf";
        var project = CreateFakeProject(host, "Contoso");
        var playwright = Directory.CreateDirectory(Path.Combine(project.Directory, "src", "Tests", "bin", "Debug", "net10.0", ".playwright")).FullName;
        Directory.CreateDirectory(Path.Combine(playwright, "package"));
        File.WriteAllText(Path.Combine(playwright, "package", "cli.js"), "");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(playwright, "node", "linux-x64")).FullName, "node"), "");

        var result = await new ProjectSteps(host.Services, project).PlaywrightAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.IsNull(result.FollowUp);
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("install-deps")), "Playwright's install-deps runs only apt-get.");
    }

    [TestMethod]
    public async Task TheAndroidSdk_Should_BeCompletedByMauiInCiToo()
    {
        using var ci = new TestHost(HostOs.Linux, new Dictionary<string, string> { ["GITHUB_ACTIONS"] = "true", ["ANDROID_HOME"] = "/usr/local/lib/android/sdk", ["JAVA_HOME"] = "/usr/lib/jvm/temurin-17" });

        var result = await new ProjectSteps(ci.Services, CreateFakeProject(ci, "Contoso")).AndroidDependenciesAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        StringAssert.Contains(ci.Runner.Calls.Single().CommandLine, "-t:InstallAndroidDependencies");
    }

    [TestMethod]
    public async Task TheAndroidSdk_Should_AskForAnElevatedTerminal_WhenItsFolderNeedsAdministratorRights()
    {
        using var host = new TestHost(HostOs.Windows);
        host.Runner.On("dotnet", "build", 1, @"error : Failure to move Android component to its destination path. System.UnauthorizedAccessException: Access to the path 'C:\Program Files (x86)\Android\android-sdk\build-tools\36.0.0' is denied.");

        var result = await new ProjectSteps(host.Services, CreateFakeProject(host, "Contoso")).AndroidDependenciesAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("The Android SDK and Java need administrator rights", result.Title);
        Assert.AreEqual("run it in an elevated terminal", result.Detail);
        StringAssert.Contains(result.FollowUp, "-t:InstallAndroidDependencies");
    }

    [TestMethod]
    public async Task TheAndroidSdk_Should_ReportAFailureThatIsNotAboutAccess()
    {
        using var host = new TestHost(HostOs.Windows);
        host.Runner.On("dotnet", "build", 1, "error XA5300: The Java SDK directory could not be found.");

        var result = await new ProjectSteps(host.Services, CreateFakeProject(host, "Contoso")).AndroidDependenciesAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Failed, result.Status);
        Assert.AreEqual("Couldn't install the Android SDK and Java", result.Title);
    }

    [TestMethod]
    public async Task Workloads_Should_SeeThatTheyNeedSudoWhateverTheMachinesLanguage()
    {
        using var host = new TestHost(HostOs.MacOS, new Dictionary<string, string> { ["LANG"] = "de_DE.UTF-8" });
        host.Runner.Executables["sudo"] = "/usr/bin/sudo";
        host.Runner.On("dotnet", "workload install", spec => new ProcessResult
        {
            ExitCode = 1,
            Output = spec.Environment?.GetValueOrDefault("DOTNET_CLI_UI_LANGUAGE") is "en"
                ? "Inadequate permissions. Run the command with elevated privileges."
                : "Unzureichende Berechtigungen. Führen Sie den Befehl mit erhöhten Rechten aus."
        });

        await new ProjectSteps(host.Services, CreateFakeProject(host, "Contoso")).WorkloadsAsync(_ => { }, CancellationToken.None);

        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName is "sudo"), string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine)));
    }

    [TestMethod]
    public async Task BuildProperties_Should_ReachTheRestoreAndTheBuildButNotTheBrowsers()
    {
        using var host = new TestHost(HostOs.Linux);
        var web = CreateFakeProject(host, "Contoso");
        var project = new ProjectContext { Name = web.Name, Directory = web.Directory, Platforms = web.Platforms, BuildProperties = ["EnforceCodeStyleInBuild=true", "Environment=Staging"] };
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), true, false, false, true, ProjectRunMode.None, CancellationToken.None);

        var commands = host.Runner.Calls.Select(c => string.Join(' ', c.Arguments)).ToList();
        CollectionAssert.Contains(commands, "restore Contoso.Web.slnf -p:EnforceCodeStyleInBuild=true -p:Environment=Staging");
        CollectionAssert.Contains(commands, "build Contoso.Web.slnf -p:EnforceCodeStyleInBuild=true -p:Environment=Staging");
        Assert.IsFalse(steps.Reports.Any(r => r.Id is "playwright"));
    }

    [TestMethod]
    public async Task TheGitHubRepo_Should_BePrivateWithDevelopAndMain()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        project.GitReady = true;
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("gh", "repo view", 0, "https://github.com/contoso-dev/Contoso\n");

        var result = await new ProjectSteps(host.Services, project).GitHubRepositoryAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.AreEqual("github.com/contoso-dev/Contoso", result.Detail);

        var calls = host.Runner.Calls.Select(c => c.CommandLine).ToList();
        var create = calls.IndexOf("gh repo create Contoso --private --source . --remote origin");
        var pushDevelop = calls.IndexOf("git push -u origin develop");
        var pushMain = calls.IndexOf("git push -u origin main");
        var defaultBranch = calls.IndexOf("gh repo edit --default-branch develop");
        var remoteHead = calls.IndexOf("git remote set-head origin develop");

        Assert.IsTrue(create >= 0 && create < pushDevelop && pushDevelop < pushMain && pushMain < defaultBranch && defaultBranch < remoteHead, string.Join(Environment.NewLine, calls));
        Assert.IsTrue(host.Runner.Calls.All(c => c.WorkingDirectory == project.Directory));
    }

    [TestMethod]
    public async Task Format_Should_FormatTheWholeSolutionWithoutRestoringItAgain()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");

        var result = await new ProjectSteps(host.Services, project).FormatAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        CollectionAssert.AreEqual(new[] { "format", "Contoso.slnx", "--no-restore", "--exclude-diagnostics", "BL0016" }, host.Runner.Calls.Single(c => c.FileName is "dotnet").Arguments.ToArray());
    }

    [TestMethod]
    public async Task AFailedStep_Should_SayWhatBitRanBeforeTheError()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        project.GitReady = true;
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("git", "push -u origin develop", 128, "fatal: unable to access 'https://github.com/contoso-dev/Contoso.git/': Could not resolve host: github.com");

        await new StepRunner(host.Services).RunAsync("github-repo", "Creating a private GitHub repo", new ProjectSteps(host.Services, project).GitHubRepositoryAsync, CancellationToken.None);

        var attempt = $"bit ran this in {project.Directory}: git push -u origin develop";
        StringAssert.Contains(host.Output, attempt);
        Assert.IsLessThan(host.Output.IndexOf("fatal: unable to access", StringComparison.Ordinal), host.Output.IndexOf(attempt, StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task TheGitHubRepo_Should_SayWhenDevelopIsntItsDefaultBranch()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        project.GitReady = true;
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("gh", "repo edit", 1, "HTTP 403: Resource not accessible by integration");
        host.Runner.On("gh", "repo view --json url", 0, "https://github.com/contoso-dev/Contoso\n");
        host.Runner.On("gh", "repo view --json defaultBranchRef", 0, "develop\n");

        var steps = new ProjectSteps(host.Services, project);

        Assert.AreEqual(StepStatus.Succeeded, (await steps.GitHubRepositoryAsync(_ => { }, CancellationToken.None)).Status);

        host.Runner.On("gh", "repo view --json defaultBranchRef", 0, "main\n");
        var result = await steps.GitHubRepositoryAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("github.com/contoso-dev/Contoso", result.Detail);
        StringAssert.EndsWith(result.FollowUp, "gh repo edit --default-branch develop");
    }

    [TestMethod]
    public async Task TheGitHubRepo_Should_PushMainOnlyAfterDevelop()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        project.GitReady = true;
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("git", "push -u origin develop", 1, "fatal: unable to access");

        var result = await new ProjectSteps(host.Services, project).GitHubRepositoryAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Failed, result.Status);
        StringAssert.EndsWith(result.FollowUp, "git push -u origin develop && git push -u origin main && gh repo edit --default-branch develop");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["push", "-u", "origin", "main"]));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["repo", "edit", ..]));
    }

    [TestMethod]
    public async Task TheGitHubRepo_Should_WaitForTheGitHubCliAndASignIn()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        project.GitReady = true;
        var steps = new ProjectSteps(host.Services, project);

        var missing = await steps.GitHubRepositoryAsync(_ => { }, CancellationToken.None);
        Assert.AreEqual(StepStatus.Failed, missing.Status);

        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("gh", "auth status", 1, "You are not logged into any GitHub hosts.");

        var signedOut = await steps.GitHubRepositoryAsync(_ => { }, CancellationToken.None);
        Assert.AreEqual(StepStatus.Warning, signedOut.Status);
        StringAssert.StartsWith(signedOut.FollowUp, "gh auth login --web && cd ");
        StringAssert.Contains(signedOut.FollowUp, "gh repo create Contoso --private --source . --remote origin");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments is ["repo", "create", ..]));

        project.GitReady = false;
        Assert.AreEqual(StepStatus.Skipped, (await steps.GitHubRepositoryAsync(_ => { }, CancellationToken.None)).Status);
    }

    [TestMethod]
    public async Task GitHubSignIn_Should_SetUpGitFirstSoGhAsksNothingButTheCode()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.Executables["gh"] = "/usr/bin/gh";

        var result = await new ProjectSteps(host.Services, project).GitHubSignInAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status);
        CollectionAssert.AreEqual(new[] { "auth setup-git --hostname github.com --force", "auth login --web --clipboard --git-protocol https --hostname github.com" },
            host.Runner.Calls.Where(c => c.FileName is "gh").Select(c => string.Join(' ', c.Arguments)).ToArray());
    }

    [TestMethod]
    public async Task GitHubSignIn_Should_ShowTheCodeAndLetEscSkipIt()
    {
        using var host = new TestHost(HostOs.Windows);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        var stopped = false;
        host.Runner.OnAsync("gh", "auth login", async (spec, ct) =>
        {
            spec.OnOutputLine?.Invoke("! One-time code (AB12-CD34) copied to clipboard");
            spec.OnOutputLine?.Invoke("Open this URL to continue in your web browser: https://github.com/login/device");

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            finally
            {
                stopped = true;
            }

            return new ProcessResult { ExitCode = 0 };
        });

        var result = await new ProjectSteps(host.Services, project) { SkipRequested = _ => Task.CompletedTask }.GitHubSignInAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Warning, result.Status);
        Assert.AreEqual("Skipped signing in to GitHub", result.Title);
        Assert.IsTrue(stopped, "Skipping stops gh's sign-in.");
        StringAssert.Contains(host.FlatOutput, "Enter AB12-CD34 (copied to the clipboard)");
        StringAssert.Contains(host.FlatOutput, "Press Esc to skip");
        CollectionAssert.AreEqual(new[] { "https://github.com/login/device" }, host.Runner.Detached.SelectMany(d => d.Arguments).Where(a => a.StartsWith("https", StringComparison.Ordinal)).ToArray());
    }

    [TestMethod]
    public async Task GitHubSignIn_Should_SayTheCodeIsCopiedOnlyWhenGhCopiedIt()
    {
        using var host = new TestHost(HostOs.Linux);
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("gh", "auth login", spec =>
        {
            spec.OnOutputLine?.Invoke("! First copy your one-time code: AB12-CD34");
            return new ProcessResult { ExitCode = 0 };
        });

        await new ProjectSteps(host.Services, project).GitHubSignInAsync(_ => { }, CancellationToken.None);

        StringAssert.Contains(host.FlatOutput, "Enter AB12-CD34 at https://github.com/login/device");
    }

    [TestMethod]
    public async Task GitHubSignIn_Should_NeedATerminalButNotPrompts()
    {
        using var host = new TestHost(prompter: new ScriptedPrompter());
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.Executables["gh"] = "/usr/bin/gh";
        host.Runner.On("gh", "auth status", 1);
        host.Services.DisablePrompts();

        Assert.IsTrue(await new ProjectSteps(host.Services, project).CanSignInToGitHubAsync(CancellationToken.None));

        host.Runner.On("gh", "auth status", 0);
        Assert.IsFalse(await new ProjectSteps(host.Services, project).CanSignInToGitHubAsync(CancellationToken.None));

        using var noTerminal = new TestHost();
        noTerminal.Runner.Executables["gh"] = "/usr/bin/gh";
        noTerminal.Runner.On("gh", "auth status", 1);
        Assert.IsFalse(await new ProjectSteps(noTerminal.Services, CreateFakeProject(noTerminal, "Contoso")).CanSignInToGitHubAsync(CancellationToken.None));
    }

    private static void WriteAppHost(ProjectContext project, params string[] resources)
    {
        var folder = Directory.CreateDirectory(project.AppHostDirectory).FullName;
        File.WriteAllLines(Path.Combine(folder, "Program.cs"), resources.Select(r => $"builder.AddProject(\"{r}\", \"../{r}.csproj\");"));
    }

    private static void NeedContainers(ProjectContext project)
    {
        File.AppendAllText(Path.Combine(project.AppHostDirectory, "Program.cs"), "var postgres = builder.AddPostgres(\"postgres\");\n");
    }

    private static void WriteServerWeb(ProjectContext project, string urls)
    {
        var web = Directory.CreateDirectory(Path.Combine(project.Directory, "src", "Server", $"{project.Name}.Server.Web")).FullName;
        var properties = Directory.CreateDirectory(Path.Combine(web, "Properties")).FullName;

        File.WriteAllText(Path.Combine(web, $"{project.Name}.Server.Web.csproj"), $"<Project><ItemGroup><ProjectReference Include=\"..\\{project.Name}.Server.Api\\{project.Name}.Server.Api.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(properties, "launchSettings.json"), $$"""
            {
                // the app's own
                "profiles": {
                    "IIS Express": { "commandName": "IISExpress" },
                    "Web": { "commandName": "Project", "applicationUrl": "{{urls}}" },
                }
            }
            """);
    }

    private static void WriteWorkbench(ProjectContext project, string url)
    {
        var properties = Directory.CreateDirectory(Path.Combine(project.WorkbenchDirectory, "Properties")).FullName;

        File.WriteAllText(Path.Combine(project.WorkbenchDirectory, $"{project.Name}.Workbench.csproj"), $"<Project Sdk=\"Microsoft.NET.Sdk.Web\"><ItemGroup><ProjectReference Include=\"..\\Client\\{project.Name}.Client.Core\\{project.Name}.Client.Core.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(properties, "launchSettings.json"), $$"""{ "profiles": { "{{project.Name}}.Workbench": { "commandName": "Project", "launchBrowser": true, "applicationUrl": "{{url}}" } } }""");
    }

    private static void WriteSolution(ProjectContext project)
    {
        var name = project.Name;
        string[] web = [$"src/Client/{name}.Client.Core/{name}.Client.Core.csproj", $"src/Client/{name}.Client.Web/{name}.Client.Web.csproj", $"src/Server/{name}.Server.Api/{name}.Server.Api.csproj", $"src/Server/{name}.Server.Web/{name}.Server.Web.csproj", $"src/Tests/{name}.Tests.csproj"];
        string[] native = [Platforms.MauiProject(name), Platforms.WindowsProject(name)];

        File.WriteAllText(project.Solution, $"""
            <Solution>
              <Folder Name="/Projects/">
            {string.Join(Environment.NewLine, web.Concat(native).Append(Platforms.WorkbenchProject(name)).Select(p => $"    <Project Path=\"{p}\">{Environment.NewLine}      <Build Solution=\"DebugBlazorServer|*\" Project=\"false\" />{Environment.NewLine}    </Project>"))}
              </Folder>
            </Solution>
            """);
        File.WriteAllText(project.WebSolutionFilter, System.Text.Json.JsonSerializer.Serialize(new { solution = new { path = Path.GetFileName(project.Solution), projects = web.Select(p => p.Replace('/', '\\')) } }));
    }

    private static List<string> DotnetCommands(TestHost host) => [.. host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments))];

    private static void WriteRecommendations(ProjectContext project, params string[] extensions)
    {
        var folder = Directory.CreateDirectory(Path.Combine(project.Directory, ".vscode")).FullName;
        var list = string.Join(", ", extensions.Select(e => $"\"{e}\""));
        File.WriteAllText(Path.Combine(folder, "extensions.json"), "{\n    // the project's picks\n    \"recommendations\": [ " + list + ", ],\n}\n");
    }

    private static ProjectContext CreateFakeProject(TestHost host, string name, TemplateSelection? template = null)
    {
        var directory = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, name)).FullName;
        File.WriteAllText(Path.Combine(directory, $"{name}.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, $"{name}.Web.slnf"), "{}");

        var core = Directory.CreateDirectory(Path.Combine(directory, "src", "Client", $"{name}.Client.Core")).FullName;
        File.WriteAllText(Path.Combine(core, $"{name}.Client.Core.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var api = Directory.CreateDirectory(Path.Combine(directory, "src", "Server", $"{name}.Server.Api")).FullName;
        File.WriteAllText(Path.Combine(api, $"{name}.Server.Api.csproj"), "<Project><ItemGroup><PackageReference Include=\"Microsoft.EntityFrameworkCore.Sqlite\" /><PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" /></ItemGroup></Project>");

        File.WriteAllText(Path.Combine(directory, "src", "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.12" />
              </ItemGroup>
            </Project>
            """);

        return new ProjectContext { Name = name, Directory = directory, Platforms = new HashSet<Platform> { Platform.Web }, Template = template };
    }

    private static Dictionary<string, string> RealPath() => new() { ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "" };

    private sealed class GitIdentity : IDisposable
    {
        private readonly Dictionary<string, string?> previous = [];

        public GitIdentity(string root)
        {
            var emptyConfig = Path.Combine(root, "empty.gitconfig");
            File.WriteAllText(emptyConfig, "");

            Set("GIT_AUTHOR_NAME", "bit tests");
            Set("GIT_AUTHOR_EMAIL", "tests@bitplatform.dev");
            Set("GIT_COMMITTER_NAME", "bit tests");
            Set("GIT_COMMITTER_EMAIL", "tests@bitplatform.dev");
            Set("GIT_CONFIG_GLOBAL", emptyConfig);
            Set("GIT_CONFIG_NOSYSTEM", "1");
        }

        public void Dispose()
        {
            foreach (var (key, value) in previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        private void Set(string key, string value)
        {
            previous[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static async Task<bool> GitAvailableAsync()
    {
        using var host = new TestHost(variables: RealPath());
        var result = await new ProcessRunner(host.Environment, CliLog.None).RunAsync(new ProcessSpec { FileName = "git", Arguments = ["--version"] });
        return result.Succeeded;
    }

    private static string TemplateDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && Directory.Exists(Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate", ".github", "workflows")) is false)
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory);
        return Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate");
    }
}
