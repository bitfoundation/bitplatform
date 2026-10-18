using System.Collections.Generic;

namespace Bit.BlazorUI.Build;

/// <summary>One component stylesheet of a package, compiled on its own to one piece of its bundles (styles.json).</summary>
public sealed class StyleChunk
{
    public StyleChunk(PackageAssets package, string name, string file)
    {
        Package = package;
        Name = name;
        File = file;
    }

    public PackageAssets Package { get; }

    /// <summary>The stylesheet's file name without its extension, which is what a csproj names to keep it.</summary>
    public string Name { get; }

    /// <summary>The compiled piece, relative to the package's assets folder.</summary>
    public string File { get; }

    /// <summary>The types whose presence keeps it: the component of the same name, unless the stylesheet says otherwise.</summary>
    public List<string> Owners { get; } = [];

    /// <summary>Kept in every trimmed bundle.</summary>
    public bool Always { get; set; }

    public string Id => $"{Package.PackageId}:{Name}";

    public override string ToString() => Id;
}
