// Clipboard helper for the "Copy" button of Shared/CodeSnippet.razor.
// It lives in a plain script (not a module) because the component calls it by name through
// IJSRuntime.InvokeAsync, and it is loaded from App.razor before blazor.web.js so the function
// exists by the time the WebAssembly runtime hydrates the prerendered markup.
window.bmCopyToClipboard = async function (text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch { /* fall through to legacy path */ }
    const ta = document.createElement('textarea');
    try {
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        return document.execCommand('copy');
    } catch {
        return false;
    } finally {
        // Always remove the textarea, even if focus/select/execCommand throws.
        if (ta.parentNode) document.body.removeChild(ta);
    }
};
