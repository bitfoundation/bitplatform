// Page-side probe of the Bswup harness. Loaded before bit-bswup.js on every harness page.
//
//  - harness.loads counts the documents this tab loaded (sessionStorage survives reloads), mirrored on
//    <html data-harness-loads>, so a test can tell "the page reloaded" from "Blazor started in place"
//    with a locator assertion that keeps retrying across the navigation.
//  - harnessBswupHandler records every Bswup message of this document. The Blazor Web App passes it to
//    BswupProgress as its Handler (called after the built-in handling); the standalone app calls it
//    from its own bitBswupHandler.
//  - <html data-harness-controlled> follows navigator.serviceWorker.controller.
(function () {
    var key = 'bit-bswup-harness-loads';
    var loads = Number(sessionStorage.getItem(key) || '0') + 1;
    sessionStorage.setItem(key, String(loads));

    var root = document.documentElement;
    root.setAttribute('data-harness-loads', String(loads));

    var harness = window.harness = { loads: loads, events: [] };

    window.harnessBswupHandler = function (type, data) {
        var entry = { type: type };
        if (data && typeof data === 'object') {
            ['firstInstall', 'percent', 'reason', 'fatal', 'version', 'url', 'message', 'status', 'index'].forEach(function (name) {
                if (data[name] !== undefined) entry[name] = data[name];
            });
            if (data.asset && typeof data.asset.url === 'string') entry.asset = data.asset.url;
        }
        harness.events.push(entry);
        root.setAttribute('data-harness-last-event', type);
    };

    function syncControlled() {
        var controlled = !!(navigator.serviceWorker && navigator.serviceWorker.controller);
        root.setAttribute('data-harness-controlled', controlled ? 'true' : 'false');
    }

    syncControlled();
    if (navigator.serviceWorker) navigator.serviceWorker.addEventListener('controllerchange', syncControlled);
}());
