interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

// bit-trim: always
// The window size the --bit-env-*-avl variables of the stylesheet are computed from, which an app's own styles may read
// as well as the components of this package.
// In a namespace block, so the helper is no global of the page in the bundle.
namespace BitBlazorUI {
    window.addEventListener('load', setBitEnvCssVars);
    window.addEventListener('resize', setBitEnvCssVars);

    // Loaded after the page has - by the lazy scripts' initializer, which Blazor imports as it starts - there is no load
    // event left to wait for.
    if (document.readyState === 'complete') setBitEnvCssVars();

    function setBitEnvCssVars() {
        document.documentElement.style.setProperty('--bit-env-win-width', `${window.innerWidth}px`);
        document.documentElement.style.setProperty('--bit-env-win-height', `${window.innerHeight}px`);
    }
}
