//+:cnd:noEmit

(function () {
    window.addEventListener('load', handleLoad);
    window.addEventListener('message', handleMessage);
    window.addEventListener('resize', setCssWindowSizes);

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', handleMessage);
    }

    function handleLoad() {
        setCssWindowSizes();
    }

    function handleMessage(e: MessageEvent) {
        // Enable publishing messages from JavaScript's `window.postMessage` or `client.postMessage` to the C# `PubSubService`.
        if (e.data.key === 'PUBLISH_MESSAGE') {
            App.publishMessage(e.data.message, e.data.payload);
        }
    }

    function setCssWindowSizes() {
        document.documentElement.style.setProperty('--win-width', `${window.innerWidth}px`);
        document.documentElement.style.setProperty('--win-height', `${window.innerHeight}px`);
    }
}());