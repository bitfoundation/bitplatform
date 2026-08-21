using System.Reflection;
using Bit.Bswup.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// The reference for the built-in progress UI: the parameters of the <c>BswupProgress</c>
/// component, read off the shipped assembly, plus the element ids
/// <c>bit-bswup.progress.js</c> drives.
/// <para>
/// The parameters are reflected rather than listed by hand for the same reason the rest of this
/// server reads the shipped sources: a default that changed - <c>AutoReload</c> did, in v-10-6-0 -
/// must not keep being reported as what it used to be. The element ids come from the script's own
/// contract and are the part a custom <c>ChildContent</c> splash has to write against.
/// </para>
/// </summary>
public static class BswupProgressCatalog
{
    private static readonly Lazy<BswupProgressUiDto> _progressUi = new(Build);

    public static BswupProgressUiDto ProgressUi => _progressUi.Value;

    /// <summary>The component's parameters, for the search index.</summary>
    public static BswupOptionDto[] Parameters => _progressUi.Value.Parameters;

    private static BswupProgressUiDto Build()
    {
        return new BswupProgressUiDto
        {
            Parameters = BuildParameters(),
            Elements =
            [
                new() { Id = "bit-bswup", Role = "The splash overlay itself. Hidden by bit-bswup.progress.css until a first install reveals it." },
                new() { Id = "bit-bswup-progress-bar", Role = "The bar whose width (and aria-valuenow) is set from the download percentage." },
                new() { Id = "bit-bswup-percent", Role = "Text node written as `${percent}%`." },
                new() { Id = "bit-bswup-assets", Role = "List the downloaded assets are prepended to, when ShowAssets is on." },
                new() { Id = "bit-bswup-error", Role = "The failure panel, revealed on a fatal install error." },
                new() { Id = "bit-bswup-error-message", Role = "The human-readable message of the failure." },
                new() { Id = "bit-bswup-error-details", Role = "The structured detail of the failure (reason, url, hash)." },
                new() { Id = "bit-bswup-error-retry", Role = "Retries the install." },
                new() { Id = "bit-bswup-reload", Role = "The update-ready button. Rendered OUTSIDE the overlay, because a background update never reveals the splash - with AutoReload off this button is the only way a finished update surfaces.", RenderedByComponent = true },
                new() { Id = "bit-bswup-reload-status", Role = "A visually hidden role=\"status\" region: revealing a display:none button is silent for screen readers, so the announcement rides here.", RenderedByComponent = true },
            ],
            RuntimeConfig = "BitBswupProgress.config({ autoReload, showLogs, showAssets, hideApp, autoHide }) - each value overrides the matching parameter for the rest of the session.",
            Requires =
            [
                "<link rel=\"stylesheet\" href=\"_content/Bit.Bswup/bit-bswup.progress.css\" />",
                "<script src=\"_content/Bit.Bswup/bit-bswup.progress.js\"></script> (after bit-bswup.js)",
            ],
            Notes =
            [
                "The full-screen splash is FIRST-INSTALL only. A background update downloads silently behind the running app and surfaces through the reload button alone.",
                "The component emits no inline <script>: its parameters are published as data-bit-bswup-* attributes that the script reads at load. That is what makes it work under a strict Content-Security-Policy and when rendered by an interactive Blazor renderer.",
                "ChildContent replaces the default splash markup; the component keeps configuring itself and drives whichever documented ids your markup includes. Do not render your own #bit-bswup-reload or #bit-bswup-reload-status - the component always renders those two itself, and a duplicate id would shadow the working button.",
                "Handler names an ADDITIONAL function called after the built-in handling, so custom behavior layers on instead of replacing the UI. Pointing it at bitBswupHandler itself is detected and ignored.",
                "In a standalone WebAssembly app the component cannot render early enough to be the first-install splash (Blazor only starts once the install finishes) - write the splash markup into index.html instead, as Sample/BasicSample/wwwroot/index.html does.",
            ]
        };
    }

    private static BswupOptionDto[] BuildParameters()
    {
        var type = typeof(BswupProgress);

        // The parameter list is read by reflection; the instance only supplies the defaults. A
        // component that cannot be constructed here (a constructor that reaches for a service)
        // must therefore cost the defaults, not the whole catalog - TryRead already answers
        // "absent" for a null instance.
        object? instance;
        try
        {
            instance = Activator.CreateInstance(type);
        }
        catch (Exception)
        {
            instance = null;
        }

        // Reading order, not declaration order: a parameter with no description recorded here is
        // still listed (reflection is what decides the set) - it simply lands at the end.
        var order = _descriptions.Keys.Select((name, index) => (name, index))
                                      .ToDictionary(entry => entry.name, entry => entry.index, StringComparer.Ordinal);

        return
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
                .OrderBy(property => order.GetValueOrDefault(property.Name, int.MaxValue))
                .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
                .Select(property => new BswupOptionDto
                {
                    Name = property.Name,
                    Type = FriendlyName(property.PropertyType),
                    Default = Format(TryRead(property, instance)),
                    Summary = _descriptions.GetValueOrDefault(property.Name),
                    VerifiedFromSource = true
                })
        ];
    }

    private static object? TryRead(PropertyInfo property, object? instance)
    {
        if (instance is null || property.GetMethod is null || property.GetMethod.IsPublic is false) return null;

        try
        {
            return property.GetValue(instance);
        }
        catch (Exception)
        {
            // A parameter that only makes sense on a mounted component has no observable default;
            // that is not a reason to fail the whole reference.
            return null;
        }
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        _ => value.ToString()
    };

    private static string FriendlyName(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null) return $"{FriendlyName(nullable)}?";

        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";

        return type.Name;
    }

    // Ordered the way the parameters are worth reading, which is also the order the reference
    // renders them in.
    private static readonly Dictionary<string, string> _descriptions = new(StringComparer.Ordinal)
    {
        ["AutoReload"] = "Activate a finished update immediately (reloading every open tab) instead of showing the reload button. CHANGED in v-10-6-0: this used to default to true. First installs are unaffected - they always complete the seamless claim-and-start flow with no reload.",
        ["HideApp"] = "Hide the app container while the first install downloads.",
        ["AppContainer"] = "Selector of the element to hide while installing (used with HideApp). An invalid selector is tolerated - the splash still works, only the hiding is skipped.",
        ["AutoHide"] = "Hide the splash automatically when the download finishes.",
        ["ShowAssets"] = "List each downloaded asset inside the splash.",
        ["ShowLogs"] = "Log lifecycle messages to the console.",
        ["Handler"] = "Name of an ADDITIONAL global handler function invoked after the built-in handling, so custom behavior layers on top of the UI instead of replacing it.",
        ["ChildContent"] = "Replaces the default splash markup with your own. The component keeps initializing itself and drives whichever of the documented element ids your markup includes.",
    };
}
