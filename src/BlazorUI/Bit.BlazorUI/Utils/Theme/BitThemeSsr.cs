namespace Bit.BlazorUI;

/// <summary>
/// Optional first-paint theme bootstrap for apps that use <c>bit-theme-persist</c> and/or <c>bit-theme-system</c> on the document element.
/// Order matches <c>bit-theme.ts</c> <c>init</c>: base from attributes, then <c>bit-theme-system</c> (prefers-color-scheme), then <c>bit-theme-persist</c> (localStorage), then resolve stored <c>system</c>.
/// Emit <see cref="InlineHeadScript"/> at the start of <c>&lt;head&gt;</c> (before stylesheets) so the correct <c>bit-theme</c> attribute is set before first paint.
/// </summary>
public static class BitThemeSsr
{
    /// <summary>
    /// Inline script only (no script tag). Wrap in a script element in your host page or layout.
    /// </summary>
    public const string InlineHeadScriptBody =
        "(function(){var r=document.documentElement,k='bit-current-theme',lt=r.getAttribute('bit-theme-light')||'light'," +
        "dk=r.getAttribute('bit-theme-dark')||'dark',m=window.matchMedia&&matchMedia('(prefers-color-scheme:dark)').matches," +
        "base=r.getAttribute('bit-theme')||r.getAttribute('bit-theme-default')||'light';" +
        "if(r.hasAttribute('bit-theme-system')){base=m?dk:lt;}" +
        "var cur=base;if(r.hasAttribute('bit-theme-persist')){try{cur=localStorage.getItem(k)||base;}catch(e){}}" +
        "if(cur==='system'){cur=m?dk:lt;}" +
        "r.setAttribute('bit-theme',cur);})();";

    /// <summary>Full script element markup for convenience.</summary>
    public static string InlineHeadScript => $"<script>{InlineHeadScriptBody}</script>";
}
