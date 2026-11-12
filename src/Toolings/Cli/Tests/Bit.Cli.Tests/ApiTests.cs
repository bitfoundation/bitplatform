using System.CommandLine;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Tools;

namespace Bit.Cli.Tests;

[TestClass]
public class ApiTests
{
    private const string Current = """{"openapi":"3.1.2","paths":{"/api/v1/Todo/Get":{}}}""";

    private const string Breaking = """[{"id":"response-optional-property-removed","text":"removed the optional property `isDone` from the response with the `200` status (media type: application/json)","level":3,"operation":"GET","path":"/api/v1/Todo/Get"},{"id":"response-optional-property-removed","text":"removed the optional property `isDone` from the response with the `200` status (media type: text/plain)","level":3,"operation":"GET","path":"/api/v1/Todo/Get"},{"id":"response-optional-property-removed","text":"removed the optional property `isDone` from the response with the `200` status (media type: application/json)","level":3,"operation":"POST","path":"/api/v1/Todo/Create"},{"id":"request-property-removed","text":"removed the request property `isDone` (media type: application/json)","level":2,"operation":"POST","path":"/api/v1/Todo/Create"}]""";

    [TestMethod]
    public async Task Check_Should_BuildServerApiAndPassWhenNoReleaseIsRecordedYet()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "0.0.1");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "check"), host.Output);

        var build = host.Runner.Calls.Single();
        Assert.AreEqual("dotnet", build.FileName);
        CollectionAssert.AreEqual(new[] { "build", Path.Combine(project, "src", "Server", "Contoso.Server.Api", "Contoso.Server.Api.csproj") }, build.Arguments.ToArray());
        StringAssert.Contains(host.FlatOutput, "Built Contoso.Server.Api, which wrote openapi/current.json");
        StringAssert.Contains(host.FlatOutput, "Nothing to compare with yet");
    }

    [TestMethod]
    public async Task Check_Should_CompareEverySupportedReleaseAndListEachBreakingChangeOnce()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.3.0", minimums: new() { ["Android"] = "1.1.0", ["Ios"] = "1.2.0", ["MacOS"] = "1.2.0", ["Windows"] = "1.2.0", ["Web"] = "1.3.0" });
        Release(project, "1.0.0", "1.1.0", "1.2.0", "notes");
        host.Runner.On("oasdiff", "breaking openapi/1.1.0.json", 0, Breaking + "\n");
        host.Runner.On("oasdiff", "breaking openapi/1.2.0.json", 0, "[]\n");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "check"), host.Output);

        var compared = host.Runner.Calls.Where(c => Path.GetFileNameWithoutExtension(c.FileName) is "oasdiff").Select(c => c.Arguments[1]).ToList();
        CollectionAssert.AreEqual(new[] { "openapi/1.1.0.json", "openapi/1.2.0.json" }, compared);
        Assert.IsTrue(host.Runner.Calls.Where(c => c.FileName == Oasdiff.ExecutablePath(host.Environment)).All(c => c.Arguments.SequenceEqual(["breaking", c.Arguments[1], "openapi/current.json", "--format", "json", "--severity-levels", c.Arguments[6]])));
        StringAssert.Contains(File.ReadAllText(host.Runner.Calls.Last().Arguments[6]), "response-optional-property-removed ERR");

        var output = host.FlatOutput;
        StringAssert.Contains(output, "It supports the apps from 1.1.0 on: Android 1.1.0, iOS 1.2.0, macOS 1.2.0, Windows 1.2.0, web 1.3.0");
        StringAssert.Contains(output, "Breaks the apps at 1.1.0 1 breaking change");
        StringAssert.Contains(output, "removed the optional property `isDone` from the response with the `200` status response-optional-property-removed GET /api/v1/Todo/Get, POST /api/v1/Todo/Create");
        StringAssert.Contains(output, "removed the request property `isDone` request-property-removed");
        StringAssert.Contains(output, "Works with the apps at 1.2.0");
        StringAssert.Contains(output, "Didn't compare with 1.0.0");
        StringAssert.Contains(output, "Or raise SupportedAppVersions to 1.2.0 in src/Server/Contoso.Server.Api/appsettings.json");
        Assert.IsFalse(output.Contains("media type", StringComparison.Ordinal), output);
    }

    [TestMethod]
    public async Task Check_Should_SupportTheServersMajorWhereNoMinimumIsSetAndReadTheProductionSettings()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "2.4.0", minimums: new() { ["Android"] = "2.3.0", ["Ios"] = "2.3.0", ["MacOS"] = "2.3.0", ["Windows"] = "2.3.0" });
        File.WriteAllText(Path.Combine(ServerApi(project), "appsettings.Production.json"), """{ "supportedAppVersions": { "minimumSupportedWebAppVersion": "2.3.0", "MINIMUMSUPPORTEDIOSAPPVERSION": null } }""");
        Release(project, "1.9.0", "2.0.0", "2.3.0");
        host.Runner.On("oasdiff", "breaking", 0, "[]");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "check", "--no-build"), host.Output);

        StringAssert.Contains(host.FlatOutput, "It supports the apps from 2.0.0 on: Android 2.3.0, iOS 2.0.0, macOS 2.3.0, Windows 2.3.0, web 2.3.0");
        CollectionAssert.AreEqual(new[] { "openapi/2.0.0.json", "openapi/2.3.0.json" }, host.Runner.Calls.Select(c => c.Arguments[1]).ToArray());
    }

    [TestMethod]
    public async Task CheckRelease_Should_RequireTheVersionsDocumentToDescribeTheApi()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.2.0");
        Release(project, "1.1.0");
        host.Runner.On("oasdiff", "breaking", 0, "[]");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "check", "--release", "--no-build"), host.Output);
        StringAssert.Contains(host.FlatOutput, "openapi/1.2.0.json isn't there");

        File.WriteAllText(Path.Combine(project, "openapi", "1.2.0.json"), """{ "paths": { "/api/v1/Todo/Get": {} }, "openapi": "3.1.2" }""");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "check", "--release", "--no-build"), host.Output);
        StringAssert.Contains(host.FlatOutput, "openapi/1.2.0.json describes this api");

        File.WriteAllText(Path.Combine(project, "openapi", "1.2.0.json"), """{ "openapi": "3.1.2", "paths": {} }""");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "check", "--release", "--no-build"), host.Output);
        StringAssert.Contains(host.FlatOutput, "openapi/1.2.0.json describes another api");
    }

    [TestMethod]
    public async Task Release_Should_RecordTheApiAndDeleteTheDocumentsOfUnsupportedVersions()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.2.0", minimums: new() { ["Android"] = "1.1.0", ["Ios"] = "1.1.0", ["MacOS"] = "1.1.0", ["Windows"] = "1.1.0", ["Web"] = "1.1.0" });
        Release(project, "1.0.0", "1.1.0");
        host.Runner.On("oasdiff", "breaking", 0, "[]");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "release"), host.Output);

        Assert.AreEqual(Current, File.ReadAllText(Path.Combine(project, "openapi", "1.2.0.json")));
        Assert.IsFalse(File.Exists(Path.Combine(project, "openapi", "1.0.0.json")));
        Assert.IsTrue(File.Exists(Path.Combine(project, "openapi", "1.1.0.json")));
        StringAssert.Contains(host.FlatOutput, "Recorded the api of 1.2.0 in openapi/1.2.0.json");
        StringAssert.Contains(host.FlatOutput, "Deleted the documents of 1.0.0");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "release", "--no-build"), host.Output);
        StringAssert.Contains(host.FlatOutput, "openapi/1.2.0.json already describes this api");
    }

    [TestMethod]
    public async Task Release_Should_ChangeNothingWhenTheApiBreaksASupportedVersion()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.2.0", minimums: new() { ["Android"] = "1.1.0", ["Ios"] = "1.1.0", ["MacOS"] = "1.1.0", ["Windows"] = "1.1.0", ["Web"] = "1.1.0" });
        Release(project, "1.0.0", "1.1.0");
        host.Runner.On("oasdiff", "breaking", 0, Breaking);

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "release"), host.Output);

        Assert.IsFalse(File.Exists(Path.Combine(project, "openapi", "1.2.0.json")));
        Assert.IsTrue(File.Exists(Path.Combine(project, "openapi", "1.0.0.json")));
        StringAssert.Contains(host.FlatOutput, "Or raise SupportedAppVersions to 1.2.0 in src/Server/Contoso.Server.Api/appsettings.json, so those apps have to update before they call the server again, then run this again.");
    }

    [TestMethod]
    public async Task Release_Should_RefuseAVersionTheServerWouldForceToUpdate()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.2.0", minimums: new() { ["Android"] = "1.3.0", ["Ios"] = "1.3.0", ["MacOS"] = "1.3.0", ["Windows"] = "1.3.0", ["Web"] = "1.3.0" });
        Release(project, "1.1.0", "1.2.0");
        host.Runner.On("oasdiff", "breaking", 0, "[]");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "release"), host.Output);

        Assert.IsTrue(File.Exists(Path.Combine(project, "openapi", "1.1.0.json")));
        Assert.IsTrue(File.Exists(Path.Combine(project, "openapi", "1.2.0.json")));
        StringAssert.Contains(host.FlatOutput, "The server would refuse the apps 1.2.0 ships");
        StringAssert.Contains(host.FlatOutput, "Android needs 1.3.0");
        StringAssert.Contains(host.FlatOutput, "Raise the version in src/Directory.Build.props to 1.3.0 or above");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "check", "--release", "--no-build"), host.Output);
        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, project, "api", "check", "--no-build"), host.Output);
    }

    [TestMethod]
    public async Task Release_Should_NeverChangeARecordedDocument()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.2.0");
        Release(project, "1.2.0");
        File.WriteAllText(Path.Combine(project, "openapi", "1.2.0.json"), """{ "openapi": "3.1.2", "paths": {} }""");
        host.Runner.On("oasdiff", "breaking", 0, "[]");

        Assert.AreEqual(CliApp.ExitCheckFailed, await RunInAsync(host, project, "api", "release", "--no-build"), host.Output);

        Assert.AreEqual("""{ "openapi": "3.1.2", "paths": {} }""", File.ReadAllText(Path.Combine(project, "openapi", "1.2.0.json")));
        StringAssert.Contains(host.FlatOutput, "openapi/1.2.0.json describes another api");
        StringAssert.Contains(host.FlatOutput, "Raise the version in src/Directory.Build.props, or delete openapi/1.2.0.json if 1.2.0 was never released");
    }

    [TestMethod]
    public async Task Check_Should_FailWhenTheBuildDoesNotWriteTheDocument()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.0.0", writesDocument: false);

        Assert.AreEqual(CliApp.ExitFailed, await RunInAsync(host, project, "api", "check"), host.Output);

        StringAssert.Contains(host.FlatOutput, "the build didn't write openapi/current.json");
    }

    [TestMethod]
    public async Task Check_Should_RefuseAVersionThatIsNotNumbers()
    {
        using var host = new TestHost();
        var project = CreateProject(host, version: "1.0.0-beta");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, project, "api", "check"), host.Output);

        StringAssert.Contains(host.FlatOutput, "The <Version> in src/Directory.Build.props is '1.0.0-beta'");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Oasdiff_Should_RefuseAnArchiveWhoseHashDiffers()
    {
        using var host = new TestHost(os: HostOs.Linux);
        using var handler = new ArchiveHandler(TarGz("oasdiff", "#!/bin/sh\necho planted\n"));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => Oasdiff.EnsureAsync(host.Environment, handler, CancellationToken.None));

        Assert.AreEqual(Oasdiff.ArchiveUrl("linux_amd64"), handler.RequestedUrl);
        Assert.IsFalse(File.Exists(Oasdiff.ExecutablePath(host.Environment)));
    }

    [TestMethod]
    public void Oasdiff_Should_HaveABuildForEveryOsAndArchitectureBitRunsOn()
    {
        foreach (var os in Enum.GetValues<HostOs>())
        {
            foreach (var architecture in new[] { System.Runtime.InteropServices.Architecture.X64, System.Runtime.InteropServices.Architecture.Arm64 })
            {
                using var host = new TestHost(os: os);
                var environment = new CliEnvironment { Os = os, Architecture = architecture, HomeDirectory = host.Home, CurrentDirectory = host.WorkingDirectory, Variables = host.Environment.Variables };

                Assert.IsNotNull(Oasdiff.Platform(environment), $"{os} {architecture}");
            }
        }
    }

    private static string ServerApi(string project) => Path.Combine(project, "src", "Server", "Contoso.Server.Api");

    private static string CreateProject(TestHost host, string version, Dictionary<string, string?>? minimums = null, bool writesDocument = true)
    {
        var directory = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso")).FullName;
        File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(directory, "Contoso.Web.slnf"), "{}");
        Directory.CreateDirectory(ServerApi(directory));
        File.WriteAllText(Path.Combine(directory, "src", "Directory.Build.props"), $"<Project><PropertyGroup><Nullable>enable</Nullable><Version>{version}</Version></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(ServerApi(directory), "Contoso.Server.Api.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");

        var section = string.Join(",\n", new[] { "Android", "Ios", "MacOS", "Windows", "Web" }.Select(p => $"        \"MinimumSupported{p}AppVersion\": {(minimums?.GetValueOrDefault(p) is { } value ? $"\"{value}\"" : "null")}"));
        File.WriteAllText(Path.Combine(ServerApi(directory), "appsettings.json"), $"{{\n    // The api's settings\n    \"SupportedAppVersions\": {{\n{section},\n    }}\n}}");

        Directory.CreateDirectory(Path.Combine(directory, "openapi"));
        Directory.CreateDirectory(Path.GetDirectoryName(Oasdiff.ExecutablePath(host.Environment))!);
        File.WriteAllText(Oasdiff.ExecutablePath(host.Environment), "");

        if (writesDocument)
        {
            File.WriteAllText(Path.Combine(directory, "openapi", "current.json"), Current);
        }

        host.Runner.On("dotnet", "build", spec =>
        {
            if (writesDocument)
            {
                File.WriteAllText(Path.Combine(directory, "openapi", "current.json"), Current);
            }

            return new ProcessResult { ExitCode = 0, Output = "Build succeeded." };
        });

        return directory;
    }

    private static void Release(string project, params string[] versions)
    {
        foreach (var version in versions)
        {
            File.WriteAllText(Path.Combine(project, "openapi", $"{version}.json"), Current);
        }
    }

    private static byte[] TarGz(string name, string content)
    {
        using var output = new MemoryStream();

        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)) });
        }

        return output.ToArray();
    }

    private sealed class ArchiveHandler(byte[] archive) : HttpMessageHandler
    {
        public string? RequestedUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(archive) });
        }
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
