namespace Bit.BlazorUI;

/// <summary>
/// Optional first-paint theme bootstrap for apps that use <c>bit-theme-persist</c> on the document element.
/// Emit <see cref="InlineHeadScript"/> at the start of <c>&lt;head&gt;</c> (before stylesheets) so the correct <c>bit-theme</c> attribute is set before first paint.
/// </summary>
public static class BitThemeSsr
{
    /// <summary>
    /// Inline script only (no script tag). Wrap in a script element in your host page or layout.
    /// </summary>
    public const string InlineHeadScriptBody =
        "(function(){var r=document.documentElement,k='bit-current-theme',cur;" +
        "if(r.hasAttribute('bit-theme-persist')){cur=localStorage.getItem(k);}" +
        "cur=cur||r.getAttribute('bit-theme')||r.getAttribute('bit-theme-default')||'light';" +
        "var lt=r.getAttribute('bit-theme-light')||'light',dk=r.getAttribute('bit-theme-dark')||'dark';" +
        "if(cur==='system'){cur=(window.matchMedia&&matchMedia('(prefers-color-scheme:dark)').matches)?dk:lt;}" +
        "r.setAttribute('bit-theme',cur);})();";

    /// <summary>Full script element markup for convenience.</summary>
    public static string InlineHeadScript => $"<script>{InlineHeadScriptBody}</script>";
}
