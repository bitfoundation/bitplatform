/**
 * Checks for and applies updates if available.
 * Called by `WebAppUpdateService.cs` when the user clicks the app version in `AppShell.razor`
 * or when `ForceUpdateSnackbar.razor` appears after a forced update.
 */
async function tryUpdatePwa(autoReload?: boolean) {
    const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup
    const bswupProgress = (window as any).BitBswupProgress;

    if (await bswup.skipWaiting()) return;

    bswupProgress.config({ autoReload: autoReload ?? true });
    bswup.checkForUpdate();
}

// To minimize user-facing force updates, the following code attempts to update the PWA ASAP.
window.addEventListener('beforeunload', () => tryUpdatePwa(true));

let appIsInBackground = false; // App is in the background if visibilityState is 'hidden' for 20 seconds.
let visibilityChangeTimeout: number;
window.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
        visibilityChangeTimeout = window.setTimeout(() => {
            appIsInBackground = true;
        }, 20 * 1000 /* 20 seconds */);
    } else {
        clearTimeout(visibilityChangeTimeout);
        appIsInBackground = false;
    }
});

// This would download the updates, but reloads the app only if it's in the background.
setInterval(() => tryUpdatePwa(appIsInBackground), 60 * 1000 /* 1 minute */);