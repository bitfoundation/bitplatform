namespace Bit.Belemetry.Samples.Core;

/// <summary>What differs between the hosts the shared pages run in.</summary>
public sealed class SampleSettings
{
    /// <summary>
    /// The URL the "fetch" button requests. The web hosts serve <c>api/weather</c>; the MAUI app has no server, so it
    /// fetches a file of its own <c>wwwroot</c>.
    /// </summary>
    public string FetchUrl { get; set; } = "api/weather";

    /// <summary>
    /// The URL the support page's "failing request" button requests: the web hosts answer <c>api/fail</c> with a 500;
    /// the MAUI and WinForms apps ask for a file their <c>wwwroot</c> does not have (a 404).
    /// </summary>
    public string FailUrl { get; set; } = "api/fail";

    /// <summary>
    /// Where the support page saves a confirmed support bundle when the host's web view does not save downloads (the
    /// MAUI app on Android, iOS and Mac Catalyst): returns where it was written. Null (the web hosts, WinForms' WebView2)
    /// lets the preview download it in the browser.
    /// </summary>
    public Func<BelemetryDiagnosticCapture, Task<string>>? SaveBundle { get; set; }
}
