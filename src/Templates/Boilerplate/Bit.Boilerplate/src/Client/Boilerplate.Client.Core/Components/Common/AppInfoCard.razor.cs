namespace Boilerplate.Client.Core.Components.Common;

/// <summary>
/// A polished, reusable app &amp; device info panel used on the About page of each client (Web, MAUI, Windows).
/// Platform-specific values are gathered by the host page; the presentation lives here so every client stays consistent.
/// </summary>
public partial class AppInfoCard
{
    [Parameter, EditorRequired] public string AppName { get; set; } = default!;
    [Parameter, EditorRequired] public string AppVersion { get; set; } = default!;
    [Parameter, EditorRequired] public string Platform { get; set; } = default!;
    [Parameter, EditorRequired] public string Environment { get; set; } = default!;
    [Parameter, EditorRequired] public string ProcessId { get; set; } = default!;

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
