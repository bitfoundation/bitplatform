namespace Bit.Bheadless.Demo.Server;

/// <summary>
/// The Content-Security-Policy /strict-csp is served with (Program.cs), and which the E2E suite checks the page
/// against (StrictCspTests). Its <c>style-src</c> has no <c>'unsafe-inline'</c>: the browser refuses every
/// <c>style</c> attribute, in the markup and set from script alike, and only the stylesheets of the site itself
/// apply. That is the part this page proves the components work under (<c>BitBhOptions.StrictCsp</c>);
/// <c>script-src</c> allows the inline scripts App.razor renders on every page (the import map, the title keeper)
/// and WebAssembly compilation.
/// </summary>
public static class StrictCspPolicy
{
    public const string Path = "/strict-csp";

    public const string Value =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none'";
}
