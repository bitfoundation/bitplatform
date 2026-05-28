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

    private static string BuildInlineScriptBody()
    {
        // Centralizing names through BitThemeAttributeNames means the inline script picks up renames
        // automatically — the previous version hard-coded each attribute literal twice (here and in
        // bit-theme.ts), which was a maintenance hazard.
        var theme = BitThemeAttributeNames.Theme;
        var themeDefault = BitThemeAttributeNames.ThemeDefault;
        var themeSystem = BitThemeAttributeNames.ThemeSystem;
        var themePersist = BitThemeAttributeNames.ThemePersist;
        var themeDark = BitThemeAttributeNames.ThemeDark;
        var themeLight = BitThemeAttributeNames.ThemeLight;
        var storageKey = BitThemeAttributeNames.ThemeStorageKey;

        // The inline script is intentionally compact; it runs on every first paint before stylesheets.
        // Logic mirrors Theme.init in bit-theme.ts.
        return
            "(function(){var r=document.documentElement," +
            $"k='{storageKey}'," +
            $"lt=r.getAttribute('{themeLight}')||'light'," +
            $"dk=r.getAttribute('{themeDark}')||'dark'," +
            "m=window.matchMedia&&matchMedia('(prefers-color-scheme:dark)').matches," +
            $"base=r.getAttribute('{theme}')||r.getAttribute('{themeDefault}')||'light';" +
            $"if(r.hasAttribute('{themeSystem}')){{base=m?dk:lt;}}" +
            $"var cur=base;if(r.hasAttribute('{themePersist}')){{try{{cur=localStorage.getItem(k)||base;}}catch(e){{}}}}" +
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
