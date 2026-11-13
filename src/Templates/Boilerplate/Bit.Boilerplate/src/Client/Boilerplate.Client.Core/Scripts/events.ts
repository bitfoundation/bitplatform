//+:cnd:noEmit

import { App } from './App';

(function () {
    window.addEventListener('message', handleMessage);
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', handleMessage);
    }

    window.addEventListener('storage', handleStorage);

    // Fires only in the other tabs, so they follow a sign-in or sign-out made in this one (See AuthManager).
    function handleStorage(e: StorageEvent) {
        if (e.storageArea !== window.localStorage) return;

        App.publishMessage('LOCAL_STORAGE_CHANGED', e.key);
    }

    // Enable publishing messages from JavaScript to the C# `PubSubService`.
    function handleMessage(e: MessageEvent) {
        /**
         * Security Check:
         * 1. If the message comes from 'window' (e.g., iframes or tabs), we must verify the origin
         *    to prevent Cross-Site Scripting (XSS) attacks. Window messages must have a non-empty
         *    origin that matches window.location.origin.
         * 2. Service Worker messages often have an empty origin; however, since they are registered
         *    on our domain, they are trusted by default when received via navigator.serviceWorker.
         */
        const isFromWindow = e.currentTarget === window;
        const isCrossOrigin = e.origin !== window.location.origin;
        if (isFromWindow && (isCrossOrigin || !e.origin)) return;

        if (e.data?.key === 'PUBLISH_MESSAGE') {
            App.publishMessage(e.data?.message, e.data?.payload);
        }
    }
}());