using System.IO.Compression;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class NewWorkflowTests
{
    [TestMethod]
    public async Task NoSetup_Should_CreateTheProjectAndLeaveTheRestToBitSetup()
    {
        using var host = new TestHost(HostOs.Linux);
        var package = FakeTemplatePackage(host);
        GeneratesProjects(host);

        var exitCode = await host.RunAsync("new", "Contoso", "--template-package", package, "--yes", "--no-setup", "--no-git", "--no-trust");

        Assert.AreEqual(0, exitCode, host.Output);
        Assert.IsTrue(File.Exists(Path.Combine(host.WorkingDirectory, "Contoso", "src", "Server", "Contoso.Server.Api", "AppCertificate.key")));
        Assert.DoesNotContain(c => c.Arguments.FirstOrDefault() is "workload" or "restore" or "build" or "dnx", host.Runner.Calls, Calls(host));
        Assert.DoesNotContain(c => c.FileName is "git" or "node" or "docker" or "aspire", host.Runner.Calls, Calls(host));
        Assert.Contains("bit setup", host.Output);
        Assert.Contains("bit setup adds it", host.Output);
        Assert.IsTrue(File.Exists(Path.Combine(host.WorkingDirectory, "Contoso", "Contoso.slnx")));
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "Contoso", "Contoso.sln")));
    }

    [TestMethod]
    public async Task Setup_Should_AddTheInitialMigrationThatNoSetupLeftOut()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        GeneratesProjects(host);
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        var project = Path.Combine(host.WorkingDirectory, "Contoso");

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--template-package", FakeTemplatePackage(host), "--yes", "--no-setup", "--no-git", "--no-trust"), host.Output);
        Assert.AreEqual(0, await host.RunAsync("setup", project, "--yes", "--no-run"), host.Output);

        var dotnetCalls = host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments)).ToList();
        var restore = dotnetCalls.FindIndex(call => call.StartsWith("restore", StringComparison.Ordinal));
        var migration = dotnetCalls.FindIndex(call => call.Contains("migrations add Initial", StringComparison.Ordinal));

        Assert.IsTrue(restore >= 0 && migration > restore, string.Join(Environment.NewLine, dotnetCalls));

        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(project, "src", "Server", "Contoso.Server.Api", "Infrastructure", "Data", "Migrations")).FullName, "Initial.cs"), "");

        Assert.AreEqual(0, await host.RunAsync("setup", project, "--yes", "--no-run"), host.Output);
        Assert.ContainsSingle(c => c.Arguments.Contains("Initial"), host.Runner.Calls, Calls(host));
    }

    [TestMethod]
    public async Task EveryStep_Should_RunInOrderEvenWhenOneFails()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        var package = FakeTemplatePackage(host);
        GeneratesProjects(host);
        host.Runner.On("dotnet", "workload list", 0, "Installed Workload Id\n---\nwasm-tools   10.0.12/10.0.100   SDK 10.0.400\n");
        host.Runner.On("dotnet", "restore", 1, "Contoso.Web.slnf : error NU1301: Unable to load the service index for source https://nuget.contoso.local/v3/index.json.");

        var exitCode = await host.RunAsync("new", "Contoso", "--template-package", package, "--yes", "--no-git", "--no-trust");

        Assert.AreEqual(CliApp.ExitFailed, exitCode, host.Output);

        var order = new[] { "new install", "new bit-bp", "workload list", "restore", "build Contoso.Web.slnf", "dnx dotnet-ef", "format Contoso" };
        var dotnetCalls = host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments)).ToList();
        var positions = order.Select(step => dotnetCalls.FindIndex(call => call.Contains(step, StringComparison.Ordinal))).ToArray();

        Assert.DoesNotContain(-1, positions, string.Join(Environment.NewLine, dotnetCalls));
        Assert.AreSequenceEqual(positions.Order().ToArray(), positions, string.Join(Environment.NewLine, dotnetCalls));
        Assert.Contains("NU1301", host.Output);
        Assert.Contains("dotnet restore Contoso.Web.slnf", host.Output);
        Assert.Contains("Contoso is ready", host.Output);
    }

    [TestMethod]
    public async Task WithoutARun_TheWorkbenchTheLandingPageAndTheWebApp_Should_StillBeBuiltBeforeTheNativeApps()
    {
        using var host = new TestHost(HostOs.Linux);
        var package = FakeTemplatePackage(host);
        GeneratesProjects(host);

        var exitCode = await host.RunAsync("new", "Contoso", "--template-package", package, "--yes", "--no-git", "--no-trust", "--no-tools", "--no-run", "--platforms", "android");

        Assert.AreEqual(0, exitCode, host.Output);
        AssertInOrder(host, [
            "build src/Workbench/Contoso.Workbench.csproj",
            "build src/LandingPage/Contoso.LandingPage.Server/Contoso.LandingPage.Server.csproj",
            "workload install wasm-tools",
            "restore Contoso.Web.slnf",
            "build Contoso.Web.slnf",
            "dnx dotnet-ef",
            "format Contoso.slnx",
            "workload install maui-android",
            "build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj -t:InstallAndroidDependencies",
            "restore Contoso.slnx",
            "build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj",
            "build src/Client/Contoso.Client.Windows/Contoso.Client.Windows.csproj"
        ]);
        Assert.DoesNotContain(c => c.Arguments.FirstOrDefault() is "run" || c.FileName is "aspire", host.Runner.Calls, Calls(host));
        Assert.DoesNotContain(c => string.Join(' ', c.Arguments) is "build Contoso.slnx", host.Runner.Calls, "The whole solution isn't built again: " + Calls(host));
        Assert.IsEmpty(host.Runner.Detached);
        Assert.Contains("build the Workbench, build the landing page, WebAssembly build support, restore and build the web app, Chromium for UI tests, initial migration, dotnet format, Android build support, restore and build the native apps", host.FlatOutput);
    }

    [TestMethod]
    public async Task Setup_Should_StartTheProjectBeforeItSetsUpTheNativeApps()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        var directory = Path.Combine(host.WorkingDirectory, "Contoso");
        WriteProject(directory, "Contoso");
        var appHost = Directory.CreateDirectory(Path.Combine(directory, "src", "Server", "Contoso.Server.AppHost")).FullName;
        File.WriteAllText(Path.Combine(appHost, "Program.cs"), "builder.AddProject(\"serverweb\", \"../Contoso.Server.Web.csproj\");");

        Assert.AreEqual(0, await host.RunAsync("setup", directory, "--yes", "--no-tools", "--no-browsers", "--platforms", "android"), host.Output);

        AssertInOrder(host, [
            "build src/Workbench/Contoso.Workbench.csproj",
            "build src/LandingPage/Contoso.LandingPage.Server/Contoso.LandingPage.Server.csproj",
            "workload install wasm-tools",
            "restore Contoso.Web.slnf",
            "build Contoso.Web.slnf",
            "start --apphost",
            "stop --apphost",
            "workload install maui-android",
            "restore Contoso.slnx",
            "build src/Client/Contoso.Client.Maui/Contoso.Client.Maui.csproj",
            "build src/Client/Contoso.Client.Windows/Contoso.Client.Windows.csproj"
        ]);
        Assert.DoesNotContain(c => c.Arguments.FirstOrDefault() is "run", host.Runner.Calls, "Without a terminal the Workbench and the landing page are only built: " + Calls(host));
        Assert.Contains("Started and stopped the project with Aspire", host.FlatOutput);
        Assert.Contains("Contoso is ready", host.FlatOutput);
    }

    [TestMethod]
    public async Task ThePlan_Should_OpenTheWorkbenchThenTheLandingPageAndSetUpTheNativeAppsLast()
    {
        using var host = new TestHost(HostOs.Linux, prompter: new ScriptedPrompter());
        host.AllToolsInstalled();

        Assert.AreEqual(0, await host.RunAsync("new", "Contoso", "--template-package", FakeTemplatePackage(host), "--platforms", "android", "--dry-run", "--yes"), host.Output);

        Assert.Contains(".NET SDK check, container runtime for Aspire, build and open the Workbench, build and open the landing page, WebAssembly build support, restore and build the web app, Chromium for UI tests, initial migration, trust for VS Code and AI tools, VS Code extensions, start it with Aspire and open the dashboard and the app, dotnet format, Android build support, restore and build the native apps, open in VS Code", host.FlatOutput);
    }

    [TestMethod]
    public async Task AMissingTemplate_Should_SkipTheProjectStepsInsteadOfFailingThem()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.On("dotnet", "new install", 1, "error: Bit.Boilerplate::0.0.0 could not be installed, the package does not exist.");

        var exitCode = await host.RunAsync("new", "Contoso", "--template-package", FakeTemplatePackage(host), "--yes", "--no-setup", "--no-git", "--no-trust");

        Assert.AreEqual(CliApp.ExitFailed, exitCode, host.Output);
        Assert.Contains("Contoso wasn't created", host.Output);
        Assert.DoesNotContain(c => c.Arguments.FirstOrDefault() is "format" or "dnx", host.Runner.Calls, Calls(host));
    }

    [TestMethod]
    public async Task ALocalTemplatePackage_Should_BeInstalledOnceUntilItChanges()
    {
        using var host = new TestHost(HostOs.Linux);
        var package = FakeTemplatePackage(host);
        host.Runner.On("dotnet", "new install", spec =>
        {
            var arguments = spec.Arguments.ToList();
            var packages = Directory.CreateDirectory(Path.Combine(arguments[arguments.IndexOf("--debug:custom-hive") + 1], "packages")).FullName;
            File.Copy(arguments[2], Path.Combine(packages, Path.GetFileName(arguments[2])), overwrite: true);
            return new ProcessResult { ExitCode = 0 };
        });

        var source = new TemplateSource(host.Environment, host.Runner);
        var first = await source.EnsureInstalledAsync(null, package, null, CancellationToken.None);
        var again = await source.EnsureInstalledAsync(null, package, null, CancellationToken.None);
        File.SetLastWriteTimeUtc(package, File.GetLastWriteTimeUtc(package).AddMinutes(1));
        var changed = await source.EnsureInstalledAsync(null, package, null, CancellationToken.None);

        Assert.IsNotNull(first.Package);
        var expectedValue = first.Package.HiveDirectory;
        var actualValue = again.Package?.HiveDirectory;
        Assert.AreEqual(expectedValue, actualValue);
        var expectedValue1 = first.Package.HiveDirectory;
        var actualValue2 = changed.Package?.HiveDirectory;
        Assert.AreNotEqual(expectedValue1, actualValue2);
        Assert.AreEqual(2, host.Runner.Calls.Count(c => c.Arguments.Take(2).SequenceEqual(["new", "install"])), Calls(host));
    }

    [TestMethod]
    public async Task AnInterruptedTemplateDownload_Should_BeDownloadedAgain()
    {
        using var host = new TestHost(HostOs.Linux);
        var complete = FakeTemplatePackage(host);
        var packages = Directory.CreateDirectory(Path.Combine(host.Environment.BitDirectory, "templates", "10.7.0", "packages")).FullName;
        File.WriteAllBytes(Path.Combine(packages, "Bit.Boilerplate.10.7.0.nupkg"), [0x50, 0x4B, 0x03, 0x04]);
        host.Runner.On("dotnet", "new install", spec =>
        {
            var arguments = spec.Arguments.ToList();
            var hivePackages = Directory.CreateDirectory(Path.Combine(arguments[arguments.IndexOf("--debug:custom-hive") + 1], "packages")).FullName;
            File.Copy(complete, Path.Combine(hivePackages, "Bit.Boilerplate.10.7.0.nupkg"));
            return new ProcessResult { ExitCode = 0 };
        });

        var source = new TemplateSource(host.Environment, host.Runner);
        var (package, _) = await source.EnsureInstalledAsync("10.7.0", null, null, CancellationToken.None);
        var (again, _) = await source.EnsureInstalledAsync("10.7.0", null, null, CancellationToken.None);

        Assert.IsNotNull(TemplateSource.ReadManifest(package!.PackagePath), "A half-written package was taken as installed.");
        var expectedValue = package.PackagePath;
        var actualValue = again?.PackagePath;
        Assert.AreEqual(expectedValue, actualValue);
        Assert.ContainsSingle(c => c.Arguments.Take(2).SequenceEqual(["new", "install"]), host.Runner.Calls, Calls(host));
    }

    [TestMethod]
    public async Task ATemplateVersionThatIsntOne_Should_NeverBecomeAFolderPath()
    {
        using var host = new TestHost(HostOs.Linux);
        var outside = Directory.CreateDirectory(Path.Combine(host.Root, "outside")).FullName;
        File.WriteAllText(Path.Combine(outside, "keep.txt"), "mine");
        var version = Path.GetRelativePath(Path.Combine(host.Environment.BitDirectory, "templates"), outside);

        var (package, result) = await new TemplateSource(host.Environment, host.Runner).EnsureInstalledAsync(version, null, null, CancellationToken.None);

        Assert.IsNull(package);
        Assert.Contains("isn't a bit Boilerplate version", result.Output);
        Assert.IsTrue(File.Exists(Path.Combine(outside, "keep.txt")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task ATemplateAnOlderBitInstalled_Should_BeUsedWithoutDownloadingItAgain()
    {
        using var host = new TestHost(HostOs.Linux);
        var hive = Path.Combine(host.Environment.BitDirectory, "templates", "10.7.0");
        var installed = Path.Combine(Directory.CreateDirectory(Path.Combine(hive, "packages")).FullName, "Bit.Boilerplate.10.7.0.nupkg");
        File.Copy(FakeTemplatePackage(host), installed);
        File.WriteAllText(Path.Combine(hive, "packages.json"), $$"""{"Packages":[{"MountPointUri":{{System.Text.Json.JsonSerializer.Serialize(installed)}}}]}""");

        var (package, _) = await new TemplateSource(host.Environment, host.Runner).EnsureInstalledAsync("10.7.0", null, null, CancellationToken.None);
        var expectedValue = installed;
        var actualValue = package?.PackagePath;

        Assert.AreEqual(expectedValue, actualValue);
        Assert.IsEmpty(host.Runner.Calls, Calls(host));
    }

    [TestMethod]
    public async Task TwoBitsInstallingTheSameTemplate_Should_DownloadItOnce()
    {
        using var host = new TestHost(HostOs.Linux);
        var complete = FakeTemplatePackage(host);
        host.Runner.OnAsync("dotnet", "new install", async (spec, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            var arguments = spec.Arguments.ToList();
            var hivePackages = Directory.CreateDirectory(Path.Combine(arguments[arguments.IndexOf("--debug:custom-hive") + 1], "packages")).FullName;
            File.Copy(complete, Path.Combine(hivePackages, "Bit.Boilerplate.10.7.0.nupkg"));
            return new ProcessResult { ExitCode = 0 };
        });

        var results = await Task.WhenAll(
            Task.Run(() => new TemplateSource(host.Environment, host.Runner).EnsureInstalledAsync("10.7.0", null, null, CancellationToken.None)),
            Task.Run(() => new TemplateSource(host.Environment, host.Runner).EnsureInstalledAsync("10.7.0", null, null, CancellationToken.None)));

        Assert.IsTrue(results.All(r => r.Package is not null), Calls(host));
        Assert.ContainsSingle(c => c.Arguments.Take(2).SequenceEqual(["new", "install"]), host.Runner.Calls, Calls(host));
    }

    [TestMethod]
    public async Task ATemplateFolder_Should_BeInstalledFromTheFolderOnce()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = Directory.CreateDirectory(Path.Combine(host.Root, "src", "Templates", "Boilerplate", "Bit.Boilerplate")).FullName;
        var config = Directory.CreateDirectory(Path.Combine(folder, ".template.config")).FullName;
        using (var resource = typeof(CliApp).Assembly.GetManifestResourceStream("Bit.Cli.template.json")!)
        using (var templateJson = File.Create(Path.Combine(config, "template.json")))
        {
            resource.CopyTo(templateJson);
        }

        File.WriteAllText(Path.Combine(folder, "global.json"), "{ \"sdk\": { \"version\": \"10.0.300\" } }");

        var source = new TemplateSource(host.Environment, host.Runner);
        var first = await source.EnsureInstalledAsync(null, folder, null, CancellationToken.None);
        var again = await source.EnsureInstalledAsync(null, folder, null, CancellationToken.None);
        var expectedValue = TemplateSource.FolderVersion;
        var actualValue = first.Package?.Version;

        Assert.AreEqual(expectedValue, actualValue);
        var expectedValue1 = first.Package!.HiveDirectory;
        var actualValue2 = again.Package?.HiveDirectory;
        Assert.AreEqual(expectedValue1, actualValue2);
        var install = host.Runner.Calls.Single(c => c.Arguments.Take(2).SequenceEqual(["new", "install"]));
        Assert.AreEqual(folder, install.Arguments[2]);
        Assert.IsNotNull(TemplateSource.ReadManifest(folder));
        Assert.Contains("10.0.300", TemplateSource.ReadEntry(folder, "Bit.Boilerplate/global.json")!);
    }

    private static string FakeTemplatePackage(TestHost host)
    {
        var path = Path.Combine(host.Root, "Bit.Boilerplate.0.0.0.nupkg");

        using var resource = typeof(CliApp).Assembly.GetManifestResourceStream("Bit.Cli.template.json")!;
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        using (var nuspec = new StreamWriter(archive.CreateEntry("Bit.Boilerplate.nuspec").Open()))
        {
            nuspec.Write("<package><metadata><id>Bit.Boilerplate</id><version>0.0.0</version></metadata></package>");
        }

        using (var templateJson = archive.CreateEntry("content/Bit.Boilerplate/.template.config/template.json").Open())
        {
            resource.CopyTo(templateJson);
        }

        return path;
    }

    private static void GeneratesProjects(TestHost host)
    {
        host.Runner.On("dotnet", "new bit-bp", spec =>
        {
            var arguments = spec.Arguments.ToList();
            var name = arguments[arguments.IndexOf("--name") + 1];
            var directory = Directory.CreateDirectory(arguments[arguments.IndexOf("--output") + 1]).FullName;

            WriteProject(directory, name);
            File.WriteAllText(Path.Combine(directory, $"{name}.sln"), "Microsoft Visual Studio Solution File, Format Version 12.00");

            return new ProcessResult { ExitCode = 0, Output = "The template \"bit Boilerplate\" was created successfully." };
        });
    }

    private static void WriteProject(string directory, string name)
    {
        string[] web = [$"src/Client/{name}.Client.Core/{name}.Client.Core.csproj", $"src/Client/{name}.Client.Web/{name}.Client.Web.csproj", $"src/Server/{name}.Server.Api/{name}.Server.Api.csproj", $"src/Server/{name}.Server.Web/{name}.Server.Web.csproj", $"src/Tests/{name}.Tests.csproj", Platforms.WorkbenchProject(name), $"src/LandingPage/{name}.LandingPage.Client/{name}.LandingPage.Client.csproj", Platforms.LandingPageProject(name)];
        string[] solution = [.. web, Platforms.MauiProject(name), Platforms.WindowsProject(name)];

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{name}.slnx"), $"<Solution>{string.Concat(solution.Select(p => $"<Project Path=\"{p}\" />"))}</Solution>");
        File.WriteAllText(Path.Combine(directory, $"{name}.Web.slnf"), System.Text.Json.JsonSerializer.Serialize(new { solution = new { path = $"{name}.slnx", projects = web.Select(p => p.Replace('/', '\\')) } }));
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(directory, "src", "Client", $"{name}.Client.Core")).FullName, $"{name}.Client.Core.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Razor\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        Directory.CreateDirectory(Path.Combine(directory, "src", "Server", $"{name}.Server.Api"));
        File.WriteAllText(Path.Combine(directory, "src", "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.12" />
              </ItemGroup>
            </Project>
            """);

        var properties = Directory.CreateDirectory(Path.Combine(directory, "src", "Workbench", "Properties")).FullName;
        File.WriteAllText(Path.Combine(directory, Platforms.WorkbenchProject(name)), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        File.WriteAllText(Path.Combine(properties, "launchSettings.json"), $$"""{ "profiles": { "{{name}}.Workbench": { "commandName": "Project", "applicationUrl": "http://localhost:6030" } } }""");

        var landingPage = Directory.CreateDirectory(Path.Combine(directory, "src", "LandingPage", $"{name}.LandingPage.Server", "Properties")).FullName;
        File.WriteAllText(Path.Combine(directory, Platforms.LandingPageProject(name)), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        File.WriteAllText(Path.Combine(landingPage, "launchSettings.json"), $$"""{ "profiles": { "{{name}}.LandingPage.Server": { "commandName": "Project", "applicationUrl": "http://localhost:7030" } } }""");
    }

    private static void AssertInOrder(TestHost host, string[] expected)
    {
        var calls = host.Runner.Calls.Select(c => string.Join(' ', c.Arguments)).ToList();
        var next = 0;

        foreach (var step in expected)
        {
            var found = calls.FindIndex(next, call => call.StartsWith(step, StringComparison.Ordinal));
            Assert.AreNotEqual(-1, found, $"'{step}' didn't run after the steps before it:{Environment.NewLine}{string.Join(Environment.NewLine, calls)}");
            next = found + 1;
        }
    }

    private static string Calls(TestHost host) => string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine));
}
