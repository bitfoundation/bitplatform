using System.Text.RegularExpressions;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The trimming as an app gets it from nuget.org: the packages packed from this source tree into a feed of their own, and
/// apps outside the repository referencing them - a Blazor WebAssembly app and a Blazor Web App's server - published with
/// each SDK the packages support (.NET 8, 9 and 10; the targets differ between 8 and 9). This is what checks the package
/// layout (buildTransitive\bit-assets, tasks\Bit.BlazorUI.Build.dll), the task loading from the package, and the SDK
/// paths a ProjectReference never takes.
/// </summary>
/// <remarks>
/// Opt-in (RUN_PACKAGE_TESTS=1): it packs four multi-targeted packages and publishes six apps, which restores the
/// WebAssembly toolchain of each SDK from nuget.org. The packages go into the machine's NuGet cache under a version of
/// their own (99.0.0-trimming.*), which <c>dotnet nuget locals global-packages --clear</c> takes back out.
/// </remarks>
[TestClass]
public sealed partial class PackageTrimmingTests
{
    private static readonly Lazy<(string Feed, string Version)> Packed = new(Pack);

    [TestMethod]
    [DataRow(8, "wasm")]
    [DataRow(9, "wasm")]
    [DataRow(10, "wasm")]
    [DataRow(8, "web")]
    [DataRow(9, "web")]
    [DataRow(10, "web")]
    [DataRow(8, "web-lazy")]
    [DataRow(9, "web-lazy")]
    [DataRow(10, "web-lazy")]
    public void AnAppReferencingThePackagesShouldPublishTrimmedBundles(int sdkMajor, string shape)
    {
        if (Environment.GetEnvironmentVariable("RUN_PACKAGE_TESTS") != "1") Assert.Inconclusive("Set RUN_PACKAGE_TESTS=1 to pack the packages and publish apps referencing them.");

        var sdk = InstalledSdks().Where(v => v.Major == sdkMajor).OrderByDescending(v => v).FirstOrDefault();
        if (sdk is null) Assert.Inconclusive($"No .NET {sdkMajor} SDK is installed.");

        var (feed, version) = Packed.Value;
        var app = WriteApp(sdk, shape, feed, version);

        var result = Publisher.Run("dotnet", ["publish", "-c", "Release", "-nologo", "-nodeReuse:false", "-o", Path.Combine(app, "publish")], app, TimeSpan.FromMinutes(20));
        Assert.AreEqual(0, result.ExitCode, $"The publish failed:\n{Tail(result.Output)}");

        var published = new PublishResult(Path.Combine(app, "publish"), result.ExitCode, result.Output);

        // The app renders BitButton and BitDropdown, and nothing of the Extras: only their stylesheets, and the scripts
        // they reach, are in the bundles the package's pieces are put back together into.
        if (shape == "web-lazy")
        {
            // The lazy scripts: the modules the app can reach and the initializers Blazor imports, no script bundle.
            CollectionAssert.Contains(published.PublishedModules("Bit.BlazorUI"), "Callouts");
            CollectionAssert.DoesNotContain(published.PublishedModules("Bit.BlazorUI"), "BitScrollablePane");
            Assert.IsFalse(published.HasAsset("Bit.BlazorUI", "scripts/bit.blazorui.js"), "A lazy app published the script bundle.");
            Assert.IsTrue(published.ListedInitializers().Any(i => i.StartsWith("_content/Bit.BlazorUI/scripts/Bit.BlazorUI.", StringComparison.Ordinal)),
                          $"A lazy app does not list the core's initializer: {string.Join(", ", published.ListedInitializers())}");
            CollectionAssert.IsSubsetOf(new[] { "BitButton", "BitDropdown" }, published.KeptStyles("Bit.BlazorUI"));
            return;
        }

        // A bundle-mode app has Blazor import none of the lazy scripts' initializers, on any SDK.
        Assert.IsFalse(published.ListedInitializers().Any(i => i.Contains("Bit.BlazorUI", StringComparison.Ordinal)),
                       $"A bundle-mode app lists an initializer of the lazy scripts: {string.Join(", ", published.ListedInitializers())}");
        Assert.AreEqual(0, published.PublishedModules("Bit.BlazorUI").Count, "A bundle-mode app published the modules of the lazy scripts.");

        var styles = published.KeptStyles("Bit.BlazorUI");
        CollectionAssert.IsSubsetOf(new[] { "BitButton", "BitDropdown" }, styles, string.Join(", ", styles));
        CollectionAssert.DoesNotContain(styles, "BitCard");
        CollectionAssert.Contains(published.KeptScripts("Bit.BlazorUI"), "Callouts");
        CollectionAssert.DoesNotContain(published.KeptScripts("Bit.BlazorUI"), "BitScrollablePane");
        Assert.AreEqual(0, published.KeptStyles("Bit.BlazorUI.Extras").Count(s => s != "BitErrorBoundary"));

        published.AssertScriptsAreThePiecesInOrder("Bit.BlazorUI");
        published.AssertStylesAreThePiecesInOrder("Bit.BlazorUI");

        StringAssert.Contains(result.Output, "Bit.BlazorUI: Bit.BlazorUI/styles/bit.blazorui.css -> ");
    }

