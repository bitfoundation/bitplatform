//+:cnd:noEmit

import { App } from './App';

(function () {
    window.addEventListener('load', handleLoad);
    window.addEventListener('resize', setCssWindowSizes);

    window.addEventListener('message', handleMessage);
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', handleMessage);
    }

    function handleLoad() {
        setCssWindowSizes();
    }

    function handleMessage(e: MessageEvent) {
        // Enable publishing messages from JavaScript's `window.postMessage` or `client.postMessage` to the C# `PubSubService`.
        if (e.origin !== window.location.origin) return;

        if (e.data?.key === 'PUBLISH_MESSAGE') {
            App.publishMessage(e.data?.message, e.data?.payload);
        }
    }

    function setCssWindowSizes() {
        document.documentElement.style.setProperty('--win-width', `${window.innerWidth}px`);
        document.documentElement.style.setProperty('--win-height', `${window.innerHeight}px`);
    }
}());