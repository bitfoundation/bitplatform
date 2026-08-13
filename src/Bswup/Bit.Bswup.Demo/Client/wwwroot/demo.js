// UI helpers for the Bswup docs site: theme, sidebar, code copy, and the Live Playground.
// Everything DOM-writing uses textContent / createElement - never innerHTML with data.
(function () {
    'use strict';

    const MAX_EVENTS = 200;
    const events = [];
    // Total events ever recorded. renderEvents keys off this, not events.length: once the
    // log reaches MAX_EVENTS its length stays pinned at the cap, and a length-based guard
    // would treat every later event as "nothing changed" and freeze the UI.
    let revision = 0;

    // ---------------------------------------------------------------- event log

    // Chained after the built-in bitBswupHandler via the data-bit-bswup-handler attribute
    // in index.html, so every Bswup lifecycle message lands here too.
    window.bswupDemoHandler = function (message, data) {
        record(message, describe(message, data));
    };

    function record(type, detail) {
        events.unshift({ time: new Date(), type: type, detail: detail });
        if (events.length > MAX_EVENTS) events.length = MAX_EVENTS;
        revision++;
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

    function renderEvent(evt) {
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
        return li;
    }

    function renderEvents() {
        const el = document.getElementById('bswup-demo-events');
        if (!el) return;
        // Guard for the MutationObserver below: re-render only when something new was
        // recorded, otherwise our own DOM writes would re-trigger the observer forever.
        // A missing dataset.rendered means a fresh element (Blazor replaced the subtree)
        // that still needs a full render even when the revision itself is unchanged - and so
        // does an element left EMPTY, which is what happens when Blazor makes the prerendered
        // page interactive: it synchronizes the server-rendered DOM back to the component's
        // (empty) render tree, discarding the entries this script had already written.
        const stale = el.dataset.rendered === undefined || el.childElementCount === 0;
        if (!stale && el.dataset.rendered === String(revision)) return;
        const prior = stale ? -1 : Number(el.dataset.rendered);
        el.dataset.rendered = String(revision);

        if (events.length === 0) {
            el.textContent = '';
            const empty = document.createElement('li');
            empty.className = 'event-empty';
            empty.textContent = 'No events yet - Bswup raises events on install, update checks, downloads, and activation.';
            el.append(empty);
            return;
        }

        // Prepend only the entries recorded since this element was last rendered (they are
        // the head of `events` - newest first). The list is an aria-live region, and a full
        // clear-and-rebuild would make screen readers re-announce the entire log on every
        // event; prepending keeps the announcement to just what is new. A fresh element or
        // a gap wider than the cap falls back to rendering everything.
        const fresh = prior < 0 || revision - prior >= events.length ? events.length : revision - prior;
        if (fresh === events.length) el.textContent = '';
        for (let i = fresh - 1; i >= 0; i--) el.prepend(renderEvent(events[i]));
        while (el.children.length > MAX_EVENTS) el.lastElementChild.remove();
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

    async function checkForUpdate() {
        record('(playground)', 'BitBswup.checkForUpdate() called');
        if (window.BitBswup && BitBswup.checkForUpdate) {
            try {
                await BitBswup.checkForUpdate();
            } catch (err) {
                // The registration-aware implementation reports failures through the
                // UPDATE_CHECK_FAILED event (logged above by bswupDemoHandler), but the
                // pre-registration fallback rejects directly when reg.update() fails
                // (e.g. offline) - record it instead of an unhandled rejection.
                record('(playground)', `BitBswup.checkForUpdate() rejected: ${err}`);
            }
        }
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

    // The theme lives in the data-theme attribute on <html>, written by this site alone. The
    // suffix test mirrors the stylesheet's [data-theme$='dark'] rules, which are carried over
    // from the BlazorUI demo, so a prefixed value ('fluent-dark') is read the same as a plain
    // 'dark'. The inline script in the host document applies the persisted value before first
    // paint; this only flips it afterwards.
    function toggleTheme() {
        const root = document.documentElement;
        const theme = (root.getAttribute('data-theme') || '').endsWith('dark') ? 'light' : 'dark';
        root.setAttribute('data-theme', theme);
        try { localStorage.setItem('bswup-demo-theme', theme); } catch (err) { }
    }

    // The drawer's open state lives in a body class (the stylesheet's only handle on it), so
    // the button's aria-expanded has to be written from here to stay truthful. Re-read from
    // the class rather than tracked separately, and re-applied from hydrate() as well, because
    // Blazor re-renders the header on every client-side navigation and restores the attribute
    // to the "false" the markup declares.
    function syncNavPanelState() {
        const open = document.body.classList.contains('nav-panel-open');
        const btn = document.querySelector('.menu-btn');
        if (btn) btn.setAttribute('aria-expanded', String(open));
    }

    function toggleNavPanel() {
        document.body.classList.toggle('nav-panel-open');
        syncNavPanelState();
    }

    function closeNavPanel() {
        document.body.classList.remove('nav-panel-open');
        syncNavPanelState();
    }

    function goToTop() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // ---------------------------------------------------------------- nav search

    // The last query typed, kept outside the DOM so it can be re-applied after Blazor replaces
    // the nav panel's markup (client-side navigation re-renders the NavLinks).
    let navQuery = '';

    function filterNav() {
        const list = document.querySelector('.nav-list');
        if (!list) return;
        const query = navQuery.trim().toLowerCase();

        // Walk the children in order so a group header can be hidden by the items that follow
        // it: the headers are siblings of the items, not their parents.
        let group = null;
        let groupMatches = 0;
        let total = 0;

        const flush = () => { if (group) group.classList.toggle('nav-hidden', groupMatches === 0); };

        for (const child of list.children) {
            if (child.classList.contains('nav-group')) {
                flush();
                group = child;
                groupMatches = 0;
            } else if (child.classList.contains('nav-item')) {
                const match = query === '' || (child.textContent || '').toLowerCase().includes(query);
                child.classList.toggle('nav-hidden', !match);
                if (match) { groupMatches++; total++; }
            }
        }
        flush();

        list.classList.toggle('is-empty', total === 0);
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

    // (Re)wires the parts of the page this script owns. The dataset guards keep it cheap: it
    // exits immediately when nothing relevant changed.
    function hydrate() {
        renderEvents();
        const playground = document.getElementById('bswup-playground');
        if (playground && playground.dataset.init !== 'true') {
            playground.dataset.init = 'true';
            refreshStatus();
        }
        // Blazor re-renders the nav panel on every client-side navigation, which restores the
        // markup to its unfiltered state and blanks the (unbound) search box. Put both back.
        const search = document.querySelector('[data-demo-nav-search]');
        if (search && search.value !== navQuery) search.value = navQuery;
        filterNav();
        syncNavPanelState();
    }

    // Called once up front because the host prerenders on the server: on a direct load of
    // /playground the markup is ALREADY in the document when this script runs, so no mutation
    // would ever announce it and the status cards would sit at their "…" placeholders forever.
    hydrate();

    // Blazor still renders/replaces page content after this script runs - when the prerendered
    // page becomes interactive, and again on every client-side navigation - so keep watching.
    if (typeof MutationObserver !== 'undefined') {
        new MutationObserver(hydrate).observe(document.documentElement, { childList: true, subtree: true });
    }

    // ---------------------------------------------------------------- CSP-safe wiring

    // The markup wires its controls through data-demo-action attributes and this single
    // delegated listener instead of inline onclick="..." attributes: inline handlers require
    // script-src 'unsafe-inline' (or per-page hashes), and this docs site keeps itself
    // compatible with the same strict Content-Security-Policy that Bswup's own scripts are
    // designed for. Delegation on document also survives Blazor re-renders replacing the
    // elements, with no per-element wiring to redo.
    const actions = {
        'copy-code': copyCode,
        'toggle-theme': toggleTheme,
        'toggle-nav-panel': toggleNavPanel,
        'close-nav-panel': closeNavPanel,
        'go-to-top': goToTop,
        'refresh-status': refreshStatus,
        'check-for-update': checkForUpdate,
        'skip-waiting': skipWaiting,
        'persist-storage': persistStorage,
        'force-refresh': forceRefresh
    };
    document.addEventListener('click', function (e) {
        const target = e.target instanceof Element ? e.target : null;
        if (!target) return;

        // Any in-app link in the chrome dismisses the mobile drawer: the nav panel's own links,
        // and the header's, which are reachable above the open drawer and would otherwise leave
        // it open over the next page.
        if (target.closest('.header a[href], .nav-panel a[href]')) closeNavPanel();

        const trigger = target.closest('[data-demo-action]');
        const action = trigger && actions[trigger.getAttribute('data-demo-action')];
        if (action) action(trigger);
    });

    // Escape dismisses the open drawer - it is a modal overlay on narrow screens (backdrop and
    // all), and focus goes back to the button that opened it so keyboard users are not dropped
    // at the top of the document.
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if (!document.body.classList.contains('nav-panel-open')) return;
        closeNavPanel();
        const btn = document.querySelector('.menu-btn');
        if (btn) btn.focus();
    });

    // Delegated for the same reason as the click handler: the search box is inside a Blazor
    // component and is replaced on every client-side navigation, so there is no stable element
    // to bind to.
    document.addEventListener('input', function (e) {
        const target = e.target instanceof Element ? e.target : null;
        if (!target || !target.matches('[data-demo-nav-search]')) return;
        navQuery = target.value || '';
        filterNav();
    });

    window.BswupDemo = {
        toggleTheme: toggleTheme,
        toggleNavPanel: toggleNavPanel,
        closeNavPanel: closeNavPanel,
        goToTop: goToTop,
        copyCode: copyCode,
        refreshStatus: refreshStatus,
        checkForUpdate: checkForUpdate,
        skipWaiting: skipWaiting,
        persistStorage: persistStorage,
        forceRefresh: forceRefresh
    };
}());
