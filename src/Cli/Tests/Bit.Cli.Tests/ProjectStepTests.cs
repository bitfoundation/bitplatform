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

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, CancellationToken.None);

        Assert.IsTrue(steps.Reports.All(r => r.Result.Status is StepStatus.Skipped));
        Assert.IsFalse(steps.AnyFailed);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task ANativeApp_Should_BuildTheWholeSolutionWithWhatItNeeds()
    {
        using var host = new TestHost(HostOs.Windows);
        var web = CreateFakeProject(host, "Contoso");
        var project = new ProjectContext { Name = web.Name, Directory = web.Directory, Platforms = new HashSet<Platform> { Platform.Web, Platform.Windows } };
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, CancellationToken.None);

        var commands = host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments)).ToList();
        CollectionAssert.Contains(commands, "workload install maui");
        Assert.IsTrue(commands.Any(c => c.StartsWith("build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj -t:InstallAndroidDependencies -f net10.0-android", StringComparison.Ordinal)), string.Join(Environment.NewLine, commands));
        CollectionAssert.Contains(commands, "restore Contoso.slnx");
        Assert.AreEqual(1, commands.Count(c => c.StartsWith("build ", StringComparison.Ordinal) && c.Contains("InstallAndroidDependencies", StringComparison.Ordinal) is false), string.Join(Environment.NewLine, commands));
        CollectionAssert.Contains(commands, "build Contoso.slnx");
        Assert.AreEqual("Built the solution", steps.Reports.Single(r => r.Id == "build").Result.Title);
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

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, CancellationToken.None);

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

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), false, false, false, false, CancellationToken.None);

        Assert.AreEqual(error, steps.Reports.Single(r => r.Id == "restore").Result.Hint);
        StringAssert.Contains(host.Output, "end-of-error");
    }

    [TestMethod]
    public void RecommendedExtensions_Should_BeReadDespiteCommentsAndRepeats()
    {
        using var host = new TestHost();
        var project = CreateFakeProject(host, "Contoso");
        WriteRecommendations(project, "ms-dotnettools.csdevkit", "Anthropic.claude-code", "ms-dotnettools.csdevkit");

        CollectionAssert.AreEqual(new[] { "ms-dotnettools.csdevkit", "Anthropic.claude-code" }, ProjectSteps.ReadRecommendedExtensions(project.Directory).ToArray());
    }

    [TestMethod]
    public async Task ExtensionsVsCodeBuildsIn_Should_CountAsInstalled()
    {
        using var host = new TestHost(HostOs.Linux);
        var code = typeof(ProjectStepTests).Assembly.Location;
        var name = Path.GetFileNameWithoutExtension(code);
        host.Runner.Executables["code"] = code;
        var project = CreateFakeProject(host, "Contoso");
        WriteRecommendations(project, "GitHub.copilot", "GitHub.copilot-chat");
        host.Runner.On(name, "--list-extensions", 0, "ms-dotnettools.csdevkit\n");
        host.Runner.On(name, "--install-extension GitHub.copilot", 0);
        host.Runner.On(name, "--install-extension GitHub.copilot --install-extension GitHub.copilot-chat", 1,
            "Error while installing extension github.copilot-chat: Extension 'github.copilot-chat' is a built-in extension with version '0.68.0' and cannot be downgraded to version '0.48.1'.\nFailed Installing Extensions: github.copilot-chat");

        var result = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail + result.FollowUp);
        Assert.AreEqual("Installed 1 VS Code extension", result.Title);
        CollectionAssert.AreEqual(new[] { "--install-extension", "GitHub.copilot" }, host.Runner.Calls.Last().Arguments.ToArray());
    }

    [TestMethod]
    public async Task VsCode_Should_GetOnlyTheExtensionsItLacks()
    {
        using var host = new TestHost(HostOs.Linux);
        var code = typeof(ProjectStepTests).Assembly.Location;
        host.Runner.Executables["code"] = code;
        var project = CreateFakeProject(host, "Contoso");
        WriteRecommendations(project, "ms-dotnettools.csdevkit", "Anthropic.claude-code", "GitHub.copilot");
        host.Runner.On(Path.GetFileNameWithoutExtension(code), "--list-extensions", 0, "github.copilot\nms-dotnettools.csdevkit\n");

        var result = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("Installed 1 VS Code extension", result.Title);
        var install = host.Runner.Calls.Single(c => c.Arguments.FirstOrDefault() is "--install-extension");
        CollectionAssert.AreEqual(new[] { "--install-extension", "Anthropic.claude-code" }, install.Arguments.ToArray());

        host.Runner.On(Path.GetFileNameWithoutExtension(code), "--list-extensions", 0, "anthropic.claude-code\ngithub.copilot\nms-dotnettools.csdevkit\n");
        var done = await new ProjectSteps(host.Services, project).VsCodeExtensionsAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual("Extensions already installed", done.Title);
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.FirstOrDefault() is "--install-extension"));
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
    public async Task BuildProperties_Should_ReachTheRestoreAndTheBuildButNotTheBrowsers()
    {
        using var host = new TestHost(HostOs.Linux);
        var web = CreateFakeProject(host, "Contoso");
        var project = new ProjectContext { Name = web.Name, Directory = web.Directory, Platforms = web.Platforms, BuildProperties = ["EnforceCodeStyleInBuild=true", "Environment=Staging"] };
        var steps = new StepRunner(host.Services);

        await NewWorkflow.RunSetupStepsAsync(host.Services, steps, project, new ProjectSteps(host.Services, project), true, false, false, true, CancellationToken.None);

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
        var push = calls.IndexOf("git push -u origin develop main");
        var defaultBranch = calls.IndexOf("gh repo edit --default-branch develop");

        Assert.IsTrue(create >= 0 && create < push && push < defaultBranch, string.Join(Environment.NewLine, calls));
        Assert.IsTrue(host.Runner.Calls.All(c => c.WorkingDirectory == project.Directory));
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
}
