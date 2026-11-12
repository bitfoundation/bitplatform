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
        Assert.Contains("\"path\": \"Contoso.Shop.slnx\"", File.ReadAllText(Path.Combine(folder, "Contoso.Shop.Web.slnf")));
        Assert.Contains("<RootNamespace>Contoso.Shop.Server.Api</RootNamespace>", File.ReadAllText(Path.Combine(api, "Contoso.Shop.Server.Api.csproj")));

        var code = File.ReadAllBytes(Path.Combine(api, "Data", "AppDbContext.cs"));
        Assert.AreSequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }, code[..3]);
        Assert.AreEqual("namespace Contoso.Shop.Server.Api.Data;\n\npublic class AppDbContext\n{\n    public const string Database = \"Contoso.ShopDb\";\n    public const string Words = \"Bold Golden MyOldValue Unfold\";\n}\n", Encoding.UTF8.GetString(code[3..]));

        Assert.AreEqual("@echo off\r\ndotnet run --project src\\Server\\Contoso.Shop.Server.Api\r\necho contoso.shop-e2e\r\n", File.ReadAllText(Path.Combine(folder, "run.cmd")));
        Assert.Contains("<ApplicationId>com.companyname.contoso.shop</ApplicationId>", File.ReadAllText(Path.Combine(folder, "src", "Client", "Contoso.Shop.Client.Maui", "Contoso.Shop.Client.Maui.csproj")));

        Assert.IsFalse(Directory.Exists(Path.Combine(api, "bin")));
        Assert.IsFalse(Directory.Exists(Path.Combine(api, "obj")));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, ".vs")));

        Assert.AreEqual("module.exports = 'Old.Server.Api';", File.ReadAllText(Path.Combine(core, "node_modules", "old-package", "bin", "Old.js")));
        Assert.AreSequenceEqual(png, File.ReadAllBytes(Path.Combine(api, "wwwroot", "Contoso.Shop.png")));
        Assert.AreSequenceEqual(blob, File.ReadAllBytes(Path.Combine(api, "Contoso.Shop.dat")));

        Assert.DoesNotContain(c => c.FileName is "dotnet" && c.Arguments.FirstOrDefault() is "build-server", host.Runner.Calls);
        Assert.Contains("Old is now Contoso.Shop", host.FlatOutput);
        Assert.Contains("git diff", host.FlatOutput);
        Assert.Contains("bit setup", host.FlatOutput);
        Assert.Contains("The project folder is still named Old", host.FlatOutput);
    }

    [TestMethod]
    public async Task Rename_Should_LeaveFilesAndFoldersAloneWhenAFileCantBeChanged()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder);
        host.Runner.On("git", "status --porcelain", 0, "");
        var csproj = Path.Combine(folder, "src", "Server", "Old.Server.Api", "Old.Server.Api.csproj");

        using (File.Open(csproj, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("rename", "Contoso.Shop", folder), host.Output);
        }

        Assert.Contains("Didn't rename files and folders", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.Contains("<RootNamespace>Old.Server.Api</RootNamespace>", File.ReadAllText(csproj));
        Assert.IsEmpty(Directory.GetFiles(folder, "*.tmp", SearchOption.AllDirectories));
    }

    [TestMethod]
    public async Task Rename_Should_KeepTheOldNameUntilEverythingIsRenamed()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder);
        host.Runner.On("git", "status --porcelain", 0, "");
        var blocker = Directory.CreateDirectory(Path.Combine(folder, "src", "Server", "Contoso.Shop.Server.Api")).FullName;

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("rename", "Contoso.Shop", folder), host.Output);
        Assert.AreEqual("Old", ProjectContext.FindProjectName(folder));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));

        Directory.Delete(blocker);

        Assert.AreEqual(0, await host.RunAsync("rename", "Contoso.Shop", folder), host.Output);
        Assert.AreEqual("Contoso.Shop", ProjectContext.FindProjectName(folder));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Contoso.Shop.slnx")));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "src", "Server", "Contoso.Shop.Server.Api", "Contoso.Shop.Server.Api.csproj")));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, "src", "Server", "Old.Server.Api")));
    }

    [TestMethod]
    public async Task Rename_Should_RefuseAGitRepositoryWithChanges()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 0, folder);
        host.Runner.On("git", "status --porcelain", 0, " M Old.slnx\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder, "--yes"));

        Assert.Contains("Commit or stash them first", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsTrue(Directory.Exists(Path.Combine(folder, "src", "Server", "Old.Server.Api", "bin")));
        Assert.DoesNotContain(c => c.FileName is "dotnet", host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_AskBeforeRenamingAFolderGitCantRestore()
    {
        using var host = new TestHost(HostOs.Linux);
        var folder = CreateProject(host);
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder));
        Assert.Contains("Pass --yes", host.FlatOutput);
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
        Assert.Contains("bit rename ContosoShop", host.Output);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_RefuseANameThatIsATypeInTheProject()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);
        Write(Path.Combine(folder, "src", "Client", "Old.Client.Core", "Components", "Shop.razor"), "<h1>Shop</h1>\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder, "--yes"));
        Assert.Contains("'Shop' is also the name of a type", host.FlatOutput);
        Assert.Contains("bit rename Contoso.MyShop", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_RefuseANameThatStartsWithANamespaceOfTheProject()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Server.Shop", folder, "--yes"));
        Assert.Contains("'Server' is also the name of a namespace inside the project", host.FlatOutput);
        Assert.Contains("bit rename MyServer.Shop", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_RefuseAProjectNamedLikeOneOfItsTypes()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);
        Write(Path.Combine(folder, "src", "Server", "Old.Server.Api", "Components", "Old.razor.cs"), "namespace Old.Server.Api.Components;\n\npublic partial class Old;\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop", folder, "--yes"));
        Assert.Contains("can't tell which Old to change", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_KeepTheNativeAppsAppIdValid()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Магазин", folder, "--yes"));
        Assert.Contains("com.companyname.магазин", host.FlatOutput);
        Assert.IsTrue(File.Exists(Path.Combine(folder, "Old.slnx")));
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_LeaveAProjectWithTheSameNameAlone()
    {
        using var host = new TestHost();
        var folder = CreateProject(host);

        Assert.AreEqual(0, await host.RunAsync("rename", "Old", folder));
        Assert.Contains("already named", host.Output);
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task Rename_Should_RequireAProjectFolder()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "Contoso.Shop"));
        Assert.Contains("isn't a bit Boilerplate project folder", host.Output);
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

    [TestMethod]
    public void Replace_Should_LeaveTheNamesACommonWordIsPartOf()
    {
        var app = new ProjectRenamer("App", "Contoso.Shop");

        Assert.AreEqual("<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />", app.Replace("<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />"));
        Assert.AreEqual("var app = builder.Build(); AppDomain.CurrentDomain AppDbContext Contoso.ShopDb.db Contoso.ShopJobDb.db <ApplicationId>com.companyname.contoso.shop</ApplicationId>",
            app.Replace("var app = builder.Build(); AppDomain.CurrentDomain AppDbContext AppDb.db AppJobDb.db <ApplicationId>com.companyname.app</ApplicationId>"));

        var web = new ProjectRenamer("Web", "Contoso");

        Assert.AreEqual("<Project Sdk=\"Microsoft.NET.Sdk.Web\">", web.Replace("<Project Sdk=\"Microsoft.NET.Sdk.Web\">"));
        Assert.AreEqual("Contoso.Web.slnf src/Server/Contoso.Server.Web Microsoft.AspNetCore.Components.WebAssembly WebAssemblyHostBuilder _framework/blazor.web.js",
            web.Replace("Web.Web.slnf src/Server/Web.Server.Web Microsoft.AspNetCore.Components.WebAssembly WebAssemblyHostBuilder _framework/blazor.web.js"));

        var test = new ProjectRenamer("Test", "Contoso");

        Assert.AreEqual("[TestMethod] Microsoft.NET.Test.Sdk dotnet test test@bitplatform.dev /iPad/.test(agent) contoso-e2e Projects.Contoso_Server_Api",
            test.Replace("[TestMethod] Microsoft.NET.Test.Sdk dotnet test test@bitplatform.dev /iPad/.test(agent) test-e2e Projects.Test_Server_Api"));

        var lowercase = new ProjectRenamer("shop", "Contoso");

        Assert.AreEqual("Contoso.Server.Api com.companyname.contoso contoso-e2e", lowercase.Replace("shop.Server.Api com.companyname.shop shop-e2e"));
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
