using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Build;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Trimming;

/// <summary>
/// Pins what the publish-time trimming of the scripts and stylesheets rests on: that the pieces the packages' bundles are
/// made of (obj/bit-assets, written by Bit.BlazorUI.Build/build-assets.mjs) are named by what the code of the packages
/// can reach. A script no interop identifier, no type and no markup keeps is dropped from every app that needs it, and a
/// component whose classes live in a stylesheet it does not bring is drawn unstyled - both only in a trimmed publish, long
/// after the change that caused it, so they are caught here, against the compiled assemblies.
/// </summary>
[TestClass]
public sealed class BitAssetTrimmingContractTests
{
    private static readonly string[] Packages = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy"];

    private static readonly Lazy<List<PackageAssets>> Assets = new(() => Packages.Select(LoadAssets).ToList());

    private static readonly Lazy<Dictionary<string, List<string>>> StringsByAssembly = new(() =>
        Packages.ToDictionary(p => p, p => ReadUserStrings(Assembly.Load(p).Location).ToList()));

    [TestMethod]
    public void EveryInteropIdentifierOfThePackagesShouldNameAScript()
    {
        var selection = new AssetSelection(Assets.Value);

        // An identifier only a script that no longer exports it would answer - a renamed function, a typo - keeps nothing,
        // and is a "BitBlazorUI.x is undefined" in a trimmed app as much as in an untrimmed one.
        var unresolved = StringsByAssembly.Value
                                          .SelectMany(p => p.Value.Where(s => AssetSelection.Identifier.IsMatch(s)).Select(s => (Package: p.Key, Identifier: s)))
                                          .Where(x => selection.ResolveIdentifier(x.Identifier).Count == 0)
                                          .Select(x => $"{x.Package}: {x.Identifier}")
                                          .Distinct()
                                          .ToList();

        Assert.AreEqual(0, unresolved.Count, $"These interop identifiers name no function a script exports: {string.Join(", ", unresolved)}");
    }

