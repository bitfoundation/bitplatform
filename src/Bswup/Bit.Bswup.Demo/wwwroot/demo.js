// UI helpers for the Bswup docs site: theme, sidebar, code copy, and the Live Playground.
// Everything DOM-writing uses textContent / createElement - never innerHTML with data.
(function () {
    'use strict';

    const MAX_EVENTS = 200;
    const events = [];

    // ---------------------------------------------------------------- event log

    // Chained after the built-in bitBswupHandler via the data-bit-bswup-handler attribute
    // in index.html, so every Bswup lifecycle message lands here too.
    window.bswupDemoHandler = function (message, data) {
        record(message, describe(message, data));
    };

    function record(type, detail) {
        events.unshift({ time: new Date(), type: type, detail: detail });
        if (events.length > MAX_EVENTS) events.length = MAX_EVENTS;
        renderEvents();
    }

    function describe(message, data) {
        try {
            switch (message) {
                case 'DOWNLOAD_STARTED':
                    return `version: ${data && data.version ? data.version : '?'}${data && data.firstInstall === false ? ' (background update)' : ''}`;
                case 'DOWNLOAD_PROGRESS':
                    return `${Math.round(Number(data.percent) || 0)}% - ${data.asset ? data.asset.url : ''}`;
                case 'DOWNLOAD_FINISHED':
                    return data && data.firstInstall ? 'first install complete' : 'update staged, ready to activate';
                case 'STATE_CHANGED':
                    return `state: ${data && data.currentTarget ? data.currentTarget.state : '?'}`;
                case 'ACTIVATE':
                    return `version: ${data && data.version ? data.version : '?'}`;
                case 'ERROR':
                    return `[${data && data.reason ? data.reason : 'unknown'}] fatal: ${data ? data.fatal : '?'} - ${data && data.message ? data.message : ''}`;
                case 'UPDATE_CHECK_FAILED':
                    return `[${data && data.reason ? data.reason : 'unknown'}] ${data && data.message ? data.message : ''}`;
                default:
                    return '';
            }
        } catch (err) {
            return '';
        }
    }

    function renderEvents() {
        const el = document.getElementById('bswup-demo-events');
        if (!el) return;
        // Guard for the MutationObserver below: re-render only when the log actually grew,
        // otherwise our own DOM writes would re-trigger the observer forever.
        if (el.dataset.rendered === String(events.length)) return;
        el.dataset.rendered = String(events.length);

        el.textContent = '';
        if (events.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'event-empty';
            empty.textContent = 'No events yet - Bswup raises events on install, update checks, downloads, and activation.';
            el.append(empty);
            return;
        }
        for (const evt of events) {
            const li = document.createElement('li');
            const time = document.createElement('span');
            time.className = 'event-time';
            time.textContent = evt.time.toLocaleTimeString();
            const type = document.createElement('b');
            type.className = 'event-type';
            type.textContent = evt.type;
            const detail = document.createElement('span');
            detail.className = 'event-detail';
            detail.textContent = evt.detail;
            li.append(time, type, detail);
            el.append(li);
        }
    }

    // ---------------------------------------------------------------- playground

    function setText(id, value) {
        const el = document.getElementById(id);
        if (el) el.textContent = value;
    }

    async function refreshStatus() {
        if (!('serviceWorker' in navigator)) {
            setText('pg-sw-supported', 'not supported');
            return;
        }
        setText('pg-sw-supported', 'supported');
        setText('pg-sw-controller', navigator.serviceWorker.controller ? 'this page is controlled' : 'this page is NOT controlled');
        try {
            const reg = await navigator.serviceWorker.getRegistration();
            setText('pg-sw-scope', reg ? reg.scope : 'no registration');
            setText('pg-sw-state', reg && reg.active ? reg.active.state : 'none');
            setText('pg-sw-waiting', reg && reg.waiting ? 'yes - an update is staged' : 'no');
        } catch (err) {
            setText('pg-sw-scope', String(err));
        }
        try {
            const persisted = navigator.storage && navigator.storage.persisted ? await navigator.storage.persisted() : undefined;
            setText('pg-storage-persisted', persisted === undefined ? 'unknown' : (persisted ? 'persistent' : 'best-effort'));
        } catch (err) {
            setText('pg-storage-persisted', 'unknown');
        }
        try {
            const keys = await caches.keys();
            const list = document.getElementById('pg-cache-list');
            if (list) {
                list.textContent = '';
                if (keys.length === 0) {
                    const li = document.createElement('li');
                    li.textContent = '(no caches yet)';
                    list.append(li);
                }
                for (const key of keys) {
                    const li = document.createElement('li');
                    li.textContent = key;
                    list.append(li);
                }
            }
        } catch (err) { /* CacheStorage unavailable (e.g. some private modes) */ }
    }

    function checkForUpdate() {
        record('(playground)', 'BitBswup.checkForUpdate() called');
        if (window.BitBswup && BitBswup.checkForUpdate) BitBswup.checkForUpdate();
    }

    async function skipWaiting() {
        if (window.BitBswup && BitBswup.skipWaiting) {
            const result = await BitBswup.skipWaiting();
            record('(playground)', `BitBswup.skipWaiting() returned ${result} (${result ? 'a staged update is being activated' : 'no update was waiting'})`);
        }
    }

    async function persistStorage() {
        if (window.BitBswup && BitBswup.persistStorage) {
            const result = await BitBswup.persistStorage();
            record('(playground)', `BitBswup.persistStorage() resolved ${result}`);
            refreshStatus();
        }
    }

    function forceRefresh() {
        if (!confirm('Force refresh clears this app\'s caches, unregisters its service worker, and reloads the page. Continue?')) return;
        if (window.BitBswup && BitBswup.forceRefresh) BitBswup.forceRefresh();
    }

    // ---------------------------------------------------------------- theme & layout

    function toggleTheme() {
        const root = document.documentElement;
        const dark = root.getAttribute('data-theme') === 'dark';
        if (dark) {
            root.removeAttribute('data-theme');
        } else {
            root.setAttribute('data-theme', 'dark');
        }
        try { localStorage.setItem('bswup-demo-theme', dark ? 'light' : 'dark'); } catch (err) { }
    }

    function toggleSidebar() {
        document.body.classList.toggle('sidebar-open');
    }

    function closeSidebar() {
        document.body.classList.remove('sidebar-open');
    }

    function copyCode(button) {
        const block = button.closest('.code-block');
        const code = block ? block.querySelector('pre code') : null;
        if (!code) return;
        const original = button.textContent;
        const flash = text => {
            button.textContent = text;
            button.disabled = true;
            setTimeout(() => { button.textContent = original; button.disabled = false; }, 1500);
        };
        if (!navigator.clipboard || !navigator.clipboard.writeText) return flash('Copy failed');
        navigator.clipboard.writeText(code.textContent || '').then(() => flash('Copied!'), () => flash('Copy failed'));
    }

    // Blazor renders pages after this script runs (and on every navigation), so watch for the
    // playground elements appearing and (re)hydrate them. The dataset guards keep this cheap:
    // the callback exits immediately when nothing relevant changed.
    if (typeof MutationObserver !== 'undefined') {
        new MutationObserver(() => {
            renderEvents();
            const playground = document.getElementById('bswup-playground');
            if (playground && playground.dataset.init !== 'true') {
                playground.dataset.init = 'true';
                refreshStatus();
            }
        }).observe(document.documentElement, { childList: true, subtree: true });
    }

    window.BswupDemo = {
        toggleTheme: toggleTheme,
        toggleSidebar: toggleSidebar,
        closeSidebar: closeSidebar,
        copyCode: copyCode,
        refreshStatus: refreshStatus,
        checkForUpdate: checkForUpdate,
        skipWaiting: skipWaiting,
        persistStorage: persistStorage,
        forceRefresh: forceRefresh
    };
}());
