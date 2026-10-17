using System.Collections.Generic;

namespace Bit.BlazorUI.Build;

/// <summary>One TypeScript file of a package, compiled to one piece of its script bundle (scripts.json).</summary>
public sealed class ScriptModule
{
    public ScriptModule(PackageAssets package, string name, string file)
    {
        Package = package;
        Name = name;
        File = file;
    }

    public PackageAssets Package { get; }

    /// <summary>The file's name without its extension, which is what a csproj names to keep it.</summary>
    public string Name { get; }

    /// <summary>The compiled piece, relative to the package's assets folder.</summary>
    public string File { get; }

    /// <summary>What it adds to the BitBlazorUI namespace, by path under it ("TextField", "Utils.isRtl").</summary>
    public List<string> Exports { get; } = [];

    /// <summary>The scripts of the same package it uses.</summary>
    public List<string> Dependencies { get; } = [];

    /// <summary>What it uses of another package's namespace, by path - resolved against that package's exports.</summary>
    public List<string> Externals { get; } = [];

    /// <summary>The types whose presence keeps it: it acts on markup they render, which no interop call names.</summary>
    public List<string> Owners { get; } = [];

    /// <summary>
    /// Texts whose presence in the code an app can run, or in its static markup, keeps it: it acts on what markup says
    /// (a data- attribute, an event name) - which a component or the app's own markup can say alike.
    /// </summary>
    public List<string> Markup { get; } = [];

    /// <summary>
    /// The listeners it adds to the page as it loads, in the order it adds them - its top level's, and those of the
    /// functions it calls there - each as its target, its event and whether it captures ("window keydown capture").
    /// </summary>
    public List<string> Listeners { get; } = [];

    /// <summary>Kept in every trimmed bundle.</summary>
    public bool Always { get; set; }

    public string Id => $"{Package.PackageId}:{Name}";

    public override string ToString() => Id;
}
