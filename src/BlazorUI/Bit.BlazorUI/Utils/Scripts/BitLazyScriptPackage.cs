using System.Reflection;
using System.Text.Json;

namespace Bit.BlazorUI;

/// <summary>
/// The lazy scripts of one package (see <see cref="BitBlazorUIScripts"/>): which module exports what, read off the
/// <c>bit-lazy-scripts.json</c> resource that Bit.BlazorUI.Build/build-assets.mjs writes and the package embeds.
/// </summary>
internal sealed class BitLazyScriptPackage
{
    /// <summary>The resource every package with scripts embeds (Bit.BlazorUI.Assets.targets).</summary>
    internal const string ResourceName = "bit-lazy-scripts.json";

    /// <summary>The resource's shape this code reads; a newer one is a package newer than the core it runs with.</summary>
    private const int Version = 1;

    private readonly Dictionary<string, List<BitLazyScriptModule>> _exports = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitLazyScriptModule> _byName = new(StringComparer.Ordinal);

    private BitLazyScriptPackage(string id, string? boot)
    {
        Id = id;
        Boot = boot;
    }

    /// <summary>The package's id, which is the folder of its static web assets under <c>_content</c>.</summary>
    public string Id { get; }

    /// <summary>
    /// The module importing the scripts the package starts with (under its wwwroot), which its JavaScript initializer
    /// imports as the app starts - null for a package with none.
    /// </summary>
    public string? Boot { get; }

    public List<BitLazyScriptModule> Modules { get; } = [];

    /// <summary>The modules exporting exactly this path under the namespace ("Utils.isRtl", "Callouts").</summary>
    public List<BitLazyScriptModule>? FindExport(string path) => _exports.TryGetValue(path, out var modules) ? modules : null;

    /// <summary>The modules exporting a member of this path - every one adding to a namespace that several files add to.</summary>
    public IEnumerable<BitLazyScriptModule> FindUnder(string path)
    {
        var prefix = path + ".";
        return _exports.Where(e => e.Key.StartsWith(prefix, StringComparison.Ordinal)).SelectMany(e => e.Value);
    }

    public BitLazyScriptModule? FindModule(string name) => _byName.TryGetValue(name, out var module) ? module : null;

    /// <summary>The lazy scripts the assembly embeds, or null for an assembly with none.</summary>
    public static BitLazyScriptPackage? Load(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null) return null;

        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        if (root.GetProperty("version").GetInt32() > Version)
        {
            throw new InvalidOperationException($"The lazy scripts of {assembly.GetName().Name} are of version {root.GetProperty("version").GetInt32()}, newer than the {Version} this Bit.BlazorUI reads: reference the same version of every bit BlazorUI package.");
        }

        var package = new BitLazyScriptPackage(root.GetProperty("package").GetString()!,
                                               root.TryGetProperty("boot", out var boot) && boot.ValueKind == JsonValueKind.String ? boot.GetString() : null);

        foreach (var item in root.GetProperty("modules").EnumerateArray())
        {
            var module = new BitLazyScriptModule(package,
                                                 item.GetProperty("name").GetString()!,
                                                 item.GetProperty("path").GetString()!,
                                                 Strings(item, "deps"),
                                                 Strings(item, "externals"),
                                                 Strings(item, "teardown"),
                                                 item.TryGetProperty("boot", out var boots) && boots.ValueKind == JsonValueKind.True);

            package.Modules.Add(module);
            package._byName[module.Name] = module;

            foreach (var export in Strings(item, "exports"))
            {
                if (package._exports.TryGetValue(export, out var list) is false) package._exports[export] = list = [];
                list.Add(module);
            }
        }

        return package;
    }

    private static string[] Strings(JsonElement item, string name)
        => item.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? [.. array.EnumerateArray().Select(e => e.GetString()!)]
            : [];
}
