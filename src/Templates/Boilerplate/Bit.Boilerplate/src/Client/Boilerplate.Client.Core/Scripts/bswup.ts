//+:cnd:noEmit

(function () {
    const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup
    if (!bswup) return;

    bswup.skipWaiting(); // Use new service worker if available.

    // Detects if the app was in the background for over 2 minutes. Since setInterval usually pauses in the background on modern browsers and runs immediately upon resuming,
    // a lastRunInForeground older than 2 minutes indicates the app was likely not focused.
    let counter = 0;
    let lastTimeAppWasInForeground = new Date().getTime();

    setInterval(() => {
        const now = new Date().getTime();
        const resuming = now - lastTimeAppWasInForeground > 60 * 2 * 1000;

        counter++;
        if (counter % 60 === 0 /*Every 60 seconds*/ || resuming) {
            counter = 0;
            App.tryUpdatePwa(resuming);
        }

        if (document.visibilityState === 'visible') {
            lastTimeAppWasInForeground = now;
        }
    }, 1000);
}());