    [TestMethod]
    public void EveryTypeAScriptOrAStylesheetIsKeptForShouldBeOneOfThePackages()
    {
        using var graph = LoadGraph();
        var names = new HashSet<string>(graph.Assemblies.SelectMany(a => a.Types).Select(t => t.Name), StringComparer.Ordinal);

        // An owner the packages have no type of is a script or a stylesheet no publish keeps: renamed with the component,
        // or misspelled in the bit-trim comment that names it (or, for a stylesheet, in its file name).
        var missing = Assets.Value.SelectMany(p => p.ScriptModules.SelectMany(m => m.Owners.Select(o => (Piece: m.Id, Owner: o))))
                            .Concat(Assets.Value.SelectMany(p => p.StyleChunks.SelectMany(c => c.Owners.Select(o => (Piece: c.Id, Owner: o)))))
                            .Where(x => AssetSelection.IsPresent(x.Owner, names) is false)
                            .Select(x => $"{x.Piece} (owner {x.Owner})")
                            .ToList();

        Assert.AreEqual(0, missing.Count, $"These pieces are kept for types the packages do not have: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void WhatAScriptUsesOfAnotherPackageShouldBeExportedThere()
    {
        var selection = new AssetSelection(Assets.Value);

        foreach (var module in Assets.Value.SelectMany(p => p.ScriptModules))
        {
            selection.KeepScript(module);
        }

        Assert.AreEqual(0, selection.UnresolvedIdentifiers.Count,
            $"Scripts use members of another package's namespace that no package exports: {string.Join(", ", selection.UnresolvedIdentifiers)}");
    }

    [TestMethod]
    public void EveryScriptShouldBeKeptForSomething()
    {
        using var graph = LoadGraph();
        var identifiers = StringsByAssembly.Value.SelectMany(p => p.Value).Where(s => AssetSelection.Identifier.IsMatch(s)).ToList();
        var selection = new AssetSelection(Assets.Value);

        // Everything a script can be kept for: an identifier of the packages naming it, a type, a markup text, always -
        // and, through all of those, being used by a script that is kept.
        var everything = new HashSet<string>(graph.Assemblies.SelectMany(a => a.Types).Select(t => t.Name), StringComparer.Ordinal);
        selection.Select(everything, identifiers.Concat(Assets.Value.SelectMany(p => p.ScriptModules).SelectMany(m => m.Markup)).ToList());

        // A file with types alone emits nothing, which is no script to keep.
        var never = Assets.Value.SelectMany(p => p.ScriptModules)
                          .Where(m => selection.Scripts.Contains(m) is false)
                          .Where(m => File.ReadAllText(m.Package.Resolve(m.File)).Trim().Length > 0)
                          .Select(m => m.Id)
                          .ToList();

        Assert.AreEqual(0, never.Count, $"No identifier, type or markup of the packages keeps these scripts, so no trimmed publish has them: {string.Join(", ", never)}");
    }

    [TestMethod]
    public void TheMarkupAScriptActsOnShouldBeWhatTheComponentsRender()
    {
        var strings = StringsByAssembly.Value.SelectMany(p => p.Value).ToList();

        // A script none of whose texts a compiled component carries is kept for an app's own markup alone, which is not
        // what it was written for: the components' markup changed under it. (A text of its own may still be the app's
        // alone - no component listens for a transition event, an app may - so one text of each is what is pinned.)
        var unseen = Assets.Value.SelectMany(p => p.ScriptModules)
                           .Where(m => m.Markup.Count > 0)
                           .Where(m => m.Markup.Any(marker => strings.Any(s => s.Contains(marker, StringComparison.Ordinal))) is false)
                           .Select(m => $"{m.Id} ({string.Join(", ", m.Markup)})")
                           .ToList();

        Assert.AreEqual(0, unseen.Count, $"No component renders the markup these scripts are kept for: {string.Join(", ", unseen)}");
    }

    [TestMethod]
    public void EveryComponentShouldBringTheStylesheetsOfTheClassesItRenders()
    {
        using var graph = LoadGraph();

        var chunks = Assets.Value.SelectMany(p => p.StyleChunks).ToList();
        var basePrefixes = new HashSet<string>(Assets.Value.SelectMany(p => p.StyleBundles)
                                                            .SelectMany(b => new[] { b.Prefix, b.Suffix }.Select(f => File.ReadAllText(Assets.Value.First(p => p.StyleBundles.Contains(b)).Resolve(f))))
                                                            .SelectMany(ClassPrefixes), StringComparer.Ordinal);

        // The prefix a component's stylesheet names its classes with (bit-btn for BitButton): the one most of its
        // class selectors start with. A class of that prefix rendered by another component is a class whose rules a bundle
        // trimmed down to that other component does not have.
        var prefixOwners = new Dictionary<string, List<StyleChunk>>(StringComparer.Ordinal);
        foreach (var chunk in chunks)
        {
            var prefix = ClassPrefixes(File.ReadAllText(chunk.Package.Resolve(chunk.File))).GroupBy(p => p).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
            if (prefix is null || basePrefixes.Contains(prefix)) continue;
            if (prefixOwners.TryGetValue(prefix, out var list) is false) prefixOwners[prefix] = list = [];
            list.Add(chunk);
        }

        var components = Packages.Select(Assembly.Load)
                                 .SelectMany(a => a.GetExportedTypes())
                                 .Where(t => t.IsAbstract is false && typeof(IComponent).IsAssignableFrom(t))
                                 .ToList();

        Assert.IsTrue(components.Count > 50, "Too few components were found to check.");

        var problems = new List<string>();
        foreach (var component in components)
        {
            var root = graph.FindByName(component.Name.Split('`')[0]).Where(n => n.Assembly.Name == component.Assembly.GetName().Name).ToList();
            if (root.Count == 0) continue;

            var reached = graph.Closure(root);
            var names = new HashSet<string>(reached.Select(t => t.Name), StringComparer.Ordinal);
            var kept = chunks.Where(c => c.Always || c.Owners.Any(o => AssetSelection.IsPresent(o, names))).ToHashSet();

            foreach (var prefix in LibraryGraph.StringsOf(reached).SelectMany(ClassPrefixes).Distinct())
            {
                if (prefixOwners.TryGetValue(prefix, out var owners) is false) continue;
                if (owners.Any(kept.Contains)) continue;

                problems.Add($"{component.Name} renders bit-{prefix}-* classes of {string.Join("/", owners.Select(o => o.Name))}");
            }
        }

        Assert.AreEqual(0, problems.Count, $"These components render classes whose stylesheet they do not bring into a trimmed bundle: {string.Join("; ", problems)}");
    }

    [TestMethod]
    [DataRow("BitText", "BitText")]
    [DataRow("BitButton", "BitButton")]
    [DataRow("BitTextField", "BitTextField")]
    [DataRow("BitIcon", "BitIcon")]
    public void ASimpleComponentShouldBringItsOwnStylesheetAlone(string component, string stylesheet)
    {
        using var graph = LoadGraph();

        // The closure follows every type a type names, so one hub type naming many components - a registry, a switch over
        // component types - would bring all of them into every app that renders any. These do not render another.
        var reached = graph.Closure(graph.FindByName(component));
        var names = new HashSet<string>(reached.Select(t => t.Name), StringComparer.Ordinal);
        var kept = Assets.Value.SelectMany(p => p.StyleChunks).Where(c => c.Always is false && c.Owners.Any(o => AssetSelection.IsPresent(o, names))).Select(c => c.Name).ToList();

        CollectionAssert.AreEquivalent(new[] { stylesheet }, kept, $"{component} brings the stylesheets of: {string.Join(", ", kept)}");
    }

    [TestMethod]
    public void EveryBundleShouldKeepTheScriptsItAlwaysNeedsAndNothingElseForAnAppOfNoComponents()
    {
        var selection = new AssetSelection(Assets.Value);
        selection.Select(new HashSet<string>(StringComparer.Ordinal), []);

        // The scripts every app pays for: what the version stamp and the keys every component relies on are in.
        CollectionAssert.AreEquivalent(new[] { "Bit.BlazorUI:general", "Bit.BlazorUI:Utils.Keys", "Bit.BlazorUI.Extras:general" },
                                       selection.Scripts.Select(m => m.Id).ToList(),
                                       "The scripts kept for an app that renders nothing changed.");

        Assert.AreEqual(0, selection.Styles.Count, $"Component stylesheets are kept for an app that renders nothing: {string.Join(", ", selection.Styles.Select(c => c.Id))}");
    }

    [TestMethod]
    [DataRow("BitBlazorUI.Utils.isRtl", "Bit.BlazorUI:Utils.Keys")]
    [DataRow("BitBlazorUI.Utils.setupFocusTrap", "Bit.BlazorUI:Utils.FocusTraps")]
    [DataRow("BitBlazorUI.Utils.uuidv4", "Bit.BlazorUI:general")]
    [DataRow("BitBlazorUI.Callouts.toggle", "Bit.BlazorUI:Callouts")]
    [DataRow("BitBlazorUI.TextField.setupMultilineInput", "Bit.BlazorUI:BitTextField")]
    [DataRow("BitBlazorUI.Legacy.Utils.initScripts", "Bit.BlazorUI.Legacy:Legacy")]
    [DataRow("BitBlazorUI.Extras.copyToClipboard", "Bit.BlazorUI.Extras:Extras")]
    public void AnIdentifierShouldNameTheScriptExportingIt(string identifier, string script)
    {
        // A function of a namespace several files add to is found by its own name; a member of a class by its class's.
        var selection = new AssetSelection(Assets.Value);
        CollectionAssert.AreEqual(new[] { script }, selection.ResolveIdentifier(identifier).Select(m => m.Id).ToList(), identifier);
    }

    [TestMethod]
    [DataRow("BitBlazorUI.Utils", "Bit.BlazorUI:Utils.FocusTraps")]
    [DataRow("BitBlazorUI.Utils", "Bit.BlazorUI:Utils.Escape")]
    [DataRow("BitBlazorUI.Callouts", "Bit.BlazorUI:Callouts")]
    [DataRow("BitBlazorUI.Callouts", "Bit.BlazorUI:Callouts.Registry")]
    public void AnIdentifierStoppingAtANamespaceShouldNameEveryScriptAddingToIt(string identifier, string script)
    {
        // An app's script holding the namespace itself (const u = BitBlazorUI.Utils; or a call made through brackets, whose
        // text stops there) can reach any member of it: a namespace is no export of its own, and resolving to nothing
        // would drop every script it calls.
        var selection = new AssetSelection(Assets.Value);
        CollectionAssert.Contains(selection.ResolveIdentifier(identifier).Select(m => m.Id).ToList(), script, identifier);
    }

    [TestMethod]
    [DataRow("window.fixtureDispose = id => BitBlazorUI.PageVisibility.dispose(id);", "BitBlazorUI.PageVisibility.dispose")]
    [DataRow("window.BitBlazorUI.PageVisibility.dispose(id);", "BitBlazorUI.PageVisibility.dispose")]
    [DataRow("globalThis.BitBlazorUI.Utils.isRtl(el)", "BitBlazorUI.Utils.isRtl")]
    [DataRow("BitBlazorUI.Theme?.onChange(handler)", "BitBlazorUI.Theme.onChange")]
    [DataRow("window.BitBlazorUI?.Callouts?.toggle()", "BitBlazorUI.Callouts.toggle")]
    [DataRow("const u = BitBlazorUI.Utils; u.setupFocusTrap(el);", "BitBlazorUI.Utils")]
    public void AnIdentifierInAnAppsOwnScriptShouldBeReadHoweverItIsReached(string text, string identifier)
    {
        // The plain form an interop call of .NET code writes, whether the script reached the namespace through a global or
        // through optional chaining.
        CollectionAssert.AreEqual(new[] { identifier }, AssetSelection.IdentifiersIn(text).ToList(), text);
    }

    [TestMethod]
    public void ANameEndingInTheNamespaceShouldNotBeReadAsAnIdentifier()
    {
        Assert.AreEqual(0, AssetSelection.IdentifiersIn("MyBitBlazorUI.Utils.isRtl(); $BitBlazorUI.x();").Count());
    }

    [TestMethod]
    public void ThePlatformsAssembliesShouldBeToldFromAnAppsOwnByTheirKey()
    {
        // The scan reads every assembly of the app but the platform's - an assembly naming only BitIconName consts, or an
        // interop identifier through a plain IJSRuntime, references no library assembly and still names what it uses.
        Assert.IsTrue(IsPlatformAssembly(typeof(object).Assembly.Location), "The core library");
        Assert.IsTrue(IsPlatformAssembly(typeof(IComponent).Assembly.Location), "ASP.NET Core");
        Assert.IsTrue(IsPlatformAssembly(typeof(Regex).Assembly.Location), "The base class library");
        Assert.IsFalse(IsPlatformAssembly(typeof(BitButton).Assembly.Location), "Bit.BlazorUI is signed with a key of its own.");
        Assert.IsFalse(IsPlatformAssembly(typeof(BitAssetTrimmingContractTests).Assembly.Location), "An unsigned assembly.");

        static bool IsPlatformAssembly(string path)
        {
            using var pe = new PEReader(File.OpenRead(path));
            return AppScan.IsPlatformAssembly(pe.GetMetadataReader());
        }
    }

    [TestMethod]
    public void AnOwnerEndingInAStarShouldStandForEveryTypeStartingWithIt()
    {
        var names = new HashSet<string>(["BitFcDayView", "BitButton"], StringComparer.Ordinal);

        Assert.IsTrue(AssetSelection.IsPresent("BitFc*", names));
        Assert.IsTrue(AssetSelection.IsPresent("BitButton", names));
        Assert.IsFalse(AssetSelection.IsPresent("BitFullCalendar", names));
        Assert.IsFalse(AssetSelection.IsPresent("BitBut", names), "A name without the star matches whole names only.");
    }

    [TestMethod]
    public void AGlyphNamedInALongTextShouldBeKept()
    {
        // A static html page or a large literal holds far more than one icon's name; the glyphs it names are kept all the same.
        var selection = new AssetSelection([]);
        var text = new string('x', 10_000) + " <i class=\"bit-icon bit-icon--Add\"></i> Delete " + new string('y', 10_000);

        selection.SelectIcons([text, ""], ["Add", "Delete", "Edit"], ["bit-icon--"]);

        CollectionAssert.AreEquivalent(new[] { "Add", "Delete" }, selection.Icons.ToList());
    }

    private static PackageAssets LoadAssets(string package)
    {
        var folder = SourceFiles.GetPath(package, "obj", "bit-assets");
        Assert.IsTrue(Directory.Exists(folder), $"Missing {folder}: build {package} first (its build writes the pieces this test reads).");
        return PackageAssets.Load(folder);
    }

    private static LibraryGraph LoadGraph() => LibraryGraph.Load(Packages.Select(p => Assembly.Load(p).Location));

    private static IEnumerable<string> ReadUserStrings(string path)
    {
        using var pe = new PEReader(File.OpenRead(path));
        return AppScan.ReadUserStrings(pe.GetMetadataReader()).ToList();
    }

    // The first segment of every class of the bit- family a text holds (btn for .bit-btn-pri), with the attributes of
    // the family (data-bit-esc) left out.
    private static IEnumerable<string> ClassPrefixes(string text)
        => Regex.Matches(text, @"(?<![\w-])bit-(?<prefix>[a-z0-9]+)(?=[-\s.:,{)\]""'>]|$)").Select(m => m.Groups["prefix"].Value);
}
