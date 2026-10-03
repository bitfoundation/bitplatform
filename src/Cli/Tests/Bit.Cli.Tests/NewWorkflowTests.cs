using System.IO.Compression;
using Bit.Cli.Infrastructure;
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

        var order = new[] { "new install", "new bit-bp", "workload list", "restore", "build Contoso.Web.slnf", "format", "dnx dotnet-ef" };
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
