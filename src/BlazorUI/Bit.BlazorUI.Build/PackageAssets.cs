using System;
using System.Collections.Generic;
using System.IO;

namespace Bit.BlazorUI.Build;

/// <summary>
/// What one bit BlazorUI package ships its scripts and stylesheets as: the folder build-assets.mjs wrote (obj/bit-assets
/// in the source tree, buildTransitive/bit-assets in the package), read from its scripts.json and styles.json.
/// </summary>
public sealed class PackageAssets
{
    /// <summary>The manifests this code reads. A higher version is a package newer than its task, which says so.</summary>
    public const int ManifestVersion = 1;

    private PackageAssets(string packageId, string directory)
    {
        PackageId = packageId;
        Directory = directory;
    }

    /// <summary>The package's id, which is also the SourceId of its static web assets.</summary>
    public string PackageId { get; }

    public string Directory { get; }

    /// <summary>The script bundle's path under wwwroot, when the package has one.</summary>
    public string? ScriptBundle { get; private set; }

    /// <summary>What goes at the head of a script bundle, ahead of every module ("use strict").</summary>
    public string ScriptPrologue { get; private set; } = string.Empty;

    /// <summary>The modules in bundle order, the order they load in.</summary>
    public List<ScriptModule> ScriptModules { get; } = [];

    public List<StyleBundle> StyleBundles { get; } = [];

    /// <summary>The component chunks in bundle order, which is the cascade order and so the only order they can go in.</summary>
    public List<StyleChunk> StyleChunks { get; } = [];

    public List<IconSet> IconSets { get; } = [];

    public string Resolve(string relativePath) => Path.Combine(Directory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public static PackageAssets Load(string directory)
    {
        var scriptsPath = Path.Combine(directory, "scripts.json");
        var stylesPath = Path.Combine(directory, "styles.json");
        if (File.Exists(scriptsPath) is false && File.Exists(stylesPath) is false)
        {
            throw new FileNotFoundException($"Neither scripts.json nor styles.json is in '{directory}'.", scriptsPath);
        }

        string? packageId = null;
        var scripts = File.Exists(scriptsPath) ? ReadManifest(scriptsPath, ref packageId) : null;
        var styles = File.Exists(stylesPath) ? ReadManifest(stylesPath, ref packageId) : null;

        var package = new PackageAssets(packageId!, directory);

        if (scripts is not null)
        {
            package.ScriptBundle = Json.GetString(scripts, "bundle", scriptsPath);
            package.ScriptPrologue = Json.GetOptionalString(scripts, "prologue") ?? string.Empty;

            foreach (var item in Json.AsArray(scripts.TryGetValue("modules", out var m) ? m : null, scriptsPath))
            {
                var obj = Json.AsObject(item, scriptsPath);
                var module = new ScriptModule(package, Json.GetString(obj, "name", scriptsPath), Json.GetString(obj, "file", scriptsPath))
                {
                    Always = Json.GetBool(obj, "always"),
                };
                module.Exports.AddRange(Json.GetStrings(obj, "exports"));
                module.Dependencies.AddRange(Json.GetStrings(obj, "deps"));
                module.Externals.AddRange(Json.GetStrings(obj, "externals"));
                module.Owners.AddRange(Json.GetStrings(obj, "owners"));
                module.Markup.AddRange(Json.GetStrings(obj, "markup"));
                module.Listeners.AddRange(Json.GetStrings(obj, "listeners"));
                package.ScriptModules.Add(module);
            }
        }

        if (styles is not null)
        {
            foreach (var item in Json.AsArray(styles.TryGetValue("bundles", out var b) ? b : null, stylesPath))
            {
                var obj = Json.AsObject(item, stylesPath);
                package.StyleBundles.Add(new StyleBundle(Json.GetString(obj, "bundle", stylesPath), Json.GetString(obj, "prefix", stylesPath), Json.GetString(obj, "suffix", stylesPath)));
            }

            foreach (var item in Json.AsArray(styles.TryGetValue("chunks", out var c) ? c : null, stylesPath))
            {
                var obj = Json.AsObject(item, stylesPath);
                var chunk = new StyleChunk(package, Json.GetString(obj, "name", stylesPath), Json.GetString(obj, "file", stylesPath))
                {
                    Always = Json.GetBool(obj, "always"),
                };
                chunk.Owners.AddRange(Json.GetStrings(obj, "owners"));
                package.StyleChunks.Add(chunk);
            }

            if (styles.TryGetValue("icons", out var icons) && icons is List<object?> iconSets)
            {
                foreach (var item in iconSets)
                {
                    var obj = Json.AsObject(item, stylesPath);
                    package.IconSets.Add(new IconSet(Json.GetString(obj, "bundle", stylesPath), Json.GetString(obj, "file", stylesPath), Json.GetString(obj, "classPrefix", stylesPath)));
                }
            }
        }

        return package;
    }

    private static Dictionary<string, object?> ReadManifest(string path, ref string? packageId)
    {
        var obj = Json.AsObject(Json.Parse(File.ReadAllText(path)), path);

        var version = (int)Json.GetNumber(obj, "version", path);
        if (version > ManifestVersion)
        {
            throw new FormatException($"{path} is of manifest version {version}, newer than the {ManifestVersion} this task reads: the package is newer than the Bit.BlazorUI.Build task loaded for it.");
        }

        var id = Json.GetString(obj, "package", path);
        if (packageId is not null && string.Equals(packageId, id, StringComparison.Ordinal) is false)
        {
            throw new FormatException($"{path} is of the package '{id}', but the folder's other manifest is of '{packageId}'.");
        }

        packageId = id;
        return obj;
    }
}
