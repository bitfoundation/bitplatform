using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Build;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Trimming;

/// <summary>
/// Pins what the lazy scripts (BitBlazorUILazyScripts) rest on: each module is its script's piece behind the imports of
/// what it uses, the boot module is the one the publish writes again for a trimmed app, every assembly embeds the map the
/// .NET side imports by, every script a component owns and that acts as it loads is loaded by that component, and no code
/// of the packages calls into a script around the loader - a call that would find its module never imported.
/// </summary>
[TestClass]
public sealed partial class BitLazyScriptsContractTests
{
    private static readonly string[] Packages = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy"];

    private static readonly Lazy<List<PackageAssets>> Assets = new(() => Packages.Select(p => PackageAssets.Load(SourceFiles.GetPath(p, "obj", "bit-assets"))).ToList());

    [TestMethod]
    public void EveryModuleShouldBeItsPieceBehindTheImportsOfWhatItUses()
    {
        var problems = new List<string>();
        foreach (var package in Assets.Value)
        {
            var order = package.ScriptModules.Select(m => m.Name).ToList();
            foreach (var module in package.ScriptModules)
            {
                Assert.IsNotNull(module.ModulePath, $"{module.Id} has no module of the lazy scripts.");

                // Its own package's scripts, in the bundle's order; the guard that leaves it out where the package's bundle
                // ran already; the very piece the bundle is made of.
                var expected = string.Concat(module.Dependencies.OrderBy(order.IndexOf).Select(d => $"import \"./{d}.js\";\n"))
                             + "var BitBlazorUI = window.BitBlazorUI || (window.BitBlazorUI = {});\n"
                             + $"if (!BitBlazorUI.$bundles || !BitBlazorUI.$bundles[\"{package.PackageId}\"]) {{\n"
                             + File.ReadAllText(package.Resolve(module.File), Encoding.UTF8)
                             + "}\n";

                var actual = File.ReadAllText(SourceFiles.GetPath(package.PackageId, "wwwroot", module.ModulePath!), Encoding.UTF8);
                if (actual != expected) problems.Add(module.Id);
            }
        }

        Assert.AreEqual(0, problems.Count, $"These modules are not their pieces behind the imports of what they use: {string.Join(", ", problems)}");
    }

    [TestMethod]
    public void EveryBundleShouldSayItRanSoNoModuleRunsAgainBesideIt()
    {
        foreach (var package in Assets.Value)
        {
            StringAssert.EndsWith(package.ScriptPrologue, $"(BitBlazorUI.$bundles || (BitBlazorUI.$bundles = {{}}))[\"{package.PackageId}\"] = true;\n", package.PackageId);
        }
    }

    [TestMethod]
    public void TheBootModuleShouldBeWhatAPublishWritesAgainForEveryScriptItBoots()
    {
        foreach (var package in Assets.Value)
        {
            var booting = package.ScriptModules.Where(m => m.Boot).ToList();
            if (booting.Count == 0)
            {
                Assert.IsNull(package.ScriptBoot, $"{package.PackageId} boots nothing, yet names a boot module.");
                continue;
            }

            // What it boots: every script acting on the page without being called, and nothing called.
            CollectionAssert.AreEquivalent(package.ScriptModules.Where(m => m.Always || m.Markup.Count > 0).ToList(), booting, package.PackageId);

            Assert.AreEqual(AssetComposer.BootModuleText(package.ScriptBoot!, booting),
                            File.ReadAllText(SourceFiles.GetPath(package.PackageId, "wwwroot", package.ScriptBoot!), Encoding.UTF8),
                            $"{package.PackageId}'s boot module is not what a trimmed publish writes for every script it boots.");

            var initializer = File.ReadAllText(SourceFiles.GetPath(package.PackageId, "wwwroot", package.ScriptInitializer!), Encoding.UTF8);
            StringAssert.Contains(initializer, $"import(\"./{Path.GetFileName(package.ScriptBoot!)}\")");
            StringAssert.Contains(initializer, $"window.BitBlazorUI.$bundles[\"{package.PackageId}\"] ? undefined", "The initializer boots beside the package's bundle too.");
            Assert.AreEqual($"{package.PackageId}.lib.module.js", Path.GetFileName(package.ScriptInitializer), "Blazor imports an initializer by the package's name only.");
        }
    }

