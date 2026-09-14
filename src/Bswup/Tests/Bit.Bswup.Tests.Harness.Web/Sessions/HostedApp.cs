using System.Text.Json.Nodes;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// The two apps the host serves, each with the service-worker settings and script-tag options its README
/// setup calls for. Tests only state what differs from these.
/// </summary>
public sealed class HostedApp
{
    /// <summary>The Blazor Web App at the root: the host document is App.razor, the manifest comes from the client project.</summary>
    public static HostedApp Root { get; } = new("root", string.Empty);

    /// <summary>The standalone WebAssembly app under /standalone/: index.html, blazor.webassembly.js, its own manifest.</summary>
    public static HostedApp Standalone { get; } = new("standalone", "/standalone");

    public static IReadOnlyList<HostedApp> All { get; } = [Root, Standalone];

    private HostedApp(string name, string pathBase)
    {
        Name = name;
        PathBase = pathBase;
    }

    public string Name { get; }

    /// <summary>Empty for the root app, "/standalone" otherwise.</summary>
    public string PathBase { get; }

    /// <summary>The web-root relative folder of the app's files: "" or "standalone/".</summary>
    public string WebRootFolder => PathBase.Length == 0 ? string.Empty : PathBase.TrimStart('/') + "/";

    public JsonObject DefaultWorkerSettings(string mode)
    {
        var settings = new JsonObject
        {
            ["caseInsensitiveUrl"] = true,
            // The control endpoints must never be answered from a cache; /api/ is the app's own server API.
            ["serverHandledUrls"] = new JsonArray(Regex(@"\/api\/"), Regex(@"\/_harness\/")),
        };

        if (this == Standalone)
        {
            // index.html is in the manifest and is the default defaultUrl, so nothing else is needed for the
            // shell. harness-version.txt stands in for a hash-less external asset of a real deployment.
            settings["externalAssets"] = new JsonArray(new JsonObject { ["url"] = "harness-version.txt" });
            return settings;
        }

        // What a Blazor Web App needs on top of its client's manifest (compare the FullSample): the document at
        // "/", blazor.web.js (the host's, never in the client manifest), its fingerprinted name when the page
        // references it through @Assets, and the resource collection .NET 10 names per build.
        settings["defaultUrl"] = "/";
        settings["noPrerenderQuery"] = "no-prerender=true";
        settings["externalAssets"] = new JsonArray(
            new JsonObject { ["url"] = "/" },
            new JsonObject { ["url"] = "_framework/blazor.web.js" },
            new JsonObject { ["url"] = Regex(@"\/_framework\/blazor\.web\.[^\/]+\.js$") },
            new JsonObject { ["url"] = Regex(@"\/_framework\/resource-collection\.[^\/]*\.js$") },
            new JsonObject { ["url"] = "harness-version.txt" });

        if (HarnessRenderModes.NeedsServerForNavigations(mode))
        {
            settings["forcePrerender"] = true;
        }

        return settings;
    }

    public Dictionary<string, string> DefaultScriptAttributes(HarnessSessionOptions options)
    {
        if (this == Standalone)
        {
            // The documented setting for a sub-path app; the scope fallback has a test of its own.
            return new() { ["scope"] = "/standalone/" };
        }

        // Without BswupProgress there is no built-in bitBswupHandler: record the messages at least.
        return options.Progress.Placement == "none"
            ? new() { ["handler"] = "harnessBswupHandler" }
            : [];
    }

    public Dictionary<string, string> ScriptAttributes(HarnessSessionOptions options)
    {
        var attributes = DefaultScriptAttributes(options);

        foreach (var (name, value) in options.ScriptAttributes ?? [])
        {
            if (value is null) attributes.Remove(name);
            else attributes[name] = value;
        }

        return attributes;
    }

    public static JsonObject Regex(string source, string flags = "") => new() { ["$regex"] = source, ["flags"] = flags };
}
