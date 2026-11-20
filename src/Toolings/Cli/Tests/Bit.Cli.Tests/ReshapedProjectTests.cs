using System.CommandLine;
using Bit.Cli.Commands;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Pipelines;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ReshapedProjectTests
{
    private const string AssetLinks = """[{"relation":["delegate_permission/common.handle_all_urls"],"target":{"namespace":"android_app","package_name":"com.bitplatform.AdminPanel.Template","sha256_cert_fingerprints":["AA"]}}]""";

    private const string AppleAppSiteAssociation = """{"applinks":{"details":[{"appID":"TEAM654321.com.bitplatform.AdminPanel.Template","paths":["*"]}]}}""";

    [TestMethod]
    public void APart_Should_BeTheOneAnOptionNamesThenTheTemplatesThenTheOneTheSolutionLists()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var template = ProjectContext.At(directory, "Shop");

        Assert.AreEqual(Path.Combine(directory, "src", "Server", "Shop.Server.Api"), template.ServerApiDirectory);
        Assert.AreEqual(Path.Combine(directory, "src", "Server", "Shop.Server.Web"), template.ServerWebDirectory);
        Assert.AreEqual(Path.Combine(directory, "src", "Server", "Shop.Server.AppHost"), template.AppHostDirectory);
        Assert.AreEqual(Path.Combine(directory, "src", "Client", "Shop.Client.Core"), template.ClientCoreDirectory);
        Assert.AreEqual(Path.Combine(directory, "src", "Workbench"), template.WorkbenchDirectory);
        Assert.AreEqual(Path.Combine(directory, "src", "LandingPage", "Shop.LandingPage.Server"), template.LandingPageDirectory);
        Assert.AreEqual("src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj", template.MauiProject);

        var api = Write(Path.Combine(directory, "backend", "Api", "Shop.Server.Api.csproj"), "<Project />");
        var appHost = Write(Path.Combine(directory, "hosting", "Shop.Server.AppHost.csproj"), "<Project />");
        var workbench = Write(Path.Combine(directory, "design", "Shop.Workbench.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(directory, "Shop.slnx"), """
            <Solution>
              <Project Path="backend/Api/Shop.Server.Api.csproj" />
              <Project Path="hosting\Shop.Server.AppHost.csproj" />
              <Project Path="design/Shop.Workbench.csproj" />
            </Solution>
            """);

        Assert.AreEqual(api, template.ServerApiProject);
        Assert.AreEqual(appHost, template.AppHostProject);
        Assert.AreEqual(workbench, template.WorkbenchProject);
        Assert.AreEqual(Path.Combine(directory, "src", "Server", "Shop.Server.Web"), template.ServerWebDirectory, "The solution lists none, so it stays the template's.");

        var named = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project />");
        var given = ProjectContext.At(directory, "Shop", new ProjectParts { ServerApi = named, Maui = Path.Combine(directory, "apps", "Mobile", "Mobile.csproj") });

        Assert.AreEqual(named, given.ServerApiProject);
        Assert.AreEqual("apps/Mobile/Mobile.csproj", given.MauiProject);
        Assert.AreEqual(appHost, given.AppHostProject);
    }

    [TestMethod]
    public void TheTemplatesParts_Should_BeWhereBitLooksForThem()
    {
        var directory = Path.GetDirectoryName(TestHost.RepositoryFile("src", "Templates", "Boilerplate", "Bit.Boilerplate", "Boilerplate.slnx"))!;
        var template = ProjectContext.At(directory, "Boilerplate");
        var web = Path.Combine(directory, "src", "Client", "Boilerplate.Client.Web");

        foreach (var project in new[] { template.ServerApiProject, template.ServerWebProjects.Single(), template.AppHostProject, template.WorkbenchProject, template.LandingPageProject, Path.Combine(directory, template.MauiProject), Path.Combine(directory, template.WindowsProject) })
        {
            Assert.IsTrue(File.Exists(project), project);
        }

        Assert.IsTrue(File.Exists(Path.Combine(template.ClientCoreDirectory, "package.json")));
        CollectionAssert.AreEqual(new[] { Path.Combine(web, "Boilerplate.Client.Web.csproj") }, template.WebProjects.ToArray());
        CollectionAssert.AreEqual(new[] { Path.Combine(web, "wwwroot", ".well-known") }, WellKnownFiles.Folders(template).ToArray());
        Assert.HasCount(1, WellKnownFiles.Files(template, WellKnownFiles.AssetLinks));
        Assert.HasCount(1, WellKnownFiles.Files(template, WellKnownFiles.AppleAppSiteAssociation));
        Assert.IsEmpty(template.PartsNotFound(settingUp: true).ToArray());

        var appHost = string.Join('\n', ProjectContext.FilesUnder(template.AppHostDirectory, "*.cs").Select(File.ReadAllText));

        Assert.AreEqual("serverweb", ProjectSteps.AspireResource(appHost, template.ServerWebProjects.Single(), ""));
        Assert.AreEqual("serverapi", ProjectSteps.AspireResource(appHost, template.ServerApiProject, ""));
    }

    [TestMethod]
    public async Task KeysAndroid_Should_AddTheFingerprintToTheAssetLinksOfEveryWebApp()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        Maui(directory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var operations = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Operations.Web", "wwwroot", ".well-known"));
        var management = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Management.Web", "wwwroot", ".well-known"));
        var built = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Operations.Web", "bin", "Release", "wwwroot", ".well-known"));
        var notServed = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Operations.Web", "samples", ".well-known"));

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android"), host.Output);

        foreach (var folder in new[] { operations, management })
        {
            StringAssert.Contains(File.ReadAllText(Path.Combine(folder, WellKnownFiles.AssetLinks)), "com.contoso.shop", folder);
        }

        foreach (var folder in new[] { built, notServed })
        {
            Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(folder, WellKnownFiles.AssetLinks)), folder);
        }

        StringAssert.Contains(host.FlatOutput, "Added its fingerprint to assetlinks.json");
        StringAssert.Contains(host.FlatOutput, "src/Client/Shop.Client.Management.Web/wwwroot/.well-known/assetlinks.json, src/Client/Shop.Client.Operations.Web/wwwroot/.well-known/assetlinks.json");
    }

    [TestMethod]
    public async Task KeysAndroid_Should_SayItFoundNoAssetLinksAndTakeTheirFolder()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        Maui(directory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var elsewhere = WellKnown(Path.Combine(directory, "web", "public", ".well-known"));

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Didn't add its fingerprint to assetlinks.json");
        StringAssert.Contains(host.FlatOutput, "bit found none in a wwwroot/.well-known folder under src/Client");
        StringAssert.Contains(host.FlatOutput, "--well-known names its folder.");
        Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(elsewhere, WellKnownFiles.AssetLinks)));

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android", "-o", Path.Combine(host.Root, "second.keystore"), "--well-known", Path.Combine("web", "public", ".well-known")), host.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(elsewhere, WellKnownFiles.AssetLinks)), "com.contoso.shop");
    }

    [TestMethod]
    public async Task KeysAndroid_Should_SayItDoesntKnowTheAppIdAndTakeTheAppsProject()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var wellKnown = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Web", "wwwroot", ".well-known"));
        Maui(directory, "apps/Mobile/Mobile.csproj");

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android"), host.Output);

        StringAssert.Contains(host.FlatOutput, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj isn't there or has no <ApplicationId>");
        StringAssert.Contains(host.FlatOutput, "--maui names the Android app's .csproj.");
        Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(wellKnown, WellKnownFiles.AssetLinks)));

        Assert.AreEqual(0, await RunInAsync(host, host.Root, "keys", "android", "-o", Path.Combine(host.Root, "second.keystore"), "--project", directory, "--maui", Path.Combine("apps", "Mobile")), host.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(wellKnown, WellKnownFiles.AssetLinks)), "com.contoso.shop");
    }

    [TestMethod]
    public async Task KeysCertificate_Should_NameTheOptionInsteadOfMakingAServerApiFolder()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var backend = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project />");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, directory, "keys", "certificate"), host.Output);

        StringAssert.Contains(host.FlatOutput, "src/Server/Shop.Server.Api isn't there");
        StringAssert.Contains(host.FlatOutput, "Pass its .csproj with --server-api, or a folder with -o.");
        Assert.IsFalse(Directory.Exists(Path.Combine(directory, "src", "Server")));

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "certificate", "--server-api", backend), host.Output);

        Assert.IsNotNull(AppCertificates.Read(Path.GetDirectoryName(backend)!, AppCertificates.ActiveName));
    }

    [TestMethod]
    public async Task KeysApple_Should_ReadTheBundleIdOfTheAppsProjectItIsGiven()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var maui = Maui(directory, "apps/Mobile/Mobile.csproj", "not a bundle id");
        var p8 = Write(Path.Combine(host.Root, "AuthKey_2X9R4HXF34.p8"), "key");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, directory, "keys", "apple", "--issuer-id", "issuer", "--key-id", "2X9R4HXF34", "--private-key", p8), host.Output);
        StringAssert.Contains(host.FlatOutput, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj isn't there");
        StringAssert.Contains(host.FlatOutput, "or the iOS app's .csproj with --maui");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, "keys", "apple", "--issuer-id", "issuer", "--key-id", "2X9R4HXF34", "--private-key", p8, "--project", directory, "--maui", maui), host.Output);
        StringAssert.Contains(host.FlatOutput, "Apple doesn't accept not a bundle id as a bundle id");
    }

    [TestMethod]
    public async Task Pipeline_Should_AddTheKeysFingerprintToEveryAssetLinksAndSayWhenItFindsNone()
    {
        using var host = Repository();
        AndroidWorkflow(host);
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "Shop.slnx"), "<Solution />");
        Maui(host.WorkingDirectory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Didn't add the key's fingerprint to assetlinks.json");
        StringAssert.Contains(host.FlatOutput, "bit found none in a wwwroot/.well-known folder under src/Client, so its Android App Links won't verify. --well-known names its folder. To add it by hand, the fingerprint of com.contoso.shop is ");

        using var split = Repository();
        AndroidWorkflow(split);
        File.WriteAllText(Path.Combine(split.WorkingDirectory, "Shop.slnx"), "<Solution />");
        Maui(split.WorkingDirectory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var operations = WellKnown(Path.Combine(split.WorkingDirectory, "src", "Client", "Shop.Client.Operations.Web", "wwwroot", ".well-known"));
        var management = WellKnown(Path.Combine(split.WorkingDirectory, "src", "Client", "Shop.Client.Management.Web", "wwwroot", ".well-known"));
        var served = WellKnown(Path.Combine(split.WorkingDirectory, "public", ".well-known"));

        Assert.AreEqual(0, await split.RunAsync("pipeline", "--yes"), split.Output);

        foreach (var folder in new[] { operations, management })
        {
            StringAssert.Contains(File.ReadAllText(Path.Combine(folder, WellKnownFiles.AssetLinks)), "com.contoso.shop", folder);
        }

        Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(served, WellKnownFiles.AssetLinks)));
        Assert.IsFalse(split.Output.Contains("Didn't add", StringComparison.Ordinal), split.Output);
        StringAssert.Contains(split.FlatOutput, "src/Client/Shop.Client.Management.Web/wwwroot/.well-known/assetlinks.json, src/Client/Shop.Client.Operations.Web/wwwroot/.well-known/assetlinks.json");

        using var given = Repository();
        AndroidWorkflow(given);
        File.WriteAllText(Path.Combine(given.WorkingDirectory, "Shop.slnx"), "<Solution />");
        Maui(given.WorkingDirectory, "apps/Mobile/Mobile.csproj");
        var named = WellKnown(Path.Combine(given.WorkingDirectory, "public", ".well-known"));

        Assert.AreEqual(0, await given.RunAsync("pipeline", "--yes", "--maui", "apps/Mobile/Mobile.csproj", "--well-known", "public/.well-known"), given.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(named, WellKnownFiles.AssetLinks)), "com.contoso.shop");
    }

    [TestMethod]
    public async Task TheWellKnownFolders_Should_BeChangedOneByOneAndNamedAsTheyWereGiven()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        Maui(directory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var broken = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Management.Web", "wwwroot", ".well-known"));
        var whole = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Operations.Web", "wwwroot", ".well-known"));
        File.WriteAllText(Path.Combine(broken, WellKnownFiles.AssetLinks), "[ not json");
        var empty = Directory.CreateDirectory(Path.Combine(directory, "public", ".well-known")).FullName;

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android"), host.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(whole, WellKnownFiles.AssetLinks)), "com.contoso.shop");
        Assert.AreEqual("[ not json", File.ReadAllText(Path.Combine(broken, WellKnownFiles.AssetLinks)));
        StringAssert.Contains(host.FlatOutput, "src/Client/Shop.Client.Management.Web/wwwroot/.well-known/assetlinks.json isn't the JSON bit expects, so bit left it as it is.");

        Assert.AreEqual(0, await RunInAsync(host, directory, "keys", "android", "-o", Path.Combine(host.Root, "second.keystore"), "--well-known", empty), host.Output);

        StringAssert.Contains(host.FlatOutput, "bit found none in what --well-known names, so the app's Android App Links won't verify.");
    }

    [TestMethod]
    public async Task Pipeline_Should_TakeTheProjectItIsGivenWhateverPathGitHasForItsRepository()
    {
        using var host = Repository();
        var repository = Path.Combine(host.Root, "the-path-git-names");
        host.Runner.On("git", "rev-parse --show-toplevel", 0, repository);
        Write(Path.Combine(repository, ".github", "workflows", "cd.yml"), "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.ANDROID_RELEASE_KEYSTORE_FILE_BASE64 }}\n");
        var directory = Shop(host);
        Maui(directory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var wellKnown = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Web", "wwwroot", ".well-known"));

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes", "--project", directory), host.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(wellKnown, WellKnownFiles.AssetLinks)), "com.contoso.shop", host.Output);
        Assert.IsFalse(host.Output.Contains("found no bit Boilerplate project", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task AProjectFolderThatIsntThere_Should_StopTheCommandInsteadOfTakingAProjectAboveIt()
    {
        using var host = Repository();
        var directory = Shop(host);
        Maui(directory, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var wellKnown = WellKnown(Path.Combine(directory, "src", "Client", "Shop.Client.Web", "wwwroot", ".well-known"));
        var typo = Path.Combine(directory, "typo");

        foreach (var command in new string[][] { ["keys", "android"], ["keys", "certificate"], ["migration", "add", "Initial"], ["api", "check"], ["pipeline", "--yes"], ["run"], ["doctor"] })
        {
            Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, [.. command, "--project", typo]), $"{string.Join(' ', command)}: {host.Output}");
        }

        Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(wellKnown, WellKnownFiles.AssetLinks)));
        Assert.IsFalse(Directory.Exists(Path.Combine(host.Home, ".bitplatform", "keys")), "No key is made for a project the command wasn't given.");
        Assert.IsEmpty(host.Runner.Calls);
        StringAssert.Contains(host.FlatOutput, "isn't there.");
    }

    [TestMethod]
    public async Task Pipeline_Should_TakeTheProjectItRunsInBeforeTheOneAtTheRepositorysRoot()
    {
        using var host = Repository();
        AndroidWorkflow(host);
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "Root.slnx"), "<Solution />");
        Maui(host.WorkingDirectory, "src/Client/Root.Client.Maui/Root.Client.Maui.csproj", "com.contoso.root");
        var ofRoot = WellKnown(Path.Combine(host.WorkingDirectory, "src", "Client", "Root.Client.Web", "wwwroot", ".well-known"));
        var shop = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "apps", "Shop")).FullName;
        File.WriteAllText(Path.Combine(shop, "Shop.slnx"), "<Solution />");
        Maui(shop, "src/Client/Shop.Client.Maui/Shop.Client.Maui.csproj");
        var ofShop = WellKnown(Path.Combine(shop, "src", "Client", "Shop.Client.Web", "wwwroot", ".well-known"));

        Assert.AreEqual(0, await RunInAsync(host, Directory.CreateDirectory(Path.Combine(shop, "src", "Server")).FullName, "pipeline", "--yes"), host.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(ofShop, WellKnownFiles.AssetLinks)), "com.contoso.shop", host.Output);
        Assert.AreEqual(AssetLinks, File.ReadAllText(Path.Combine(ofRoot, WellKnownFiles.AssetLinks)));

        using var atRoot = Repository();
        AndroidWorkflow(atRoot);
        File.WriteAllText(Path.Combine(atRoot.WorkingDirectory, "Root.slnx"), "<Solution />");
        Maui(atRoot.WorkingDirectory, "src/Client/Root.Client.Maui/Root.Client.Maui.csproj", "com.contoso.root");
        var itsOwn = WellKnown(Path.Combine(atRoot.WorkingDirectory, "src", "Client", "Root.Client.Web", "wwwroot", ".well-known"));

        Assert.AreEqual(0, await RunInAsync(atRoot, Directory.CreateDirectory(Path.Combine(atRoot.WorkingDirectory, "docs")).FullName, "pipeline", "--yes"), atRoot.Output);

        StringAssert.Contains(File.ReadAllText(Path.Combine(itsOwn, WellKnownFiles.AssetLinks)), "com.contoso.root", atRoot.Output);
    }

    [TestMethod]
    public async Task Pipeline_Should_SayItFoundNoProjectToAddTheFingerprintTo()
    {
        using var host = Repository();
        AndroidWorkflow(host);
        host.Runner.On("gh", "variable get APP_ID", 0, "com.contoso.shop\n");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Didn't add the key's fingerprint to assetlinks.json");
        StringAssert.Contains(host.FlatOutput, $"bit found no bit Boilerplate project in {host.WorkingDirectory}, and --project names its folder.");
    }

    [TestMethod]
    public async Task Pipeline_Should_FindTheServersSettingsWhereServerApiIs()
    {
        using var host = Repository();
        Write(Path.Combine(host.WorkingDirectory, ".github", "workflows", "cd.yml"), "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }}\n        env:\n          SECRETS: ${{ toJSON(secrets) }}\n");
        var project = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "apps", "Shop")).FullName;
        File.WriteAllText(Path.Combine(project, "Shop.slnx"), "<Solution />");
        var backend = Write(Path.Combine(project, "src", "Backend", "Shop.Backend.csproj"), "<Project />");
        Write(Path.Combine(project, "src", "Backend", "appsettings.json"), """{ "AdsPushAPNS": { "P8PrivateKey": null } }""");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "DATABASE_PASSWORD\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check"), host.Output);
        Assert.IsFalse(host.Output.Contains(PipelineSetup.ApnsSettings, StringComparison.Ordinal), host.Output);

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check", "--project", project), host.Output);
        StringAssert.Contains(host.FlatOutput, "src/Server/Shop.Server.Api/appsettings.json isn't there, and --server-api names Server.Api's .csproj.");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check", "--project", project, "--server-api", backend), host.Output);
        StringAssert.Contains(host.FlatOutput, PipelineSetup.ApnsSettings);
        Assert.AreEqual(project, host.Runner.Calls.Last(c => c.FileName is "git").WorkingDirectory);
    }

    [TestMethod]
    public async Task Api_Should_TakeTheProjectItsServerApiItsDocumentsAndItsVersion()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var backend = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        Write(Path.Combine(directory, "src", "Backend", "appsettings.json"), "{}");
        var documents = Path.Combine(directory, "docs", "api");
        Write(Path.Combine(documents, "current.json"), """{"openapi":"3.1.2","paths":{"/api/v1/Todo/Get":{}}}""");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, "api", "check", "--no-build"), host.Output);
        StringAssert.Contains(host.FlatOutput, "or name it with --project.");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, "api", "check", "--no-build", "--project", directory), host.Output);
        StringAssert.Contains(host.FlatOutput, "src/Server/Shop.Server.Api/Shop.Server.Api.csproj isn't there, and its api is what bit api checks. Pass Server.Api's .csproj with --server-api.");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, "api", "check", "--no-build", "--project", directory, "--server-api", "src/Backend"), host.Output);
        StringAssert.Contains(host.FlatOutput, "or pass the version with --version.");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, host.Root, "api", "check", "--no-build", "--project", directory, "--server-api", "src/Backend", "--version", "2.1.0"), host.Output);
        StringAssert.Contains(host.FlatOutput, "pass that folder with --openapi.");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, host.Root, "api", "release", "--project", directory, "--server-api", "src/Backend", "--version", "2.1.0", "--openapi", "docs/api"), host.Output);

        StringAssert.Contains(host.FlatOutput, "Shop's api at version 2.1.0");
        StringAssert.Contains(host.FlatOutput, "Built Shop.Backend, which wrote docs/api/current.json");
        CollectionAssert.AreEqual(new[] { "build", backend }, host.Runner.Calls.Single(c => c.FileName is "dotnet").Arguments.ToArray());
        Assert.IsTrue(File.Exists(Path.Combine(documents, "2.1.0.json")), host.Output);
    }

    [TestMethod]
    public async Task Migration_Should_AddToTheServerApiItIsGiven()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var backend = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, directory, "migration", "add", "AddProductRating"), host.Output);
        StringAssert.Contains(host.FlatOutput, "Shop has no src/Server/Shop.Server.Api folder and none of its .cs files declares the AppDbContext class");
        StringAssert.Contains(host.FlatOutput, "Pass that project's .csproj with --server-api.");

        Assert.AreEqual(CliApp.ExitOk, await RunInAsync(host, host.Root, "migration", "add", "AddProductRating", "--project", Path.Combine(directory, "src"), "--server-api", backend), host.Output);

        var call = host.Runner.Calls.First(c => c.FileName is "dotnet");
        Assert.AreEqual(Path.GetDirectoryName(backend), call.WorkingDirectory);
        CollectionAssert.AreEqual(new[] { "dnx", "dotnet-ef", "--", "migrations", "add", "AddProductRating", "--output-dir", "Infrastructure/Data/Migrations" }, call.Arguments.Take(8).ToArray());
    }

    [TestMethod]
    public async Task Run_Should_TakeTheProjectAndSayWhichServerItDidntFind()
    {
        using var host = new TestHost(HostOs.Windows);
        var directory = Shop(host);
        File.WriteAllText(Path.Combine(directory, "Shop.Web.slnf"), "{}");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", "--project", Path.Combine(host.Root, "nowhere")), host.Output);
        StringAssert.Contains(host.FlatOutput, "or name it with --project.");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", "--project", directory, "--server-web", "src/Server/Shop.Server.Operations.Web"), host.Output);
        StringAssert.Contains(host.FlatOutput, "--server-web:");
        StringAssert.Contains(host.FlatOutput, "isn't there.");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("run", "--project", directory), host.Output);
        StringAssert.Contains(host.FlatOutput, "Didn't start the project");
        StringAssert.Contains(host.FlatOutput, "src/Server/Shop.Server.Web isn't there");
        StringAssert.Contains(host.FlatOutput, "--server-web names the server's .csproj when it's somewhere else.");
        Assert.AreEqual(directory, host.Runner.Calls.Single(c => c.FileName is "dotnet" && c.Arguments[0] is "build").WorkingDirectory);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("run", "windows", "--project", directory), host.Output);
        StringAssert.Contains(host.FlatOutput, "--windows names its .csproj when it's somewhere else.");
    }

    [TestMethod]
    public async Task DotnetRun_Should_StartEveryServerWebItIsGivenAndOpenTheFirst()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.OnAsync("dotnet", "run", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new ProcessResult { ExitCode = 0 };
        });
        var directory = Shop(host);
        var operations = ServerWeb(directory, "Shop.Server.Operations.Web", "http://localhost:5030");
        var management = ServerWeb(directory, "Shop.Server.Management.Web", "http://localhost:5040");
        var project = new ProjectContext { Name = "Shop", Directory = directory, Platforms = new HashSet<Platform> { Platform.Web }, Parts = new ProjectParts { ServerWeb = [operations, management] } };
        var steps = new ProjectSteps(host.Services, project) { AliveProbe = (_, _) => Task.FromResult(true) };

        var result = await steps.DotnetRunAsync(_ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("http://localhost:5030", steps.Running!.AppUrl);
        var runs = host.Runner.Calls.Where(c => c.FileName is "dotnet").ToList();
        CollectionAssert.AreEqual(new[] { operations, management }, runs.Select(c => c.Arguments[2]).ToArray(), "The folder of a project an option names may hold more than one .csproj.");
        Assert.IsFalse(runs.Any(c => c.Arguments.Contains("--no-build")), "A server an option names may be outside the web solution filter bit built, so dotnet run builds it.");

        await steps.Running.StopAsync(false);
    }

    [TestMethod]
    public async Task AspireStart_Should_ReadTheServersResourceFromTheAppHost()
    {
        using var host = new TestHost(HostOs.Linux);
        host.Runner.Executables["aspire"] = "aspire";
        host.Runner.On("aspire", "start", 0, """{ "dashboardUrl": "https://localhost:2030/login?t=abc" }""");
        host.Runner.On("aspire", "describe operations", 0, """{"resources":[{"name":"operations","urls":[{"name":"http","url":"http://localhost:5030","isInternal":false}]}]}""");
        var directory = Shop(host);
        File.WriteAllText(Path.Combine(directory, "Shop.Web.slnf"), "{}");
        var operations = ServerWeb(directory, "Shop.Server.Operations.Web", "http://localhost:1");
        var appHost = Write(Path.Combine(directory, "hosting", "Host.csproj"), """<Project><ItemGroup><ProjectReference Include="..\src\Backend\Shop.Backend.csproj" /></ItemGroup></Project>""");
        Write(Path.Combine(directory, "hosting", "Program.cs"), """
            var operations = builder.AddProject("operations", "../src/Server/Shop.Server.Operations.Web/Shop.Server.Operations.Web.csproj");
            var api = builder.AddProject<Projects.Shop_Backend>("backend");
            """);
        var project = new ProjectContext
        {
            Name = "Shop",
            Directory = directory,
            Platforms = new HashSet<Platform> { Platform.Web },
            Parts = new ProjectParts { ServerWeb = [operations], AppHost = appHost, ServerApi = Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj") }
        };
        var steps = new ProjectSteps(host.Services, project);

        var result = await steps.AspireStartAsync(keepRunning: true, _ => { }, CancellationToken.None);

        Assert.AreEqual(StepStatus.Succeeded, result.Status, result.Detail);
        Assert.AreEqual("http://localhost:5030", steps.Running!.AppUrl);
        CollectionAssert.AreEqual(new[] { "wait operations", "wait backend" }, host.Runner.Calls.Where(c => c.Arguments[0] is "wait").Select(c => string.Join(' ', c.Arguments.Take(2))).ToArray());
        Assert.AreEqual(Path.GetDirectoryName(appHost), host.Runner.Calls.First(c => c.Arguments[0] is "start").Arguments[2]);
    }

    [TestMethod]
    public void TheServersResource_Should_BeTheOneOfItsProjectOrElseTheTemplatesName()
    {
        const string web = "/shop/src/Server/Shop.Server.Web/Shop.Server.Web.csproj";

        Assert.AreEqual("web", ProjectSteps.AspireResource("""builder.AddProject( "web" , @"..\Shop.Server.Web\Shop.Server.Web.csproj")""", web, "serverweb"));
        Assert.AreEqual("web", ProjectSteps.AspireResource("""builder.AddProject<Projects.Shop_Server_Web>("web")""", web, "serverweb"));
        Assert.AreEqual("serverweb", ProjectSteps.AspireResource("""builder.AddProject("serverweb", path)""", web, "serverweb"));
        Assert.IsNull(ProjectSteps.AspireResource("""builder.AddProject("api", "../Shop.Server.Api/Shop.Server.Api.csproj")""", web, "serverweb"));
        Assert.IsNull(ProjectSteps.AspireResource("""builder.AddProject("serverweb", "../Other/Other.csproj")""", web, null), "A server an option names isn't another app's serverweb.");

        using var host = new TestHost();
        var appHost = Path.Combine(host.Root, "src", "Server", "Shop.Server.AppHost");
        var management = Path.Combine(host.Root, "src", "Management", "Web", "Web.csproj");
        const string two = """
            builder.AddProject("operations", "../../Operations/Web/Web.csproj");
            builder.AddProject("management", "../../Management/Web/Web.csproj");
            """;

        Assert.AreEqual("management", ProjectSteps.AspireResource(two, management, null, appHost), "Two projects may share a file name.");
        Assert.IsNull(ProjectSteps.AspireResource("""builder.AddProject("operations", "../../Operations/Web/Web.csproj");""", management, null, appHost), "A project the AppHost doesn't add has no resource, whatever its file is called.");

        const string typed = """builder.AddProject<Projects.Web>("operations");""";
        Write(Path.Combine(appHost, "Shop.Server.AppHost.csproj"), """<Project><ItemGroup><ProjectReference Include="..\..\Operations\Web\Web.csproj" /></ItemGroup></Project>""");

        Assert.AreEqual("operations", ProjectSteps.AspireResource(typed, Path.Combine(host.Root, "src", "Operations", "Web", "Web.csproj"), null, appHost));
        Assert.IsNull(ProjectSteps.AspireResource(typed, management, null, appHost), "The type is the one of the project the AppHost references, not of another with its name.");
    }

    [TestMethod]
    public async Task Doctor_Should_SayWhichPartsItDidntFindAndTakeThem()
    {
        using var host = new TestHost(HostOs.Linux);
        host.AllToolsInstalled();
        var directory = Shop(host);
        Write(Path.Combine(directory, "src", "Client", "Shop.Client.Shared", "package.json"), """{ "engines": { "node": ">=99.0.0" } }""");
        var web = Write(Path.Combine(directory, "apps", "Web", "Web.csproj"), "<Project />");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("doctor", "--project", directory), host.Output);

        StringAssert.Contains(host.FlatOutput, "bit doesn't know which Node.js Shop needs: src/Client/Shop.Client.Core/package.json isn't there. --client-core names Client.Core's folder.");
        StringAssert.Contains(host.FlatOutput, "it found no Blazor WebAssembly project under src/Client. --web names its .csproj.");

        var before = host.Output.Length;

        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("doctor", "--project", directory, "--client-core", "src/Client/Shop.Client.Shared", "--web", web), host.Output);

        var output = host.Output[before..];
        Assert.IsFalse(output.Contains("--client-core", StringComparison.Ordinal) || output.Contains("--web", StringComparison.Ordinal), output);
        StringAssert.Contains(output, "too old");
    }

    [TestMethod]
    public async Task Setup_Should_SayWhichPartsItDidntFindAndTakeThem()
    {
        using var host = new TestHost(HostOs.Linux);
        var directory = Shop(host);
        Write(Path.Combine(directory, "src", "Client", "Shop.Client.Shared", "package.json"), "{}");
        var web = Write(Path.Combine(directory, "src", "Client", "Shop.Client.Operations.Web", "Shop.Client.Operations.Web.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><WasmBuildNative>true</WasmBuildNative></PropertyGroup></Project>");
        var backend = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project />");
        string[] quiet = ["--yes", "--no-tools", "--no-workloads", "--no-restore", "--no-build", "--no-browsers", "--no-run", "--no-trust", "--no-open"];

        Assert.IsTrue(ProjectContext.At(directory, "Shop").UsesNativeWebAssembly, "A Blazor WebAssembly project under src/Client is a web app, whatever its name.");
        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync(["setup", directory, .. quiet]), host.Output);

        StringAssert.Contains(host.FlatOutput, "--client-core names Client.Core's folder.");
        StringAssert.Contains(host.FlatOutput, "bit didn't look at the migrations of Shop's database: src/Server/Shop.Server.Api isn't there. --server-api names Server.Api's .csproj.");
        Assert.IsFalse(host.Output.Contains("--web names", StringComparison.Ordinal), host.Output);

        var before = host.Output.Length;

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync(["setup", directory, .. quiet, "--client-core", "src/Client/Shop.Client.Shared", "--server-api", backend, "--web", web]), host.Output);

        Assert.IsFalse(host.Output[before..].Contains(" names ", StringComparison.Ordinal), host.Output[before..]);
    }

    [TestMethod]
    public async Task Rename_Should_CheckTheAppIdOfTheAppsProjectItIsGiven()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var maui = Maui(directory, "apps/Mobile/Mobile.csproj", "com.contoso.shop");
        host.Runner.On("git", "rev-parse", 128, "fatal: not a git repository");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("rename", "_Store", directory, "--yes", "--maui", maui), host.Output);

        StringAssert.Contains(host.FlatOutput, "The rename would give the native apps the app id com.contoso._store");
        StringAssert.Contains(host.FlatOutput, "set <ApplicationId> in apps/Mobile/Mobile.csproj first");
    }

    [TestMethod]
    public async Task AnOptionThatNamesAPart_Should_TakeAFileOrItsFolderFromTheCurrentFolderOrTheProjects()
    {
        using var host = new TestHost();
        var directory = Shop(host);
        var backend = Write(Path.Combine(directory, "src", "Backend", "Shop.Backend.csproj"), "<Project />");
        Directory.CreateDirectory(Path.Combine(directory, "empty"));
        var options = new ProjectOptions();
        var command = new Command("test");
        options.AddTo(command, options.ServerApi, options.ClientCore, options.WellKnown);

        (ProjectParts Parts, string? Error) Read(params string[] args) => options.Read(command.Parse(args), host.Services, directory);

        Assert.AreEqual(backend, Read("--server-api", backend).Parts.ServerApi);
        Assert.AreEqual(backend, Read("--server-api", Path.Combine("src", "Backend")).Parts.ServerApi, "A folder stands for the .csproj in it, and a relative path is the project's when the current folder has none.");
        Assert.IsTrue(Read().Parts.IsEmpty);
        Assert.IsNull(Read().Error);

        StringAssert.Contains(Read("--server-api", "nowhere.csproj").Error, "--server-api:");
        StringAssert.Contains(Read("--server-api", "empty").Error, "--server-api takes a .csproj");
        StringAssert.Contains(Read("--server-api", Path.Combine(directory, "Shop.slnx")).Error, "--server-api takes a .csproj or its folder");

        var tests = Write(Path.Combine(directory, "src", "Web", "Shop.Web.Tests.csproj"), "<Project />");
        Write(Path.Combine(directory, "src", "Web", "Shop.Web.csproj"), "<Project />");
        StringAssert.Contains(Read("--server-api", Path.Combine("src", "Web")).Error, "has 2 of them: name the one you mean");
        Assert.AreEqual(tests, Read("--server-api", tests).Parts.ServerApi);
        Assert.AreEqual(Write(Path.Combine(directory, "src", "Web", "Web.csproj"), "<Project />"), Read("--server-api", Path.Combine("src", "Web")).Parts.ServerApi, "The one named after the folder is the folder's project.");
        StringAssert.Contains(Read("--client-core", backend).Error, "--client-core takes a folder");
        Assert.HasCount(2, Read("--well-known", "empty", "--well-known", Path.Combine("src", "Backend")).Parts.WellKnown);
        Assert.HasCount(1, Read("--well-known", "empty", "--well-known", "empty").Parts.WellKnown);

        var cased = Directory.CreateDirectory(Path.Combine(directory, "cased")).FullName;

        if (Directory.Exists(Path.Combine(directory, "Cased")))
        {
            CollectionAssert.AreEqual(new[] { cased }, Read("--well-known", "cased", "--well-known", "Cased", "--well-known", Path.Combine(Path.GetDirectoryName(directory)!, "SHOP", "CASED")).Parts.WellKnown.ToArray(), "On a file system that doesn't tell them apart, it is one folder, named as it is on the disk.");
        }
        else
        {
            Directory.CreateDirectory(Path.Combine(directory, "Cased"));
            Assert.HasCount(2, Read("--well-known", "cased", "--well-known", "Cased").Parts.WellKnown, "On a file system that tells them apart, they are two folders.");
        }

        Assert.AreEqual(CliApp.ExitUsage, await RunInAsync(host, directory, "keys", "certificate", "--server-api", "nowhere.csproj"));
        StringAssert.Contains(host.FlatOutput, "nowhere.csproj isn't there.");
    }

    private static string Shop(TestHost host)
    {
        var directory = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Shop")).FullName;
        File.WriteAllText(Path.Combine(directory, "Shop.slnx"), "<Solution />");
        return directory;
    }

    private static string Maui(string directory, string path, string appId = "com.contoso.shop") => Write(Path.Combine(directory, path), $"<Project><PropertyGroup><ApplicationId>{appId}</ApplicationId></PropertyGroup></Project>");

    private static string WellKnown(string folder)
    {
        Write(Path.Combine(folder, WellKnownFiles.AssetLinks), AssetLinks);
        Write(Path.Combine(folder, WellKnownFiles.AppleAppSiteAssociation), AppleAppSiteAssociation);
        return folder;
    }

    private static string ServerWeb(string directory, string name, string url)
    {
        var folder = Path.Combine(directory, "src", "Server", name);
        Write(Path.Combine(folder, "Properties", "launchSettings.json"), $$"""{ "profiles": { "{{name}}": { "commandName": "Project", "applicationUrl": "{{url}}" } } }""");
        return Write(Path.Combine(folder, $"{name}.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
    }

    private static string Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private static TestHost Repository()
    {
        var host = new TestHost();
        host.Runner.Executables["gh"] = "gh";
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse --show-toplevel", 0, host.WorkingDirectory);
        host.Runner.On("gh", "repo view", 0, "contoso/shop\n");
        host.Runner.On("gh", "secret list", 0, "");
        return host;
    }

    private static void AndroidWorkflow(TestHost host) => Write(Path.Combine(host.WorkingDirectory, ".github", "workflows", "cd.yml"), "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.ANDROID_RELEASE_KEYSTORE_FILE_BASE64 }}\n");

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
        var root = CliApp.BuildRootCommand(() => services);

        return CliApp.InvokeAsync(root, root.Parse(args), services, args, new InvocationConfiguration { EnableDefaultExceptionHandler = false, Output = new StringWriter(), Error = new StringWriter() });
    }
}