    private static (string Feed, string Version) Pack()
    {
        var version = $"99.0.0-trimming.{DateTime.UtcNow:yyyyMMddHHmmss}";
        var feed = Path.Combine(Path.GetTempPath(), "bit-blazorui-trimming", "feed");
        if (Directory.Exists(feed)) Directory.Delete(feed, recursive: true);
        Directory.CreateDirectory(feed);

        // Built, then packed without building, the way a release is: a pack that builds a multi-targeted package on its own
        // can reach for an inner build's output before it is written.
        foreach (var package in new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.Icons" })
        {
            var project = Path.Combine(Publisher.Root, package, $"{package}.csproj");

            var build = Publisher.Run("dotnet", ["build", project, "-c", "Release", "-nologo", "-nodeReuse:false", $"-p:PackageVersion={version}", "-p:GeneratePackageOnBuild=false"], Publisher.Root, TimeSpan.FromMinutes(20));
            Assert.AreEqual(0, build.ExitCode, $"Building {package} failed:\n{Tail(build.Output)}");

            var pack = Publisher.Run("dotnet", ["pack", project, "-c", "Release", "-nologo", "-nodeReuse:false", "--no-build", "-o", feed, $"-p:PackageVersion={version}", "-p:EnablePackageValidation=false"], Publisher.Root, TimeSpan.FromMinutes(20));
            Assert.AreEqual(0, pack.ExitCode, $"Packing {package} failed:\n{Tail(pack.Output)}");
        }

        return (feed, version);
    }

    // An app outside the repository, so neither the repository's global.json nor its Directory.Build files reach it.
    private static string WriteApp(Version sdk, string shape, string feed, string version)
    {
        var tfm = $"net{sdk.Major}.0";
        var app = Path.Combine(Path.GetTempPath(), "bit-blazorui-trimming", $"app-{shape}-{sdk.Major}");
        if (Directory.Exists(app)) Directory.Delete(app, recursive: true);
        Directory.CreateDirectory(app);

        File.WriteAllText(Path.Combine(app, "global.json"), $$"""{ "sdk": { "version": "{{sdk}}", "rollForward": "disable" } }""");
        File.WriteAllText(Path.Combine(app, "nuget.config"), $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <!-- A key of its own: a source of the same name disabled in the machine's own NuGet.Config would stay disabled. -->
              <packageSources>
                <clear />
                <add key="bit-blazorui-trimming-feed" value="{{feed}}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);

        var packages = $"""
                <PackageReference Include="Bit.BlazorUI" Version="{version}" />
                <PackageReference Include="Bit.BlazorUI.Extras" Version="{version}" />
                <PackageReference Include="Bit.BlazorUI.Icons" Version="{version}" />
            """;

        if (shape == "wasm")
        {
            File.WriteAllText(Path.Combine(app, "App.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
                  <PropertyGroup>
                    <TargetFramework>{{tfm}}</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="{{sdk.Major}}.0.*" />
                {{packages}}
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(app, "Program.cs"), """
                using Bit.BlazorUI;
                using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                builder.RootComponents.Add<App.Main>("#app");
                builder.Services.AddBitBlazorUIServices();
                await builder.Build().RunAsync();
                """);
            Directory.CreateDirectory(Path.Combine(app, "wwwroot"));
            File.WriteAllText(Path.Combine(app, "wwwroot", "index.html"), """
                <!DOCTYPE html>
                <html><head><base href="/" /><link rel="stylesheet" href="_content/Bit.BlazorUI/styles/bit.blazorui.css" /></head>
                <body><div id="app"></div><script src="_framework/blazor.webassembly.js"></script><script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script></body></html>
                """);
        }
        else
        {
            File.WriteAllText(Path.Combine(app, "App.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>{{tfm}}</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <BitBlazorUITrimAssets>true</BitBlazorUITrimAssets>
                    <BitBlazorUILazyScripts>{{(shape == "web-lazy" ? "true" : "false")}}</BitBlazorUILazyScripts>
                  </PropertyGroup>
                  <ItemGroup>
                {{packages}}
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(app, "Program.cs"), """
                using Bit.BlazorUI;

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddRazorComponents();
                builder.Services.AddBitBlazorUIServices();
                var app = builder.Build();
                app.UseStaticFiles();
                app.UseAntiforgery();
                app.MapRazorComponents<App.Main>();
                app.Run();
                """);
        }

        File.WriteAllText(Path.Combine(app, "_Imports.razor"), "@using Microsoft.AspNetCore.Components.Web\n@using Bit.BlazorUI\n");
        File.WriteAllText(Path.Combine(app, "Main.razor"), """
            <BitButton>Save</BitButton>
            <BitDropdown TItem="BitDropdownItem<string>" TValue="string" Items="_items" />

            @code {
                private readonly List<BitDropdownItem<string>> _items = [new() { Text = "One", Value = "1" }];
            }
            """);

        return app;
    }

    private static List<Version> InstalledSdks()
    {
        var result = Publisher.Run("dotnet", ["--list-sdks"], Path.GetTempPath(), TimeSpan.FromMinutes(1));
        return SdkLine().Matches(result.Output).Select(m => Version.Parse(m.Groups["version"].Value)).ToList();
    }

    [GeneratedRegex(@"^(?<version>\d+\.\d+\.\d+)\s", RegexOptions.Multiline)]
    private static partial Regex SdkLine();

    private static string Tail(string text) => text.Length <= 6000 ? text : "..." + text[^6000..];
}
