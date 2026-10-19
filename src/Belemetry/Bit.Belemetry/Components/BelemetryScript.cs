using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Belemetry;

/// <summary>
/// Renders the browser script with its configuration inline, so it starts as early as possible - before Blazor boots,
/// and on static SSR pages that never become interactive. Place it in the <c>&lt;head&gt;</c> of <c>App.razor</c>.
/// </summary>
/// <remarks>
/// Only effective where it is rendered statically (the document head of a Blazor Web App). In a standalone WebAssembly
/// or Hybrid app, use <see cref="BelemetryInitializer"/>, optionally with a manual
/// <c>&lt;script type="module" src="_content/Bit.Belemetry/bit-belemetry.js"&gt;</c> in <c>index.html</c>.
/// </remarks>
public sealed class BelemetryScript : ComponentBase
{
    [Inject] private BelemetrySettings Settings { get; set; } = default!;

    /// <summary>The script URL. Defaults to <c>_content/Bit.Belemetry/bit-belemetry.js</c>; set it to the fingerprinted asset path when using <c>@Assets[...]</c>.</summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>A CSP nonce added to both script elements.</summary>
    [Parameter] public string? Nonce { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Settings.Options.Enabled is false || Settings.Options.Browser.Enabled is false) return;

        // The JSON serializer escapes '<', '>' and '&', so the config can never close the script element early.
        var json = BelemetryBrowserConfigFactory.ToJson(BelemetryBrowserConfigFactory.Create(Settings));
        var nonce = Nonce is null ? string.Empty : $" nonce=\"{WebUtility.HtmlEncode(Nonce)}\"";
        var src = WebUtility.HtmlEncode(Src ?? BelemetryJsBridge.ModulePath[2..]);

        builder.AddMarkupContent(0, $"<script type=\"application/json\" id=\"bit-belemetry-config\"{nonce}>{json}</script>\n" +
                                    $"<script type=\"module\" src=\"{src}\"{nonce}></script>");
    }
}
