using System.Text;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class RenameTests
{
    private static readonly byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. "Old OldDb com.companyname.old"u8];

    private static readonly byte[] blob = [0x00, 0x01, .. "namespace Old.Server.Api;"u8, 0x00];

    [TestMethod]
    public async Task Rename_Should_ReplaceTheNameEverywhereAndKeepTheRest()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder);
        host.Runner.On("git", "status --porcelain", 0, "");

        var exitCode = await host.RunAsync("rename", "Contoso.Shop", folder);

        Assert.AreEqual(0, exitCode, host.Output);
        Assert.AreEqual("Contoso.Shop", ProjectContext.FindProjectName(folder));

        var api = Path.Combine(folder, "src", "Server", "Contoso.Shop.Server.Api");
        var core = Path.Combine(folder, "src", "Client", "Contoso.Shop.Client.Core");

        Assert.IsFalse(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, "src", "Server", "Old.Server.Api")));
        Assert.AreEqual("<Solution>\n  <Project Path=\"src/Server/Contoso.Shop.Server.Api/Contoso.Shop.Server.Api.csproj\" />\n</Solution>\n", File.ReadAllText(Path.Combine(folder, "Contoso.Shop.slnx")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(folder, "Contoso.Shop.Web.slnf")), "\"path\": \"Contoso.Shop.slnx\"");
        StringAssert.Contains(File.ReadAllText(Path.Combine(api, "Contoso.Shop.Server.Api.csproj")), "<RootNamespace>Contoso.Shop.Server.Api</RootNamespace>");

        var code = File.ReadAllBytes(Path.Combine(api, "Data", "AppDbContext.cs"));
        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, code[..3]);
        Assert.AreEqual("namespace Contoso.Shop.Server.Api.Data;\n\npublic class AppDbContext\n{\n    public const string Database = \"Contoso.ShopDb\";\n    public const string Words = \"Bold Golden MyOldValue Unfold\";\n}\n", Encoding.UTF8.GetString(code[3..]));

        Assert.AreEqual("@echo off\r\ndotnet run --project src\\Server\\Contoso.Shop.Server.Api\r\necho contoso.shop-e2e\r\n", File.ReadAllText(Path.Combine(folder, "run.cmd")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(folder, "src", "Client", "Contoso.Shop.Client.Maui", "Contoso.Shop.Client.Maui.csproj")), "<ApplicationId>com.companyname.contoso.shop</ApplicationId>");

        Assert.IsFalse(Directory.Exists(Path.Combine(api, "bin")));
        Assert.IsFalse(Directory.Exists(Path.Combine(api, "obj")));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, ".vs")));

        Assert.AreEqual("module.exports = 'Old.Server.Api';", File.ReadAllText(Path.Combine(core, "node_modules", "old-package", "bin", "Old.js")));
        CollectionAssert.AreEqual(png, File.ReadAllBytes(Path.Combine(api, "wwwroot", "Contoso.Shop.png")));
        CollectionAssert.AreEqual(blob, File.ReadAllBytes(Path.Combine(api, "Contoso.Shop.dat")));

        Assert.IsTrue(host.Runner.Calls.Any(c => c.FileName is "dotnet" && string.Join(' ', c.Arguments) is "build-server shutdown"));
        StringAssert.Contains(host.FlatOutput, "Old is now Contoso.Shop");
        StringAssert.Contains(host.FlatOutput, "git diff");
        StringAssert.Contains(host.FlatOutput, "bit setup");
        StringAssert.Contains(host.FlatOutput, "The project folder is still named Old");
    }

    [TestMethod]
    public async Task Rename_Should_RefuseAGitRepositoryWithChanges()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder);
        host.Runner.On("git", "status --porcelain", 0, " M Old.slnx\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder, "--yes"));

        StringAssert.Contains(host.FlatOutput, "Commit or stash them first");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsTrue(Directory.Exists(Path.Combine(folder, "src", "Server", "Old.Server.Api", "bin")));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.FileName is "dotnet"));
    }

    [TestMethod]
    public async Task Rename_Should_AskBeforeRenamingAFolderGitCantRestore()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder));
        StringAssert.Contains(host.FlatOutput, "Pass --yes");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));

        var prompter = new ScriptedPrompter().On("isn't in a git repository", false);
        using var interactive = new TestHost(HostOs.Linux, prompter: prompter);
        var other = CreateProject(interactive);
        interactive.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await interactive.RunAsync("rename", "Contoso.Shop", other));
        Assert.AreEqual("no", prompter.Find("isn't in a git repository")!.Preselected.Single());
        Assert.IsTrue(File.Exists(Path.Combine(other, "Old.slnx")));

        Assert.AreEqual(0, await host.RunAsync("rename", "Contoso.Shop", folder, "--yes"), host.Output);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Shop.slnx")));
        Assert.IsFalse(host.FlatOutput.Contains("git diff", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task Rename_Should_RejectAnInvalidNameWithASuggestion()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "contoso-shop", folder));
        StringAssert.Contains(host.Output, "bit rename ContosoShop");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_LeaveAProjectWithTheSameNameAlone()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);

        Assert.AreEqual(0, await host.RunAsync("rename", "Old", folder));
        StringAssert.Contains(host.Output, "already named");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_RequireAProjectFolder()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop"));
        StringAssert.Contains(host.Output, "isn't a bit Boilerplate project folder");
    }

    [TestMethod]
    public void Replace_Should_MatchOnlyTheNameOnItsOwn()
    {
        var dotted = new ProjectRenamer("Old.App", "Contoso.Shop");

        Assert.AreEqual("Contoso.Shop.Server Contoso_Shop contoso.shop-e2e XOld.App my_old.app Contoso.ShopDb", dotted.Replace("Old.App.Server Old_App old.app-e2e XOld.App my_old.app Old.AppDb"));

        var growing = new ProjectRenamer("Shop", "ShopHub");

        Assert.AreEqual("ShopHub.Server ShopHubDb shophub-e2e com.companyname.shophub Workshop Shopping shopping Shops", growing.Replace("Shop.Server ShopDb shop-e2e com.companyname.shop Workshop Shopping shopping Shops"));

        var text = "nothing to see";
        Assert.AreSame(text, growing.Replace(text));
    }

    private static string CreateProject(TestHost host)
    {
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Old")).FullName;
        var api = Path.Combine(folder, "src", "Server", "Old.Server.Api");
        var core = Path.Combine(folder, "src", "Client", "Old.Client.Core");

        Write(Path.Combine(folder, "Old.slnx"), "<Solution>\n  <Project Path=\"src/Server/Old.Server.Api/Old.Server.Api.csproj\" />\n</Solution>\n");
        Write(Path.Combine(folder, "Old.Web.slnf"), "{\n  \"solution\": {\n    \"path\": \"Old.slnx\",\n    \"projects\": [ \"src\\\\Server\\\\Old.Server.Api\\\\Old.Server.Api.csproj\" ]\n  }\n}\n");
        Write(Path.Combine(folder, "run.cmd"), "@echo off\r\ndotnet run --project src\\Server\\Old.Server.Api\r\necho old-e2e\r\n");
        Write(Path.Combine(api, "Old.Server.Api.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <PropertyGroup>\n    <RootNamespace>Old.Server.Api</RootNamespace>\n  </PropertyGroup>\n</Project>\n");
        Write(Path.Combine(folder, "src", "Client", "Old.Client.Maui", "Old.Client.Maui.csproj"), "<Project>\n  <PropertyGroup>\n    <ApplicationId>com.companyname.old</ApplicationId>\n  </PropertyGroup>\n</Project>\n");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(api, "Data")).FullName, "AppDbContext.cs"),
            "namespace Old.Server.Api.Data;\n\npublic class AppDbContext\n{\n    public const string Database = \"OldDb\";\n    public const string Words = \"Bold Golden MyOldValue Unfold\";\n}\n", new UTF8Encoding(true));
        Write(Path.Combine(api, "bin", "Debug", "Old.Server.Api.dll"), "MZ");
        Write(Path.Combine(api, "obj", "project.assets.json"), "{ \"project\": \"Old.Server.Api\" }");
        Write(Path.Combine(folder, ".vs", "Old", "v17", ".suo"), "Old");
        Write(Path.Combine(core, "node_modules", "old-package", "bin", "Old.js"), "module.exports = 'Old.Server.Api';");
        File.WriteAllBytes(Path.Combine(Directory.CreateDirectory(Path.Combine(api, "wwwroot")).FullName, "Old.png"), png);
        File.WriteAllBytes(Path.Combine(api, "Old.dat"), blob);

        return folder;
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
