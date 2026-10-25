namespace Bit.Bsqlite;

/// <summary>Where the application's .NET code runs, which decides where Bsqlite runs the engine.</summary>
internal enum BsqliteHostKind
{
    /// <summary>
    /// Anywhere else: a server rendering or prerendering pages, whose users' databases live in their
    /// browsers, or a desktop process such as a test run.
    /// </summary>
    Other,

    /// <summary>Blazor WebAssembly: the engine runs in a dedicated worker in the same tab.</summary>
    Browser,

    /// <summary>
    /// Blazor Hybrid: .NET runs natively next to a web view, and the engine is the native SQLite that
    /// ships with the package, opened in-process on a file of the app's own.
    /// </summary>
    BlazorHybrid,
}

/// <summary>Works out the <see cref="BsqliteHostKind"/> of the running application.</summary>
internal static class BsqliteHost
{
    private static readonly Lazy<BsqliteHostKind> Detected = new(Detect);

    /// <summary>The host this process is, worked out once.</summary>
    public static BsqliteHostKind Current => Detected.Value;

    private static BsqliteHostKind Detect()
    {
        if (OperatingSystem.IsBrowser())
        {
            return BsqliteHostKind.Browser;
        }

        // Blazor on a phone, a tablet or Mac Catalyst is always Blazor Hybrid.
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsTvOS())
        {
            return BsqliteHostKind.BlazorHybrid;
        }

        // On the desktop, a Blazor Hybrid app - MAUI on Windows, WPF, Windows Forms, or any other web view
        // host - is built on Microsoft.AspNetCore.Components.WebView, which no server or browser app ships.
        return Type.GetType("Microsoft.AspNetCore.Components.WebView.WebViewManager, Microsoft.AspNetCore.Components.WebView", throwOnError: false) is null
            ? BsqliteHostKind.Other
            : BsqliteHostKind.BlazorHybrid;
    }
}
