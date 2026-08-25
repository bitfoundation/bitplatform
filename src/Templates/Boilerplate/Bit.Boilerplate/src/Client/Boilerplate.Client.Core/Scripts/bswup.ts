//+:cnd:noEmit

import { App } from './App';

(function () {
    const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup
    if (!bswup) return;

    bswup.skipWaiting(); // Use new service worker if available.

    // Detects if the app was in the background for over 2 minutes. Since setInterval usually pauses in the background on modern browsers and runs immediately upon resuming,
    // a lastTimeAppWasInForeground older than 2 minutes indicates the app was likely not focused.
    let counter = 0;
    let lastTimeAppWasInForeground = new Date().getTime();

    setInterval(() => {
        const now = new Date().getTime();
        const isVisible = document.visibilityState === 'visible';

        // Only a tick that can see the page counts as a resume. Without that conjunct the condition stays true for
        // every subsequent tick of a long background period, turning a once-per-resume update check into a
        // once-per-second one on a device whose screen is off - and it also skips the reload path (autoReload) that
        // resuming exists to trigger, because the page is not there to reload.
        const resuming = isVisible && now - lastTimeAppWasInForeground > 60 * 2 * 1000;

        counter++;
        if (counter % 60 === 0 /*Every 60 seconds*/ || resuming) {
            counter = 0;
            App.tryUpdatePwa(resuming);
        }

        if (isVisible) {
            lastTimeAppWasInForeground = now;
        }
    }, 1000);
}());