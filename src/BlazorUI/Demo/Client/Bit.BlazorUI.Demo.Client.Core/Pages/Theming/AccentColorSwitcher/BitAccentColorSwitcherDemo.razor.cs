namespace Bit.BlazorUI.Demo.Client.Core.Pages.Theming.AccentColorSwitcher;

public partial class BitAccentColorSwitcherDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAccentColorSwitcherClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the switcher.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Config",
            Type = "BitAccentColorConfig?",
            DefaultValue = "null",
            Description = "The app-wide accent configuration: the accents offered as swatches, the stores the picked one is persisted to, and the first-paint strategy to maintain when applying it. When null, the BitAccentColorConfig registered in DI (the accentColor option of AddBitBlazorUIExtrasServices) is used; with neither, the DefaultAccents are offered, nothing is persisted and no first-paint machinery runs. The configuration is app-wide state on the shared BitAccentColorService - the first initialized instance (or an explicit BitAccentColorService.InitializeAsync call) fixes it - so state it once: register it in DI, or hand one shared instance to every switcher and to the host page's BitAccentColorHead.",
            LinkType = LinkType.Link,
            Href = "#accent-color-config",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback that is called when the accent color changes, receiving the applied accent color.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAccentColorSwitcherClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the switcher.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "accent-color-config",
            Title = "BitAccentColorConfig",
            Description = "The app-wide configuration of the accent color feature. The host page's BitAccentColorHead and every BitAccentColorSwitcher are the head and body halves of one mechanism, so they have to agree on these values - state them once, in code both the server and the client compile: either register the configuration in DI through the accentColor option of AddBitBlazorUIExtrasServices (in the service-registration method the server and client Program.cs share), which every component falls back to when no Config parameter is handed to it, or define one shared instance (e.g. a static field in a shared project) and pass that same instance to each of them.",
            Parameters =
            [
                new()
                {
                    Name = "Accents",
                    Type = "IReadOnlyList<BitAccentColorItem>?",
                    DefaultValue = "null",
                    Description = "The accent colors of the app: the swatches the switchers offer, the palettes the head emits, and the list a persisted value is validated against on restore. When null, the DefaultAccents (the six BitAccentColorPresets hues) are used.",
                },
                new()
                {
                    Name = "FirstPaintStrategy",
                    Type = "BitAccentColorFirstPaintStrategy",
                    DefaultValue = "BitAccentColorFirstPaintStrategy.None",
                    Description = "How the accent palette reaches the very first paint of a page load, before any Blazor runtime is up: None (the default) applies the accent after hydration only, StaticCss keys a prebuilt all-accents stylesheet on the bit-accent root attribute, StoredCss keeps a snapshot of the generated palette CSS in localStorage. The CSS strategies restore from the stores Persistence enables, so they need a persistence other than None to have any effect.",
                    LinkType = LinkType.Link,
                    Href = "#accent-color-first-paint-strategy-enum",
                },
                new()
                {
                    Name = "Persistence",
                    Type = "BitAccentColorPersistence",
                    DefaultValue = "BitAccentColorPersistence.None",
                    Description = "The stores the picked accent is persisted to: LocalStorage, Cookie, or both (All); None (the default) keeps the accent for the current session only. The cookie half is what lets the server read the preference while prerendering (SSR), so enable it when the server takes part in painting or seeding the accent.",
                    LinkType = LinkType.Link,
                    Href = "#accent-color-persistence-enum",
                },
            ]
        },
        new()
        {
            Id = "accent-color-item",
            Title = "BitAccentColorItem",
            Description = "An accent color offered by the BitAccentColorSwitcher.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The display name of the accent color, used as the swatch tooltip and accessible label.",
                },
                new()
                {
                    Name = "AriaLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The accessible label of the swatch button. When not set, an English label is composed from Name (\"Apply the {Name} accent color\") - set this to localize it.",
                },
                new()
                {
                    Name = "Color",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The accent color in #RGB or #RRGGBB hex format, fed to BitThemeFactory as the seed the whole palette is derived from.",
                },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitAccentColorSwitcherClassStyles",
            Description = "Custom CSS classes/styles for different parts of the BitAccentColorSwitcher.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAccentColorSwitcher.",
                },
                new()
                {
                    Name = "Swatch",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each swatch button of the BitAccentColorSwitcher.",
                },
                new()
                {
                    Name = "ActiveSwatch",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the swatch button of the active accent of the BitAccentColorSwitcher.",
                },
            ]
        },
        new()
        {
            Id = "accent-color-service",
            Title = "BitAccentColorService",
            Description = "The scoped service (registered by AddBitBlazorUIExtrasServices) that owns the accent: state, persistence, the theme overlay and the dark/light re-derivation. Inject it to seed, restore or apply the accent from app code.",
            Parameters =
            [
                new()
                {
                    Name = "ActiveAccent",
                    Type = "string",
                    DefaultValue = "BitAccentColorPresets.Blue",
                    Description = "The accent currently applied, as the canonical hex of the matching accent item. BitAccentColorPresets.Blue is the packaged palette's own primary, i.e. \"no override\".",
                },
                new()
                {
                    Name = "AccentChanged",
                    Type = "event EventHandler?",
                    DefaultValue = "",
                    Description = "Raised after ActiveAccent changes, so the switchers can re-render.",
                },
                new()
                {
                    Name = "SeedFromPrerender",
                    Type = "void SeedFromPrerender(string? accent)",
                    DefaultValue = "",
                    Description = "Adopts the accent the server read from the accent cookie while prerendering, so the prerendered markup already marks the right swatch as active.",
                },
                new()
                {
                    Name = "InitializeAsync",
                    Type = "Task InitializeAsync(BitAccentColorConfig? config = null)",
                    DefaultValue = "",
                    Description = "Restores the persisted accent, applies it, and starts tracking dark/light switches. Call it after the first interactive render (the switcher does this itself); only the first interactive call does the work, and its config (falling back to the DI-registered BitAccentColorConfig when null) becomes the app-wide configuration.",
                },
                new()
                {
                    Name = "ApplyAsync",
                    Type = "Task ApplyAsync(string accentColor)",
                    DefaultValue = "",
                    Description = "Applies the given accent color and persists it, per the first-paint strategy and persistence configured by the first InitializeAsync call. Values outside the configured accents are re-validated as plain hex, so an app can programmatically apply an accent it never offers as a swatch.",
                },
            ]
        },
        new()
        {
            Id = "accent-color-store",
            Title = "IBitAccentColorStore",
            Description = "An app-supplied store for the accent preference, for hosts where the built-in web stores are not the right home for it - e.g. a Blazor Hybrid app persisting through native preferences instead of the webview's localStorage. Register an implementation in DI and BitAccentColorService restores from it first (ahead of the stores the Persistence flags enable) and (re)writes it on every apply, so it can be combined with the built-in stores or run alone with BitAccentColorPersistence.None.",
            Parameters =
            [
                new()
                {
                    Name = "GetAccentAsync",
                    Type = "Task<string?> GetAccentAsync()",
                    DefaultValue = "",
                    Description = "Reads the persisted accent color, or null when none is stored. The value goes through the same validation as the built-in stores - anything unrecognized is treated as \"nothing persisted\".",
                },
                new()
                {
                    Name = "SetAccentAsync",
                    Type = "Task SetAccentAsync(string accent)",
                    DefaultValue = "",
                    Description = "Persists the accent color.",
                },
                new()
                {
                    Name = "RemoveAccentAsync",
                    Type = "Task RemoveAccentAsync()",
                    DefaultValue = "",
                    Description = "Removes the persisted accent color. Called when the accent reverts to the packaged palette's own primary, which is stored as \"no preference\" rather than as a value.",
                },
            ]
        },
        new()
        {
            Id = "accent-color-head",
            Title = "BitAccentColorHead",
            Description = "The single-drop first-paint setup: place it at the top of the host page's <head> (after BitThemeSsr.InlineHeadScript, before any stylesheet) and it emits the accent inline script plus the palette CSS the selected FirstPaintStrategy needs (nothing for the default None) - see the \"First paint setup\" demo above.",
            Parameters =
            [
                new()
                {
                    Name = "Config",
                    Type = "BitAccentColorConfig?",
                    DefaultValue = "null",
                    Description = "The app-wide accent configuration this component emits the head half of: its FirstPaintStrategy selects what is emitted (nothing for the default None), its Persistence selects the stores the inline script reads, and its Accents are the palettes emitted in StaticCss mode. When null, the BitAccentColorConfig registered in DI (the accentColor option of AddBitBlazorUIExtrasServices) is used; alternatively hand it the same instance the app's BitAccentColorSwitcher instances use.",
                    LinkType = LinkType.Link,
                    Href = "#accent-color-config",
                },
                new()
                {
                    Name = "Nonce",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Optional CSP nonce for the emitted inline script, to satisfy a script-src 'nonce-…' Content-Security-Policy.",
                },
                new()
                {
                    Name = "PersistedAccent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The persisted accent preference, usually the BitAccentColorNames.CookieName cookie's value read from the request. Only used by the StoredCss strategy; the StaticCss strategy is deliberately cookie-independent.",
                },
                new()
                {
                    Name = "StylesheetHref",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "StaticCss mode only: when set, the all-accents stylesheet is referenced as an external stylesheet at this href (with the library version appended as a cache-buster) instead of being inlined - serve BitAccentColorSsr.BuildStaticCss there with long cache headers. When null, the stylesheet is inlined into the response, which needs no endpoint at the cost of re-sending the (well-compressing) palette CSS per page load.",
                },
            ]
        },
        new()
        {
            Id = "accent-color-ssr",
            Title = "BitAccentColorSsr",
            Description = "Static first-paint helpers for the app's host page - see the \"First paint setup\" demo above.",
            Parameters =
            [
                new()
                {
                    Name = "InlineHeadScript",
                    Type = "string",
                    DefaultValue = "",
                    Description = "Full <script> markup ready to drop into <head>, before the stylesheets. Re-resolves the accent from localStorage / the preference cookie pre-paint: sets the bit-accent root attribute and injects the StoredCss snapshot when one matches. BuildInlineHeadScript(nonce, persistence) is the parameterized variant: the optional CSP nonce is emitted onto the script element, and the optional persistence restricts which stores the script reads.",
                },
                new()
                {
                    Name = "BuildRootAccentAttributes",
                    Type = "string BuildRootAccentAttributes(string? persistedAccent)",
                    DefaultValue = "",
                    Description = "Builds the bit-accent attribute for the root <html> element from the persisted preference (usually the accent cookie), so origin-rendered markup paints correctly with no script involved. BuildRootAccentAttributeMap is the @attributes-splat variant.",
                },
                new()
                {
                    Name = "BuildStaticCss",
                    Type = "string BuildStaticCss(IEnumerable<BitAccentColorItem>? accents = null)",
                    DefaultValue = "",
                    Description = "The accent-agnostic stylesheet of the StaticCss mode: every offered accent's palette, scoped to its bit-accent attribute value and split dark/light on bit-theme. Identical for every visitor, so serve it as a long-cached static asset.",
                },
                new()
                {
                    Name = "BuildPrerenderCss",
                    Type = "string? BuildPrerenderCss(string? persistedAccent, IEnumerable<BitAccentColorItem>? accents = null)",
                    DefaultValue = "",
                    Description = "The per-request style the server emits (as <style id=\"@BitAccentColorNames.StyleElementId\">) so an origin-rendered page paints the persisted accent immediately - the server half of the StoredCss mode.",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "accent-color-first-paint-strategy-enum",
            Name = "BitAccentColorFirstPaintStrategy",
            Description = "How the accent palette reaches the very first paint of a page load, before any Blazor runtime is up. After hydration every strategy behaves identically; the strategy only decides what a cold browser has to work with, which is what matters when the served HTML comes from a cache (e.g. a CDN) that cannot vary on this visitor's cookie. The CSS strategies restore the accent from the stores Persistence enables, so they require a BitAccentColorPersistence other than None - with nothing persisted there is nothing to restore pre-paint, and the inline head script is not even emitted.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "No first-paint machinery (the default): the accent is applied only after hydration, so a server-rendered page paints the packaged palette first and flips to the persisted accent once the client is up. The preference is still persisted to (and restored from) whatever stores BitAccentColorPersistence enables; no bit-accent attribute is set, no palette snapshot is kept, and BitAccentColorHead emits nothing.",
                    Value = "0",
                },
                new()
                {
                    Name = "StaticCss",
                    Description = "First paint comes from a static stylesheet holding the palettes of every offered accent, keyed on the bit-accent root attribute the inline head script sets pre-paint. The served HTML is accent-agnostic and safe to cache. Recommended when the accents are a fixed set known at build time.",
                    Value = "1",
                },
                new()
                {
                    Name = "StoredCss",
                    Description = "First paint comes from a snapshot of the generated palette CSS kept in localStorage, injected pre-paint by the inline head script. No static stylesheet is needed, so the accents do not have to be known up front. The snapshot lives in localStorage, so this strategy needs the LocalStorage store enabled.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "accent-color-persistence-enum",
            Name = "BitAccentColorPersistence",
            Description = "Which stores the accent preference is persisted to when an accent is applied (a flags enum). The two stores serve different readers: localStorage is the client-side copy the runtime restores from, while the cookie is the only copy the server can read - it is what lets SSR prerender the page with the visitor's accent. Stores a configuration disables are cleaned up on the next apply, so changing it leaves no stale copy behind.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "Nothing is persisted (the default): the accent applies for the current session only and is gone on the next load.",
                    Value = "0",
                },
                new()
                {
                    Name = "LocalStorage",
                    Description = "The localStorage entry the client restores the accent from.",
                    Value = "1",
                },
                new()
                {
                    Name = "Cookie",
                    Description = "The cookie that carries the preference to the server, so SSR can prerender the visitor's accent. Required for the server halves of BitAccentColorSsr to see anything.",
                    Value = "2",
                },
                new()
                {
                    Name = "All",
                    Description = "Both stores. Writing both also self-heals either store going missing on its own.",
                    Value = "3",
                },
            ]
        },
    ];



    private string? changedAccentColor;

    private readonly BitAccentColorConfig customAccentsConfig = new()
    {
        Accents =
        [
            new() { Name = "Crimson", Color = "#DC143C" },
            new() { Name = "Indigo", Color = "#4B0082" },
            new() { Name = "Chocolate", Color = "#D2691E" },
        ],
    };

    private readonly BitAccentColorConfig persistenceConfig = new()
    {
        Persistence = BitAccentColorPersistence.All,
    };

    private readonly BitAccentColorConfig firstPaintConfig = new()
    {
        FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss,
        Persistence = BitAccentColorPersistence.All,
    };



    private readonly string example1RazorCode = @"
