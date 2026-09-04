//+:cnd:noEmit

import { App } from './App';

(function () {
    const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup
    if (!bswup) return;

    bswup.skipWaiting(); // If update is downloaded, activate it now while the app is not started yet.

    let hiddenAt: number | null = null;

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            hiddenAt = Date.now();
            return;
        }

        const awayFor = hiddenAt === null ? 0 : Date.now() - hiddenAt;
        hiddenAt = null;

        // Short switches away - another tab for a moment, a notification - are not "away". Reloading on those is the
        // interruption this whole policy exists to avoid.
        if (awayFor < 2 * 60 * 1000) return;

        // autoReload: the user has been gone long enough that reloading costs them nothing they were looking at.
        App.tryUpdatePwa(true);
    });
}());
