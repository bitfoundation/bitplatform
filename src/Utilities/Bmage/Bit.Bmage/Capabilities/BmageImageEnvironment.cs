namespace Bit.Bmage;

/// <summary>Where Bmage is running.</summary>
public enum BmageImageEnvironment
{
    Unknown = 0,
    /// <summary>Blazor WebAssembly, or another browser host: full browser API access.</summary>
    BrowserWebAssembly,
    /// <summary>
    /// .NET runs outside the browser, which is reachable only through JavaScript interop: Blazor Server, over a circuit,
    /// or Blazor Hybrid (MAUI, WPF, Windows Forms), over the WebView's bridge inside the app.
    /// </summary>
    BrowserServer,
    /// <summary>Plain .NET with no browser: only the managed codecs and headless APIs work.</summary>
    Server,
    /// <summary>Server-side prerendering, before interactivity is established. No browser APIs are usable yet.</summary>
    Prerendering,
}
