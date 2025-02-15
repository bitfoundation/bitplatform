interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

window.addEventListener('load', setBitEnvCssVars);
window.addEventListener('resize', setBitEnvCssVars);

function setBitEnvCssVars() {
    document.documentElement.style.setProperty('--bit-env-win-width', `${window.innerWidth}px`);
    document.documentElement.style.setProperty('--bit-env-win-height', `${window.innerHeight}px`);
}

