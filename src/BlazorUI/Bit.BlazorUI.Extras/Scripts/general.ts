interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

// bit-trim: always
// The window size the --bit-env-*-avl variables of the stylesheet are computed from, which an app's own styles may read
// as well as the components of this package.
window.addEventListener('load', setBitEnvCssVars);
window.addEventListener('resize', setBitEnvCssVars);

function setBitEnvCssVars() {
    document.documentElement.style.setProperty('--bit-env-win-width', `${window.innerWidth}px`);
    document.documentElement.style.setProperty('--bit-env-win-height', `${window.innerHeight}px`);
}

