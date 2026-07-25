namespace Bit.BlazorUI;

/// <summary>
/// Optional first-paint theme bootstrap for apps that use <c>bit-theme-persist</c> and/or <c>bit-theme-system</c> on the document element.
/// Order matches <c>bit-theme.ts</c> <c>init</c>: base from attributes, then <c>bit-theme-system</c> (prefers-color-scheme), then <c>bit-theme-persist</c> (localStorage), then resolve stored <c>system</c>.
/// Emit <see cref="InlineHeadScript"/> at the start of <c>&lt;head&gt;</c> (before stylesheets) so the correct <c>bit-theme</c> attribute is set before first paint.
/// </summary>
/// <remarks>
/// Attribute and storage key names come from <see cref="BitThemeAttributeNames"/> so the inline script and the runtime <c>BitBlazorUI.Theme</c> client stay in sync.
/// </remarks>
public static class BitThemeSsr
{
    /// <summary>
    /// Inline script body without a wrapping <c>&lt;script&gt;</c> tag. Concatenate into your own
    /// <c>&lt;script&gt;</c> element when you need full control over attributes (CSP nonce,
    /// <c>type</c>, <c>defer</c>, etc.).
    /// </summary>
    public static readonly string InlineHeadScriptBody = BuildInlineScriptBody();

    /// <summary>
    /// Full <c>&lt;script&gt;</c> markup ready to drop into <c>&lt;head&gt;</c>. Equivalent to
    /// <see cref="BuildInlineHeadScript(string?)"/> with a <see langword="null"/> nonce.
    /// </summary>
    public static string InlineHeadScript => BuildInlineHeadScript(nonce: null);

    /// <summary>
    /// Builds the inline first-paint script and wraps it in a <c>&lt;script&gt;</c> element.
    /// Pass <paramref name="nonce"/> to satisfy a <c>script-src 'nonce-…'</c> Content-Security-Policy.
    /// </summary>
    /// <param name="nonce">Optional CSP nonce. When supplied, the value is HTML-attribute-encoded and emitted as <c>nonce="…"</c>.</param>
    public static string BuildInlineHeadScript(string? nonce)
    {
        if (string.IsNullOrWhiteSpace(nonce))
        {
            return $"<script>{InlineHeadScriptBody}</script>";
        }

        // HTML-attribute-encode the nonce so a tampered value cannot break out of the attribute.
        // CSP nonces are base64url in practice, but defense-in-depth is cheap.
        var safeNonce = HtmlEncodeAttribute(nonce);
        return $"<script nonce=\"{safeNonce}\">{InlineHeadScriptBody}</script>";
    }

    /// <summary>
    /// Builds the <c>bit-theme*</c> attribute string to render on the root <c>&lt;html&gt;</c>
    /// element from a persisted preference (typically read from the
    /// <see cref="BitThemeCookie.PreferenceCookieName"/> cookie) so server-rendered markup paints
    /// the correct theme before the client script runs.
    /// </summary>
    /// <param name="persistedPreference">
    /// The stored abstract theme key (for example <c>dark</c>, <c>fluent-light</c>, or
    /// <c>system</c>). Usually the cookie value; may be <see langword="null"/>/blank when no
    /// preference has been stored yet.
    /// </param>
    /// <param name="defaultTheme">
    /// Optional theme to use when <paramref name="persistedPreference"/> is missing, or as the
    /// no-JS base when the OS is being followed.
    /// </param>
    /// <returns>
    /// An attribute fragment such as <c>bit-theme="dark"</c> or
    /// <c>bit-theme-system bit-theme-default="light"</c>, ready to interpolate into the root
    /// <c>&lt;html&gt;</c> tag.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A concrete preference is emitted as <c>bit-theme="…"</c> so first paint is correct with no
    /// flash. The special <c>system</c> value (and a missing preference with no default) emit the
    /// <c>bit-theme-system</c> marker and defer the light/dark resolution to the client inline
    /// script - the server cannot read <c>prefers-color-scheme</c>. A missing preference with a
    /// concrete <paramref name="defaultTheme"/> paints that default directly.
    /// </para>
    /// <para>
    /// Values are normalized (trim + lower-case, matching <see cref="BitThemeName"/>) and validated
    /// against a strict <c>[a-z0-9-]</c> theme-name pattern; anything else is ignored and treated as
    /// missing, so a tampered cookie cannot inject markup into the document.
    /// </para>
    /// <para>
    /// This is the server half of cookie-based persistence: the app is responsible for writing the
    /// cookie (client- or server-side) under <see cref="BitThemeCookie.PreferenceCookieName"/>.
    /// </para>
    /// </remarks>
    public static string BuildRootThemeAttributes(string? persistedPreference, string? defaultTheme = null)
    {
        var preference = NormalizeThemeToken(persistedPreference);
        var fallback = NormalizeThemeToken(defaultTheme);

        // Explicit "system" → follow the OS; the inline head script resolves light/dark before
        // paint. Carry a concrete default through for no-JS clients.
        if (preference == BitThemePresets.System)
        {
            return fallback is null || fallback == BitThemePresets.System
                ? BitThemeAttributeNames.ThemeSystem
                : $"{BitThemeAttributeNames.ThemeSystem} {BitThemeAttributeNames.ThemeDefault}=\"{fallback}\"";
        }

        // Concrete persisted preference → paint it directly (no flash).
        if (preference is not null)
        {
            return $"{BitThemeAttributeNames.Theme}=\"{preference}\"";
        }

        // No stored preference: use the configured default theme, or follow the OS if none.
        if (fallback is not null && fallback != BitThemePresets.System)
        {
            return $"{BitThemeAttributeNames.Theme}=\"{fallback}\"";
        }

        return BitThemeAttributeNames.ThemeSystem;
    }