<BitAccentColorSwitcher />";

    private readonly string example2RazorCode = @"
<BitAccentColorSwitcher Config=""customAccentsConfig"" />

@code {
    private readonly BitAccentColorConfig customAccentsConfig = new()
    {
        Accents =
        [
            new() { Name = ""Crimson"", Color = ""#DC143C"" },
            new() { Name = ""Indigo"", Color = ""#4B0082"" },
            new() { Name = ""Chocolate"", Color = ""#D2691E"" },
        ],
    };
}";

    private readonly string example3RazorCode = @"
<BitAccentColorSwitcher Config=""persistenceConfig"" />

@code {
    private readonly BitAccentColorConfig persistenceConfig = new()
    {
        Persistence = BitAccentColorPersistence.All,
    };
}";

    private readonly string example4RazorCode = @"
<BitAccentColorSwitcher Config=""firstPaintConfig"" />

@code {
    private readonly BitAccentColorConfig firstPaintConfig = new()
    {
        FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss,
        Persistence = BitAccentColorPersistence.All,
    };
}";

    private readonly string example5RazorCode = @"
<BitAccentColorSwitcher OnChange=""color => changedAccentColor = color"" />

<div>Changed accent color: <b>@(changedAccentColor ?? ""-"")</b></div>";

    private readonly string example5CsharpCode = @"
private string? changedAccentColor;";

    private readonly string example6RazorCode = @"
@* In the host page (e.g. App.razor of a Blazor Web App): *@

<head>
    @* After BitThemeSsr.InlineHeadScript and before the stylesheets. Emits the inline script that
       re-resolves the accent from localStorage / the cookie pre-paint - which is what keeps the
       accent correct when the HTML comes out of a cache that served it to a visitor whose cookie
       never reached the server - plus the palette CSS. The configuration comes from the
       BitAccentColorConfig registered in DI (see the C# tab); a Config parameter would override it.
       In StaticCss mode with no StylesheetHref, the whole all-accents stylesheet is inlined; no
       endpoint needed. *@
    <BitAccentColorHead />

    @* Or reference the stylesheet as a long-cached asset instead of inlining it (see the C# tab;
       the library version is appended as a cache-buster automatically). Keep the href root-relative:
       this sits before any <base>, so a relative href would resolve against the current page path
       and 404 on every non-root route. *@
    <BitAccentColorHead StylesheetHref=""/accent-colors.css"" />

    @* StoredCss strategy: no stylesheet at all; pass the accent cookie so origin-rendered responses
       paint immediately (cached responses are covered by the localStorage snapshot): *@
    <BitAccentColorHead PersistedAccent=""@HttpContext.Request.Cookies[BitAccentColorNames.CookieName]"" />
    ...
</head>

@* And wherever the switcher renders (a layout, a settings page, ...) - it falls back to the same
   DI-registered configuration: *@
<BitAccentColorSwitcher />";

    private readonly string example6CsharpCode = @"
// The app-wide configuration, stated ONCE in the service-registration method both the server and
// the client Program.cs already call (the usual shared AddClientServices-style extension), so the
// BitAccentColorHead and every BitAccentColorSwitcher - in whichever process they render - resolve
// the same values:
services.AddBitBlazorUIExtrasServices(accentColor: options =>
{
    options.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss;
    options.Persistence = BitAccentColorPersistence.All;
    // options.Accents = ...;
});

// Alternative without DI: define one shared BitAccentColorConfig instance (e.g. a static field in
// a shared project) and hand that same instance to the Config parameter of the head and of every
// switcher - a Config parameter always outranks the DI-registered configuration.

// StaticCss mode: serve the all-accents stylesheet as a long-cached asset (e.g. a minimal API):
app.MapGet(""/accent-colors.css"", context =>
{
    context.Response.Headers.ContentType = ""text/css"";
    context.Response.Headers.CacheControl = ""public, max-age=31536000, immutable"";

    return context.Response.WriteAsync(BitAccentColorSsr.BuildStaticCss(), context.RequestAborted);
});

// To mark the right swatch active in prerendered markup, cascade the cookie into the component
// tree and hand it to BitAccentColorService.SeedFromPrerender before the first render:
services.AddCascadingValue(""PrerenderedAccentColor"", sp =>
    sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Cookies[BitAccentColorNames.CookieName]);";

    private readonly string example7RazorCode = @"
<style>
    .custom-swatch {
        border-radius: 0.25rem;
        border-width: 2px;
    }

    .custom-active-swatch {
        outline-width: 4px;
        outline-style: dotted;
    }
</style>

<BitAccentColorSwitcher Style=""padding:0.5rem;border:1px dashed gray;border-radius:0.5rem"" />

<BitAccentColorSwitcher Classes=""@(new() { Swatch = ""custom-swatch"", ActiveSwatch = ""custom-active-swatch"" })"" />

<BitAccentColorSwitcher Styles=""@(new() { Root = ""gap:1.5rem"", Swatch = ""border-radius:0.25rem"" })"" />";
}
