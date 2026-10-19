using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Gives the endpoints of the trimmed bundles back the label an app's lookups go through.
/// </summary>
/// <remarks>
/// A fingerprinted route carries a "label" endpoint property holding the name without the fingerprint, and that label is
/// what <c>@Assets["_content/Bit.BlazorUI/styles/bit.blazorui.css"]</c> and an import map resolve through. The SDK
/// writes it as the asset's relative path, bare ("styles/bit.blazorui.css"), because the trimmed bundles are defined in
/// the app's own project; the packages' own endpoints reach an app through a reference instead, which rebases route and
/// label together and so carries the base path in both. Left alone, the lookups would stop resolving to the trimmed
/// bundles. Run at the end of the publish pipeline, after the compressed copies - each with a label of its own, the
/// bundle's plus ".br" or ".gz" - have been derived from them.
/// </remarks>
public sealed class RelabelBitBlazorUIEndpoints : Task
{
    private static readonly Regex FingerprintPattern = new(@"#\[[^\]]*\]\??", RegexOptions.CultureInvariant);

    /// <summary>Every endpoint of the publish.</summary>
    public ITaskItem[] Endpoints { get; set; } = [];

    /// <summary>The trimmed bundles' assets.</summary>
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>The endpoints of the routes whose labels change - every endpoint of each such route, to be removed.</summary>
    [Output]
    public ITaskItem[] Replaced { get; set; } = [];

    /// <summary>The same endpoints with their labels restored, to be added back.</summary>
    [Output]
    public ITaskItem[] Relabelled { get; set; } = [];

    public override bool Execute()
    {
        var fixes = new List<(string Bare, string Labelled, string Route, string AssetFile)>();
        foreach (var asset in Assets)
        {
            var basePath = asset.GetMetadata("BasePath").Trim('/');
            if (basePath.Length == 0) continue;

            var relative = FingerprintPattern.Replace(asset.GetMetadata("RelativePath"), string.Empty).Replace('\\', '/').TrimStart('/');
            fixes.Add((relative, $"{basePath}/{relative}", $"{basePath}/{relative}", asset.GetMetadata("FullPath")));
        }

        var routes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var endpoint in Endpoints)
        {
            var properties = endpoint.GetMetadata("EndpointProperties");
            foreach (var fix in fixes)
            {
                if (endpoint.ItemSpec.StartsWith(fix.Route.Substring(0, fix.Route.Length - fix.Bare.Length), StringComparison.Ordinal)
                    && properties.Contains(LabelOf(fix.Bare)))
                {
                    routes.Add(endpoint.ItemSpec);
                }
            }
        }

        var replaced = Endpoints.Where(e => routes.Contains(e.ItemSpec)).ToList();
        var relabelled = new List<ITaskItem>();
        foreach (var endpoint in replaced)
        {
            var copy = new TaskItem(endpoint);
            var properties = endpoint.GetMetadata("EndpointProperties");
            foreach (var fix in fixes) properties = properties.Replace(LabelOf(fix.Bare), LabelOf(fix.Labelled));
            copy.SetMetadata("EndpointProperties", properties);
            relabelled.Add(copy);
        }

        // Should an SDK ever write the label differently, the replacement above does nothing and the lookups quietly stop
        // resolving to the trimmed bundles. So the outcome is what is checked: a bundle served from a route besides its
        // plain one - fingerprinting is on, and a label is what reaches it - has an endpoint answering to that label.
        var all = Endpoints.Where(e => routes.Contains(e.ItemSpec) is false).Concat(relabelled).ToList();
        foreach (var fix in fixes)
        {
            var fingerprinted = all.Any(e => string.Equals(e.GetMetadata("AssetFile"), fix.AssetFile, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.ItemSpec, fix.Route, StringComparison.Ordinal) is false);
            var labelled = all.Any(e => e.GetMetadata("EndpointProperties").Contains(LabelOf(fix.Labelled) + "\""));

            if (fingerprinted && labelled is false)
            {
                Log.LogError("Bit.BlazorUI: the trimmed {0} is served from a fingerprinted route, but no endpoint carries the '{0}' label, so @Assets[\"{0}\"] would no longer resolve to it. Set BitBlazorUITrimAssets to false to publish the full bundles instead.", fix.Labelled);
            }
        }

        Replaced = [.. replaced];
        Relabelled = [.. relabelled];
        return Log.HasLoggedErrors is false;
    }

    // A prefix match on the value, so that "x.css", "x.css.br" and "x.css.gz" are all covered by one replacement.
    private static string LabelOf(string value) => $"\"Name\":\"label\",\"Value\":\"{value}";
}
