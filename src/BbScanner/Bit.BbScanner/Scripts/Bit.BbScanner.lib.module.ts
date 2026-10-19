// Blazor JS initializer for the Bit.BbScanner package.
//
// The scanner's layout depends on its stylesheet (the video, the mask, the reticle and the controls are all
// absolutely positioned over each other), so it is injected automatically and applications do not have to
// remember a <link> tag. Injection is idempotent, and adding the link manually in the host page still works:
// the id and href checks make the second attempt a no-op.

const STYLESHEET_ID = 'bit-bbscanner-styles';
const STYLESHEET_HREF = '_content/Bit.BbScanner/bit-bbscanner.css';

function injectStylesheet(): void {
    if (typeof document === 'undefined') return;
    if (document.getElementById(STYLESHEET_ID)) return;
    if (document.querySelector(`link[href$="${STYLESHEET_HREF}"]`)) return;

    const link = document.createElement('link');
    link.id = STYLESHEET_ID;
    link.rel = 'stylesheet';
    link.href = STYLESHEET_HREF;
    document.head.appendChild(link);
}

// Blazor WebAssembly and Blazor Server standalone apps.
export function beforeStart(): void {
    injectStylesheet();
}

// Blazor Web App (per-page render modes).
export function beforeWebStart(): void {
    injectStylesheet();
}
