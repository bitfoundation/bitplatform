using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

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
    /// Blazor Hybrid: .NET runs natively next to a web view, and the engine is the native SQLite the app
    /// brings, opened in-process on a file of the app's own.
    /// </summary>
    BlazorHybrid,
}

/// <summary>Works out the <see cref="BsqliteHostKind"/> of the running application.</summary>
internal static class BsqliteHost
{
    private const string WebViewAssemblyName = "Microsoft.AspNetCore.Components.WebView";

    private static readonly Lazy<BsqliteHostKind> Detected = new(Detect);

    private static readonly Lazy<bool> ShipsWebView = new(() => Type.GetType(
        $"Microsoft.AspNetCore.Components.WebView.WebViewManager, {WebViewAssemblyName}",
        throwOnError: false) is not null);

    /// <summary>
    /// What the process alone tells: the browser, or a phone, a tablet or Mac Catalyst - where Blazor is
    /// always Blazor Hybrid. A desktop process is <see cref="BsqliteHostKind.Other"/> here: whether it is a
    /// Blazor Hybrid app only its services tell, see <see cref="Of"/>.
    /// </summary>
    public static BsqliteHostKind Current => Detected.Value;

    /// <summary>
    /// Whether this process could be a Blazor Hybrid app: <see cref="Current"/> says so, or it is a desktop
    /// process that ships Microsoft.AspNetCore.Components.WebView - which a WPF, Windows Forms or MAUI app on
    /// Windows does, but so may a server that references a library built on it.
    /// </summary>
    public static bool MayBeBlazorHybrid => Current is BsqliteHostKind.BlazorHybrid
        || (Current is BsqliteHostKind.Other && ShipsWebView.Value);

    /// <summary>The host the application whose services <paramref name="services"/> are runs as.</summary>
    /// <remarks>
    /// On the desktop, a Blazor Hybrid app is one whose <see cref="IJSRuntime"/> is the web view's: every web view
    /// host - MAUI, WPF, Windows Forms, or any other built on Microsoft.AspNetCore.Components.WebView - registers it
    /// through <c>AddBlazorWebView</c>, and no server or test host does, whatever assemblies it ships. Resolving it
    /// calls no JavaScript, so it works before the web view is up.
    /// </remarks>
    public static BsqliteHostKind Of(IServiceProvider services)
    {
        if (Current is not BsqliteHostKind.Other || !ShipsWebView.Value)
        {
            return Current;
        }

        return string.Equals(
                services.GetService<IJSRuntime>()?.GetType().Assembly.GetName().Name,
                WebViewAssemblyName,
                StringComparison.Ordinal)
            ? BsqliteHostKind.BlazorHybrid
            : BsqliteHostKind.Other;
    }

    private static BsqliteHostKind Detect()
    {
        if (OperatingSystem.IsBrowser())
        {
            return BsqliteHostKind.Browser;
        }

        // Blazor on a phone, a tablet or Mac Catalyst is always Blazor Hybrid.
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsTvOS()
            ? BsqliteHostKind.BlazorHybrid
            : BsqliteHostKind.Other;
    }
}

/// <summary>The host this scope's application runs as - worked out once per scope, from its services.</summary>
internal sealed record BsqliteRuntimeHost(BsqliteHostKind Kind);
