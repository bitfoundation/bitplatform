namespace Boilerplate.Client.Core.Components.Common;

/// <summary>
/// A polished, reusable app &amp; device info panel used on the About page of each client (Web, MAUI, Windows).
/// Platform-specific values are gathered by the host page; the presentation lives here so every client stays consistent.
/// </summary>
public partial class AppInfoCard
{
    [Parameter, EditorRequired] public string AppName { get; set; } = default!;
    [Parameter, EditorRequired] public string AppVersion { get; set; } = default!;
    /// <summary>
    /// The device's operating system; hidden when not provided. The web head cannot answer this while it is being
    /// rendered on the server, so it fills it in after the first render rather than showing the server's OS.
    /// </summary>
    [Parameter] public string? Platform { get; set; }

    [Parameter, EditorRequired] public string Environment { get; set; } = default!;

    /// <summary>
    /// Optional process id; hidden when not provided. The native heads run in the process the user launched, so it
    /// identifies their app - on the web the component may be rendered server-side, where it would be the web server's
    /// worker instead, which is neither the visitor's nor useful to them.
    /// </summary>
    [Parameter] public string? ProcessId { get; set; }

    /// <summary>Optional native web view (MAUI/Windows); hidden when not provided (e.g. on the web).</summary>
    [Parameter] public string? WebView { get; set; }

    /// <summary>Optional device manufacturer/OEM; hidden when not provided.</summary>
    [Parameter] public string? Oem { get; set; }

    /// <summary>Optional note/guidance rendered as an info message under the details.</summary>
    [Parameter] public RenderFragment? Note { get; set; }

    private string Monogram => string.IsNullOrWhiteSpace(AppName) ? "?" : AppName.Trim()[..1].ToUpperInvariant();

    private BitColor EnvironmentColor => Environment?.ToLowerInvariant() switch
    {
        "production" or "prod" => BitColor.Success,
        "development" or "dev" => BitColor.Warning,
        _ => BitColor.Info
    };
}
