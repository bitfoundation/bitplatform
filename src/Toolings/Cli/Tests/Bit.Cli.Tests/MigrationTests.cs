using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class MigrationTests
{
    private static readonly string[] diagnostics = ["--verbose", "--no-color", "--prefix-output"];

    [TestMethod]
    public async Task Add_Should_RunTheProjectsDotnetEfInServerApiFromAnyFolderOfTheProject()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        var inside = Directory.CreateDirectory(Path.Combine(project, "src", "Shared", "Features")).FullName;

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, inside, "migration", "add", "AddProductRating"), host.Output);

        var call = host.Runner.Calls.Single();
        Assert.AreEqual("dotnet", call.FileName);
        Assert.AreEqual(ServerApi(project), call.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("migrations", "add", "AddProductRating", "--output-dir", "Infrastructure/Data/Migrations"), call.Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, "Added the AddProductRating migration");
    }

    [TestMethod]
    public async Task AddOffline_Should_AddItToClientCoreWithServerWebAsStartupAndThenRegenerateTheCompiledModel()
    {
        using var host = new TestHost();
        var project = CreateProject(host, offline: true);

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "AddTodoTags", "--offline"), host.Output);

        Assert.HasCount(2, host.Runner.Calls);
        var (add, optimize) = (host.Runner.Calls[0], host.Runner.Calls[1]);

        Assert.AreEqual(ServerWeb(project), add.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("migrations", "add", "AddTodoTags", "--output-dir", "Infrastructure/Data/Migrations", "--context", "AppOfflineDbContext", "--project", ClientCoreProject), add.Arguments.ToArray());
        Assert.AreEqual(Path.Combine(project, "src", "Client", "Contoso.Client.Core", "Contoso.Client.Core.csproj"), Path.GetFullPath(Path.Combine(add.WorkingDirectory!, ClientCoreProject)));

        Assert.AreEqual(ServerWeb(project), optimize.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("dbcontext", "optimize", "--output-dir", "Infrastructure/Data/CompiledModel", "--namespace", "Contoso.Client.Core.Infrastructure.Data", "--context", "AppOfflineDbContext", "--project", ClientCoreProject, "--no-build"), optimize.Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, "Regenerated the compiled model");
    }

    [TestMethod]
    public async Task Remove_Should_RegenerateTheCompiledModelOnlyForTheOfflineDatabase()
    {
        using var host = new TestHost();
        var project = CreateProject(host, offline: true);
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations remove", 0, "info:    Build succeeded.\ninfo:    Removing migration '20261028090000_AddTodoTags'.\ninfo:    Done.");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "remove", "--offline"), host.Output);

        Assert.HasCount(2, host.Runner.Calls);
        Assert.AreEqual(ServerWeb(project), host.Runner.Calls[0].WorkingDirectory);
        CollectionAssert.AreEqual(Ef("migrations", "remove", "--context", "AppOfflineDbContext", "--project", ClientCoreProject), host.Runner.Calls[0].Arguments.ToArray());
        CollectionAssert.AreEqual(Ef("dbcontext", "optimize", "--output-dir", "Infrastructure/Data/CompiledModel", "--namespace", "Contoso.Client.Core.Infrastructure.Data", "--context", "AppOfflineDbContext", "--project", ClientCoreProject, "--no-build"), host.Runner.Calls[1].Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, "Removed the last migration 20261028090000_AddTodoTags");

        host.Runner.Calls.Clear();

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "remove"), host.Output);

        var server = host.Runner.Calls.Single();
        Assert.AreEqual(ServerApi(project), server.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("migrations", "remove"), server.Arguments.ToArray());
    }

    [TestMethod]
    public async Task Optimize_Should_BuildBeforeItRegeneratesTheCompiledModel()
    {
        using var host = new TestHost();
        var project = CreateProject(host, offline: true);

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "optimize"), host.Output);

        var call = host.Runner.Calls.Single();
        Assert.AreEqual(ServerWeb(project), call.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("dbcontext", "optimize", "--output-dir", "Infrastructure/Data/CompiledModel", "--namespace", "Contoso.Client.Core.Infrastructure.Data", "--context", "AppOfflineDbContext", "--project", ClientCoreProject), call.Arguments.ToArray());
    }

    [TestMethod]
    public async Task Bundle_Should_PassTheRuntimeThrough()
    {
        using var host = new TestHost();
        var project = CreateProject(host);

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "bundle", "--runtime", "win-x64"), host.Output);

        var call = host.Runner.Calls.Single();
        Assert.AreEqual(ServerApi(project), call.WorkingDirectory);
        CollectionAssert.AreEqual(Ef("migrations", "bundle", "--configuration", "Release", "--self-contained", "--target-runtime", "win-x64", "--output", "efbundle.exe", "--force"), call.Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, $"Copy {Path.Combine("src", "Server", "Contoso.Server.Api", "efbundle.exe")} into the deployed app's folder");

        host.Runner.Calls.Clear();

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "bundle"), host.Output);
        CollectionAssert.IsSubsetOf(new[] { "linux-x64", "efbundle" }, host.Runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task AFailedAdd_Should_ShowTheBuildErrorsAndTheCommandToRetryAndKeepTheCompiledModel()
    {
        using var host = new TestHost();
        var project = CreateProject(host, offline: true);
        var broken = Path.Combine(project, "src", "Client", "Contoso.Client.Core", "Broken.cs");
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations add", 1, $"""
            info:    Build started...
            verbose: {broken}(1,35): error CS1002: ; expected [{Path.Combine(project, "src", "Client", "Contoso.Client.Core", "Contoso.Client.Core.csproj")}]
            verbose:     1 Error(s)
            verbose: {broken}(1,35): error CS1002: ; expected [{Path.Combine(project, "src", "Client", "Contoso.Client.Core", "Contoso.Client.Core.csproj")}]
            error:   Build failed. Use dotnet build to see the errors.
            """);

        Assert.AreEqual(CliApp.ExitFailed, await RunInAsync(host, project, "migration", "add", "AddTodoTags", "--offline"));

        Assert.HasCount(1, host.Runner.Calls);
        StringAssert.Contains(host.FlatOutput, "Couldn't add the AddTodoTags migration");
        StringAssert.Contains(host.FlatOutput, $"{Path.Combine("src", "Client", "Contoso.Client.Core", "Broken.cs")}(1,35): error CS1002: ; expected");
        StringAssert.Contains(host.FlatOutput, "Build failed. Use dotnet build to see the errors.");
        StringAssert.Contains(host.FlatOutput, "Didn't regenerate the compiled model");
        StringAssert.Contains(host.FlatOutput, $"dotnet dnx dotnet-ef@10.0.14 -- migrations add AddTodoTags --output-dir Infrastructure/Data/Migrations --context AppOfflineDbContext --project {ClientCoreProject} --verbose");
        StringAssert.Contains(host.Output, $"bit was running EF Core's command-line tool, dotnet-ef 10.0.14, in {ServerWeb(project)}: dotnet dnx dotnet-ef@10.0.14 -- migrations add AddTodoTags --output-dir Infrastructure/Data/Migrations --context AppOfflineDbContext --project {ClientCoreProject}{Environment.NewLine}");
        Assert.IsLessThan(host.Output.IndexOf("Build failed.", StringComparison.Ordinal), host.Output.IndexOf("bit was running EF Core's", StringComparison.Ordinal));
        Assert.IsFalse(host.Output.Contains("--prefix-output", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task AnAddedMigration_Should_BeFormattedOnItsOwn()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        var migrations = Directory.CreateDirectory(Path.Combine(ServerApi(project), "Infrastructure", "Data", "Migrations")).FullName;
        File.WriteAllText(Path.Combine(migrations, "20260101000000_Initial.cs"), "");
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations add", spec =>
        {
            File.WriteAllText(Path.Combine(migrations, "20261029000000_AddProductRating.cs"), "");
            File.WriteAllText(Path.Combine(migrations, "20261029000000_AddProductRating.Designer.cs"), "");
            return new ProcessResult { ExitCode = 0 };
        });

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "AddProductRating"), host.Output);

        var format = host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "format");
        Assert.AreEqual(ServerApi(project), format.WorkingDirectory);
        CollectionAssert.AreEqual(new[] { "format", "Contoso.Server.Api.csproj", "--no-restore", "--include", "Infrastructure/Data/Migrations/20261029000000_AddProductRating.Designer.cs", "Infrastructure/Data/Migrations/20261029000000_AddProductRating.cs" }, format.Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, "Formatted the migration");
    }

    [TestMethod]
    public async Task AMigrationThatCouldNotBeFormatted_Should_StayAndSayHowToFormatIt()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        var migrations = Path.Combine(ServerApi(project), "Infrastructure", "Data", "Migrations");
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations add", spec =>
        {
            Directory.CreateDirectory(migrations);
            File.WriteAllText(Path.Combine(migrations, "20261029000000_AddProductRating.cs"), "");
            return new ProcessResult { ExitCode = 0 };
        });
        host.Runner.On("dotnet", "format", 1, "Could not find a MSBuild project file");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "AddProductRating"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Couldn't format the migration");
        StringAssert.Contains(host.Output, $"bit ran this in {ServerApi(project)}: dotnet format Contoso.Server.Api.csproj --no-restore --include Infrastructure/Data/Migrations/20261029000000_AddProductRating.cs");
        StringAssert.Contains(host.FlatOutput, "To try again");
        Assert.IsTrue(File.Exists(Path.Combine(migrations, "20261029000000_AddProductRating.cs")));
    }

    [TestMethod]
    public async Task AFailedAdd_Should_ShowDotnetEfsOwnErrorRatherThanTheBuildSummary()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations add", 1, """
            info:    Build started...
            verbose:     0 Warning(s)
            verbose:     0 Error(s)
            info:    Build succeeded.
            verbose: Microsoft.EntityFrameworkCore.Design.OperationException: The name 'Initial' is used by an existing migration.
            verbose:    at Microsoft.EntityFrameworkCore.Migrations.Design.MigrationsScaffolder.ScaffoldMigration(String migrationName)
            error:   The name 'Initial' is used by an existing migration.
            """);

        Assert.AreEqual(CliApp.ExitFailed, await RunInAsync(host, project, "migration", "add", "Initial"));

        StringAssert.Contains(host.FlatOutput, "The name 'Initial' is used by an existing migration.");
        Assert.IsFalse(host.Output.Contains("0 Error(s)", StringComparison.Ordinal), host.Output);
        StringAssert.Contains(host.FlatOutput, "dotnet dnx dotnet-ef@10.0.14 -- migrations add Initial --output-dir Infrastructure/Data/Migrations --verbose");
    }

    [TestMethod]
    public async Task ADataLossWarning_Should_BeShownWithoutTheLogsOfTheApp()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        host.Runner.On("dotnet", "dnx dotnet-ef@10.0.14 -- migrations add", 0, """
            warn: Microsoft.EntityFrameworkCore.Model.Validation[30000]
                  The entity type 'HangfireLock' is configured to use schema 'jobs', but SQLite does not support schemas.
            warn:    Sensitive data logging is enabled. Log entries and exception messages may include sensitive application data.
            warn:    An operation was scaffolded that may result in the loss of data. Please review the migration for accuracy.
            info:    Done. To undo this action, use 'ef migrations remove'
            """);

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "DropProductColor"));

        StringAssert.Contains(host.FlatOutput, "may result in the loss of data. Please review the migration for accuracy.");
        Assert.IsFalse(host.Output.Contains("HangfireLock", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.Output.Contains("Sensitive data logging", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task AProjectCreatedWithDatabaseOther_Should_BeToldItHasNoProviderYet()
    {
        using var host = new TestHost();
        var project = CreateProject(host, offline: true);
        File.Copy(TestHost.RepositoryFile("src", "Templates", "Boilerplate", "Bit.Boilerplate", "src", "Server", "Boilerplate.Server.Api", "Program.Services.cs"), Path.Combine(ServerApi(project), "Program.Services.cs"), overwrite: true);

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, project, "migration", "add", "Initial"));
        StringAssert.Contains(host.FlatOutput, "--database Other, so its database has no provider yet");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, project, "migration", "bundle"));
        Assert.IsEmpty(host.Runner.Calls);

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "AddTodoTags", "--offline"));
    }

    [TestMethod]
    public async Task AProviderBitDoesntKnow_Should_StillGetItsMigrations()
    {
        using var host = new TestHost();
        var project = CreateProject(host);
        File.WriteAllText(Path.Combine(ServerApi(project), "Contoso.Server.Api.csproj"), "<Project><ItemGroup><PackageReference Include=\"Oracle.EntityFrameworkCore\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(ServerApi(project), "Program.Services.cs"), "options.UseOracle(connectionString);");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "migration", "add", "Initial"), host.Output);
        Assert.IsTrue(host.Runner.Calls.Any(c => c.Arguments.Contains("migrations") && c.Arguments.Contains("Initial")));
    }

    [TestMethod]
    public async Task AProjectWithoutAnOfflineDatabase_Should_BeToldSo()
    {
        using var host = new TestHost();
        var project = CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, project, "migration", "add", "AddTodoTags", "--offline"));
        StringAssert.Contains(host.FlatOutput, "has no offline database");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, project, "migration", "optimize"));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task AFolderOutsideAProject_Should_BeRefused()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.WorkingDirectory, "migration", "add", "Initial"));
        StringAssert.Contains(host.FlatOutput, "isn't in a bit Boilerplate project");
        Assert.IsEmpty(host.Runner.Calls);
    }

    private static string[] Ef(params string[] arguments) => ["dnx", "dotnet-ef@10.0.14", "--", .. arguments, .. diagnostics];

    private const string ClientCoreProject = "../../Client/Contoso.Client.Core/Contoso.Client.Core.csproj";

    private static string ServerApi(string project) => Path.Combine(project, "src", "Server", "Contoso.Server.Api");

    private static string ServerWeb(string project) => Path.Combine(project, "src", "Server", "Contoso.Server.Web");

    private static string CreateProject(TestHost host, bool offline = false)
    {
        var directory = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, "Contoso.Web.slnf"), "{}");

        var api = Directory.CreateDirectory(ServerApi(directory)).FullName;
        File.WriteAllText(Path.Combine(api, "Contoso.Server.Api.csproj"), "<Project><ItemGroup><PackageReference Include=\"Microsoft.EntityFrameworkCore.Sqlite\" /><PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(api, "Program.Services.cs"), "options.UseNpgsql(dataSource);");
        Directory.CreateDirectory(ServerWeb(directory));

        var data = Directory.CreateDirectory(Path.Combine(directory, "src", "Client", "Contoso.Client.Core", "Infrastructure", "Data")).FullName;

        if (offline)
        {
            File.WriteAllText(Path.Combine(data, "AppOfflineDbContext.cs"), "");
        }

        File.WriteAllText(Path.Combine(directory, "src", "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.13" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.14" />
              </ItemGroup>
            </Project>
            """);

        return directory;
    }

    private static Task<int> RunInAsync(TestHost host, string directory, params string[] args)
    {
        var environment = new CliEnvironment
        {
            Os = host.Environment.Os,
            Architecture = host.Environment.Architecture,
            HomeDirectory = host.Home,
            CurrentDirectory = directory,
            Variables = host.Environment.Variables,
            IsInputRedirected = true,
            IsOutputRedirected = true
        };

        var services = new CliServices(environment, host.Console, runner: host.Runner);

        return CliApp.BuildRootCommand(() => services)
            .Parse(args)
            .InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false, Output = new StringWriter(), Error = new StringWriter() });
    }
}