    [TestMethod]
    public void EveryAssemblyShouldEmbedTheLazyScriptsOfItsBuild()
    {
        foreach (var package in Packages)
        {
            using var stream = Assembly.Load(package).GetManifestResourceStream("bit-lazy-scripts.json");
            Assert.IsNotNull(stream, $"{package} embeds no lazy scripts.");

            using var reader = new StreamReader(stream);
            Assert.AreEqual(File.ReadAllText(SourceFiles.GetPath(package, "obj", "bit-assets", "lazy-scripts.json")), reader.ReadToEnd(), $"{package} embeds lazy scripts other than its build's.");
        }
    }

    [TestMethod]
    public void EveryScriptAComponentOwnsThatActsAsItLoadsShouldBeLoadedByIt()
    {
        using var graph = LibraryGraph.Load(Packages.Select(p => Assembly.Load(p).Location));

        // Kept for its component and running as it loads - a listener on the document acting on the component's markup -
        // such a script is called by nothing: in lazy mode the component loads it itself, by its package and name.
        var problems = new List<string>();
        foreach (var module in Assets.Value.SelectMany(p => p.ScriptModules).Where(m => m.Loads && m.Boot is false && m.Owners.Count > 0))
        {
            foreach (var owner in module.Owners.Where(o => o.EndsWith('*') is false))
            {
                var types = graph.FindByName(owner).ToList();
                var nested = graph.Assemblies.SelectMany(a => a.Types).Where(t => types.Any(o => t.FullName.StartsWith(o.FullName + "/", StringComparison.Ordinal)));
                var strings = LibraryGraph.StringsOf(types.Concat(nested)).ToHashSet(StringComparer.Ordinal);

                if (strings.Contains(module.Name) is false || strings.Contains(module.Package.PackageId) is false)
                {
                    problems.Add($"{owner} does not load {module.Id} (EnsureScriptAsync(\"{module.Package.PackageId}\", \"{module.Name}\"))");
                }
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join("; ", problems));
    }

    [TestMethod]
    public void NoCodeOfThePackagesShouldCallAScriptAroundTheLoader()
    {
        // IJSRuntime.InvokeAsync and InvokeVoidAsync take the identifier straight to the page, which in lazy mode has not
        // imported its module yet: the packages call through InvokeVoid / Invoke / FastInvoke or InvokeScript(Void)Async.
        var problems = new List<string>();
        foreach (var package in Packages)
        {
            foreach (var file in Directory.EnumerateFiles(SourceFiles.GetDirectory(package), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                if (Path.GetFileName(file) is "IJSRuntimeExtensions.cs" or "IJSRuntimeFastExtensions.cs") continue;

                foreach (Match match in DirectCall().Matches(File.ReadAllText(file)))
                {
                    problems.Add($"{Path.GetRelativePath(SourceFiles.Root, file)}: {match.Value.Trim()}");
                }
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public void EveryTeardownShouldBeAFunctionThePackagesCallFromDotNet()
    {
        // A teardown no identifier of the packages names is a comment out of date with the code calling it.
        var identifiers = Packages.SelectMany(p => ReadUserStrings(Assembly.Load(p).Location)).Where(s => s.StartsWith("BitBlazorUI.", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);

        var unused = Assets.Value.SelectMany(p => p.ScriptModules)
                           .SelectMany(m => m.Teardown.Select(t => (Module: m, Teardown: t)))
                           .Where(x => identifiers.Contains($"BitBlazorUI.{x.Teardown}") is false)
                           // BitMap's providers are called through identifiers put together at run time.
                           .Where(x => x.Module.Owners.Any(o => o.EndsWith("MapProvider", StringComparison.Ordinal)) is false)
                           .Select(x => $"{x.Module.Id}: {x.Teardown}")
                           .ToList();

        Assert.AreEqual(0, unused.Count, $"No .NET code calls these teardowns: {string.Join(", ", unused)}");
    }

    private static IEnumerable<string> ReadUserStrings(string path)
    {
        using var pe = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(path));
        return AppScan.ReadUserStrings(pe.GetMetadataReader()).ToList();
    }

    [GeneratedRegex(@"\.(InvokeVoidAsync|InvokeAsync(<(?:[^<>]|<[^<>]*>)*>)?)\(\s*\$?""BitBlazorUI\.")]
    private static partial Regex DirectCall();
}
