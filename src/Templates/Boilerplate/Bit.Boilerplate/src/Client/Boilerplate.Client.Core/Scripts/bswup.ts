const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup

/**
 * Checks for and applies updates if available.
 * Called by `WebAppUpdateService.cs` when the user clicks the app version in `AppShell.razor`
 * or when `ForceUpdateSnackbar.razor` appears after a forced update.
 */
async function tryUpdatePwa(autoReload: boolean) {
    const bswupProgress = (window as any).BitBswupProgress;

    if (autoReload) {
        if (await bswup.skipWaiting()) return; // Use new service worker if available and reload the page.
    }

    bswupProgress.config({ autoReload: autoReload });
    bswup.checkForUpdate();
}

bswup?.skipWaiting(); // Use new service worker if available.

// Check for updates every minute.
let lastRunInForeground = new Date().getTime();
setInterval(() => {
    const now = new Date().getTime();
    const resuming = now - lastRunInForeground > 60 * 2 * 1000;
    if (document.visibilityState === 'visible') {
        lastRunInForeground = now;
    }
    // Detects if the app was in the background for over 2 minutes. Since setInterval usually pauses in the background on modern browsers and runs immediately upon resuming,
    // a lastRunInForeground older than 2 minutes indicates the app was likely not focused.
    tryUpdatePwa(resuming);
}, 60 * 1000);