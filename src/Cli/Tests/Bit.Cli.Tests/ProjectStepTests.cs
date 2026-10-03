using System.Security.Cryptography.X509Certificates;
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
        var project = new ProjectContext { Name = "Missing", Directory = Path.Combine(host.WorkingDirectory, "Missing"), Platforms = new HashSet<Platform> { Platform.Web } };
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, CancellationToken.None);

        Assert.IsTrue(steps.Reports.All(r => r.Result.Status is StepStatus.Skipped));
        Assert.IsFalse(steps.AnyFailed);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task AFailedStep_Should_NotStopTheNextOnes()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        host.Runner.On("dotnet", "restore", 1, "x.csproj : error NU1301: Unable to load the service index for source https://nuget.contoso.local/v3/index.json. [x.csproj]");
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, CancellationToken.None);

        var byId = steps.Reports.ToDictionary(r => r.Id, r => r.Result);
        Assert.AreEqual(StepStatus.Failed, byId["restore"].Status);
        CollectionAssert.AreEqual(new[] { "NU1301" }, byId["restore"].Codes.ToArray());
        StringAssert.StartsWith(byId["restore"].Hint, "error NU1301");
        Assert.AreEqual(StepStatus.Succeeded, byId["build-web"].Status);
        StringAssert.Contains(byId["restore"].FollowUp, "dotnet restore Contoso.Web.slnf");
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
}
