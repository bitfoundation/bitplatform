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
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "workload" or "restore" or "build"), Calls(host));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "git" or "node" or "docker" or "aspire"), Calls(host));
        StringAssert.Contains(host.Output, "bit setup");
        Assert.IsTrue(File.Exists(Path.Combine(host.WorkingDirectory, "Contoso", "Contoso.slnx")));
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "Contoso", "Contoso.sln")));
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

        var order = new[] { "new install", "new bit-bp", "workload list", "restore", "build Contoso.Web.slnf", "format Contoso", "dnx dotnet-ef" };
        var dotnetCalls = host.Runner.Calls.Where(c => c.FileName is "dotnet").Select(c => string.Join(' ', c.Arguments)).ToList();
        var positions = order.Select(step => dotnetCalls.FindIndex(call => call.Contains(step, StringComparison.Ordinal))).ToArray();

        CollectionAssert.DoesNotContain(positions, -1, string.Join(Environment.NewLine, dotnetCalls));
        CollectionAssert.AreEqual(positions.Order().ToArray(), positions, string.Join(Environment.NewLine, dotnetCalls));
        StringAssert.Contains(host.Output, "NU1301");
        StringAssert.Contains(host.Output, "dotnet restore Contoso.Web.slnf");
        StringAssert.Contains(host.Output, "Contoso is ready");
    }

    [TestMethod]
    public async Task AMissingTemplate_Should_SkipTheProjectStepsInsteadOfFailingThem()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.On("dotnet", "new install", 1, "error: Bit.Boilerplate::0.0.0 could not be installed, the package does not exist.");

        var exitCode = await host.RunAsync("new", "Contoso", "--template-package", FakeTemplatePackage(host), "--yes", "--no-setup", "--no-git", "--no-trust");

        Assert.AreEqual(CliApp.ExitFailed, exitCode, host.Output);
        StringAssert.Contains(host.Output, "Contoso wasn't created");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.FirstOrDefault() is "format" or "dnx"), Calls(host));
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
        Assert.AreEqual(first.Package.HiveDirectory, again.Package?.HiveDirectory);
        Assert.AreNotEqual(first.Package.HiveDirectory, changed.Package?.HiveDirectory);
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
        Assert.AreEqual(package.PackagePath, again?.PackagePath);
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.Take(2).SequenceEqual(["new", "install"])), Calls(host));
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
        StringAssert.Contains(result.Output, "isn't a bit Boilerplate version");
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

        Assert.AreEqual(installed, package?.PackagePath);
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
        Assert.AreEqual(1, host.Runner.Calls.Count(c => c.Arguments.Take(2).SequenceEqual(["new", "install"])), Calls(host));
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

        Assert.AreEqual(TemplateSource.FolderVersion, first.Package?.Version);
        Assert.AreEqual(first.Package!.HiveDirectory, again.Package?.HiveDirectory);
        var install = host.Runner.Calls.Single(c => c.Arguments.Take(2).SequenceEqual(["new", "install"]));
        Assert.AreEqual(folder, install.Arguments[2]);
        Assert.IsNotNull(TemplateSource.ReadManifest(folder));
        StringAssert.Contains(TemplateSource.ReadEntry(folder, "Bit.Boilerplate/global.json"), "10.0.300");
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

            File.WriteAllText(Path.Combine(directory, $"{name}.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, $"{name}.sln"), "Microsoft Visual Studio Solution File, Format Version 12.00");
            File.WriteAllText(Path.Combine(directory, $"{name}.Web.slnf"), "{}");
            Directory.CreateDirectory(Path.Combine(directory, "src", "Server", $"{name}.Server.Api"));
            File.WriteAllText(Path.Combine(directory, "src", "Directory.Packages.props"), """
                <Project>
                  <ItemGroup>
                    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.12" />
                  </ItemGroup>
                </Project>
                """);

            return new ProcessResult { ExitCode = 0, Output = "The template \"bit Boilerplate\" was created successfully." };
        });
    }

    private static string Calls(TestHost host) => string.Join(Environment.NewLine, host.Runner.Calls.Select(c => c.CommandLine));
}
