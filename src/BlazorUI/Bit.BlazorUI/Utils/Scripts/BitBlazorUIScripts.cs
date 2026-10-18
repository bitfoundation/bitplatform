namespace Bit.BlazorUI;

/// <summary>
/// How the bit BlazorUI packages load their JavaScript: from the bundles an app references with a <c>&lt;script&gt;</c> tag
/// each (<c>bit.blazorui.js</c>, <c>bit.blazorui.extras.js</c>, <c>bit.blazorui.legacy.js</c>) - the default - or as
/// lazy scripts, one module per script imported the first time something calls into it.
/// </summary>
/// <remarks>
/// The preferred way to turn the lazy scripts on is the <c>&lt;BitBlazorUILazyScripts&gt;true&lt;/BitBlazorUILazyScripts&gt;</c>
/// MSBuild property of the app's project, which also keeps the bundles out of its static web assets and turns the mode on
/// at runtime; <see cref="UseLazyScripts"/> and <see cref="UseBundledScripts"/> are for a host where the property cannot be
/// applied, and decide only how the scripts are loaded, never what the app publishes. Both are process-wide toggles meant
/// to be set once at startup, before any component renders, and shared by every circuit of a Blazor Server app.
/// <br />
/// In lazy mode no <c>&lt;script&gt;</c> tag of the packages is needed: the scripts that act on the page without being called
/// (the theme, the claim of the Escape key, the keys every component relies on) are imported as the app starts by the
/// packages' JavaScript initializers, and every other script the first time a component calls into it.
/// </remarks>
public static class BitBlazorUIScripts
{
    /// <summary>The AppContext switch the <c>BitBlazorUILazyScripts</c> MSBuild property sets.</summary>
    internal const string LazyScriptsSwitchName = "Bit.BlazorUI.LazyScripts";

    /// <summary>Where the packages' static web assets are served from by default, relative to the app's base href.</summary>
    internal const string DefaultContentPath = "./_content/";

    // null = not overridden at runtime, so the build-time switch decides.
    private static volatile object? _lazyScriptsOverride;
    private static volatile string _contentPath = DefaultContentPath;

    /// <summary>
    /// The build-time half of the switch: <c>&lt;BitBlazorUILazyScripts&gt;true&lt;/BitBlazorUILazyScripts&gt;</c> in the app's
    /// project ends up here as an <see cref="AppContext"/> switch. A feature switch, so a trimmed publish can treat it as a
    /// constant; the runtime override is what keeps <see cref="UseLazyScripts"/> working there regardless.
    /// </summary>
#if NET9_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.FeatureSwitchDefinition(LazyScriptsSwitchName)]
#endif
    internal static bool LazyScriptsFeature => AppContext.TryGetSwitch(LazyScriptsSwitchName, out var enabled) && enabled;

    // Read once: AppContext.TryGetSwitch takes a lock, and the mode is asked for on every interop call of the packages,
    // in bundle mode too, which must not pay for a feature it does not use. The switch comes from the host's runtime
    // configuration, which is in place before any code of this assembly runs.
    private static readonly bool LazyScriptsFeatureValue = LazyScriptsFeature;

    /// <summary>
    /// True when the packages import their scripts one module at a time, on first use, rather than expecting their bundles
    /// to have been loaded by <c>&lt;script&gt;</c> tags: decided by the <c>BitBlazorUILazyScripts</c> MSBuild property unless
    /// <see cref="UseLazyScripts"/> or <see cref="UseBundledScripts"/> said otherwise. A host page serving both modes reads
    /// it to decide whether to render the tags.
    /// </summary>
    public static bool LazyScriptsEnabled => _lazyScriptsOverride is bool overridden ? overridden : LazyScriptsFeatureValue;

    /// <summary>Where the packages' static web assets are served from, each package's folder under it; see <see cref="UseLazyScripts"/>.</summary>
    public static string ContentPath => _contentPath;

    /// <summary>
    /// Imports the packages' scripts one module at a time, the first time something calls into one, instead of expecting
    /// their bundles to have been loaded: an app downloads the scripts of the components a page actually uses, and needs no
    /// <c>&lt;script&gt;</c> tag of the packages at all. The <c>BitBlazorUILazyScripts</c> MSBuild property is the better
    /// switch wherever the project can say it: it also keeps the bundles out of the published output.
    /// </summary>
    /// <param name="contentPath">
    /// Where the packages' static web assets are served from when it is not the default <c>./_content/</c> (a CDN, a base
    /// path of its own): the folder holding <c>Bit.BlazorUI/</c>, <c>Bit.BlazorUI.Extras/</c> and the rest. A root-relative
    /// path (<c>/cdn/</c>) or an absolute URL is taken as given; anything else is relative to the base href, so the leading
    /// <c>./</c> a dynamic <c>import()</c> needs and a trailing slash are added where they are missing.
    /// </param>
    public static void UseLazyScripts(string? contentPath = null)
    {
        _lazyScriptsOverride = true;

        if (string.IsNullOrWhiteSpace(contentPath) is false) SetContentPath(contentPath);
    }

    /// <summary>
    /// Expects the packages' bundles to be loaded by the app's <c>&lt;script&gt;</c> tags and never imports a module of them -
    /// the default. It decides the loading at runtime only: a publish of an app whose project set
    /// <c>BitBlazorUILazyScripts</c> left the bundles out, and calling this cannot bring them back.
    /// </summary>
    public static void UseBundledScripts()
    {
        _lazyScriptsOverride = false;
    }

    /// <summary>
    /// Sets where the packages' static web assets are served from without changing the mode - for an app whose project
    /// turned the lazy scripts on but which serves the assets from somewhere else.
    /// </summary>
    internal static void SetContentPath(string contentPath)
    {
        var path = contentPath.Trim();

        // A specifier that is not a URL and does not start with /, ./ or ../ is a bare module specifier to the browser,
        // resolved through the page's import map rather than against its base URI, and fails without an entry there.
        var isUrl = path[0] == '/'
                 || path.StartsWith("./", StringComparison.Ordinal)
                 || path.StartsWith("../", StringComparison.Ordinal)
                 || path.IndexOf("://", StringComparison.Ordinal) > 0;

        if (isUrl is false) path = "./" + path;

        _contentPath = path.EndsWith('/') ? path : path + "/";
    }
}