    /// <summary>
    /// Normalizes a theme key (trim + lower-case invariant) and validates it as a safe
    /// <c>[a-z0-9-]</c> token. Returns <see langword="null"/> for blank or invalid input so callers
    /// treat it as "no preference" - this also prevents a tampered cookie value from being emitted
    /// into the document.
    /// </summary>
    private static string? NormalizeThemeToken(string? value) => BitThemeName.NormalizeToken(value, out _);

    private static string BuildInlineScriptBody()
    {
        // Centralizing names through BitThemeAttributeNames means the inline script picks up renames
        // automatically - the previous version hard-coded each attribute literal twice (here and in
        // bit-theme.ts), which was a maintenance hazard.
        var theme = BitThemeAttributeNames.Theme;
        var themeDefault = BitThemeAttributeNames.ThemeDefault;
        var themeSystem = BitThemeAttributeNames.ThemeSystem;
        var themePersist = BitThemeAttributeNames.ThemePersist;
        var themePersistCookie = BitThemeAttributeNames.ThemePersistCookie;
        var themeDark = BitThemeAttributeNames.ThemeDark;
        var themeLight = BitThemeAttributeNames.ThemeLight;
        var storageKey = BitThemeAttributeNames.ThemeStorageKey;
        var cookieName = BitThemeCookie.PreferenceCookieName;

        // The inline script is intentionally compact; it runs on every first paint before stylesheets.
        // Logic mirrors Theme.init in bit-theme.ts, including the localStorage→cookie persistence
        // fallback so a cookie-only (SSR) persistence setup paints correctly with no flash.
        return
            "(function(){var r=document.documentElement," +
            $"k='{storageKey}'," +
            $"ck='{cookieName}'," +
            $"lt=r.getAttribute('{themeLight}')||'light'," +
            $"dk=r.getAttribute('{themeDark}')||'dark'," +
            "m=window.matchMedia&&matchMedia('(prefers-color-scheme:dark)').matches," +
            // Fall back to the CONFIGURED light theme (lt), not the literal 'light': BitTheme.ts init
            // resolves its base to Theme._lightTheme (= bit-theme-light's value). Using 'light' here
            // would paint a different attribute than hydration for <html bit-theme-light="fluent-light">
            // with no bit-theme/-default/-system - a flash of the wrong theme. lt already defaults to
            // 'light', so this is strictly safer.
            $"base=r.getAttribute('{theme}')||r.getAttribute('{themeDefault}')||lt;" +
            $"if(r.hasAttribute('{themeSystem}')){{base=m?dk:lt;}}" +
            "var cur=base,got=false;" +
            $"if(r.hasAttribute('{themePersist}')){{try{{var s=localStorage.getItem(k);if(s){{cur=s;got=true;}}}}catch(e){{}}}}" +
            $"if(!got&&r.hasAttribute('{themePersistCookie}')){{var cm=('; '+document.cookie).match('; '+ck+'=([^;]*)');if(cm){{try{{cur=decodeURIComponent(cm[1]);}}catch(e){{cur=cm[1];}}}}}}" +
            "if(cur==='system'){cur=m?dk:lt;}" +
            $"r.setAttribute('{theme}',cur);}})();";
    }

    private static string HtmlEncodeAttribute(string value)
    {
        // Minimal attribute-context encoding; we don't want a full WebUtility dep just for this and
        // CSP nonces are constrained to base64url chars in practice. Encode the characters that can
        // break out of a double-quoted attribute or alter parsing.
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&': builder.Append("&amp;"); break;
                case '"': builder.Append("&quot;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                default: builder.Append(ch); break;
            }
        }
        return builder.ToString();
    }
}
