// UI behaviour for the Bswup docs site: theme, navigation, the "On this page" outline,
// heading anchors, code copying, and the Live Playground.
//
// Two rules hold throughout:
//   1. Nothing writes DOM through innerHTML. Everything is textContent / createElement, so no
//      string on this page can ever become markup.
//   2. Nothing is wired with an inline onclick. The markup declares data-demo-action and the
//      single delegated listener at the bottom of this file dispatches it - inline handlers
//      would need script-src 'unsafe-inline', and this site keeps itself compatible with the
//      same strict Content-Security-Policy that Bswup's own scripts are designed for.
//      Delegation on document also survives Blazor replacing the elements, with no per-element
//      wiring to redo.
(function () {
    'use strict';

    const MAX_EVENTS = 200;
    const events = [];
    // Total events ever recorded. renderEvents keys off this, not events.length: once the
    // log reaches MAX_EVENTS its length stays pinned at the cap, and a length-based guard
    // would treat every later event as "nothing changed" and freeze the UI.
    let revision = 0;

    const reducedMotion = window.matchMedia ? window.matchMedia('(prefers-reduced-motion: reduce)') : null;

    function prefersReducedMotion() {
        return !!(reducedMotion && reducedMotion.matches);
    }

    // ---------------------------------------------------------------- event log

    // Chained after the built-in bitBswupHandler via the Handler parameter on BswupProgress in
    // the host document, so every Bswup lifecycle message lands here too.
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
    // suffix test mirrors the stylesheet's [data-theme$='dark'] rules, so a prefixed value
    // ('fluent-dark') is read the same as a plain 'dark'. The inline script in the host document
    // applies the persisted value before first paint; this only flips it afterwards.
    function toggleTheme() {
        const root = document.documentElement;
        const theme = (root.getAttribute('data-theme') || '').endsWith('dark') ? 'light' : 'dark';
        root.setAttribute('data-theme', theme);
        try { localStorage.setItem('bswup-demo-theme', theme); } catch (err) { }
        syncThemeColor(theme);
    }

    // The browser paints its own chrome (the address bar on Android, the title bar of an
    // installed PWA) from <meta name="theme-color">. The host document ships one tag per colour
    // scheme, which the OS preference selects between - so once the site's own toggle disagrees
    // with that preference, the chrome is painted from the wrong one until it is overridden here.
    function syncThemeColor(theme) {
        const color = theme === 'dark' ? '#141414' : '#FFFFFF';
        let tag = document.querySelector('meta[name="theme-color"][data-demo-theme-color]');
        if (!tag) {
            tag = document.createElement('meta');
            tag.setAttribute('name', 'theme-color');
            tag.setAttribute('data-demo-theme-color', '');
            document.head.appendChild(tag);
        }
        tag.setAttribute('content', color);
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
        window.scrollTo({ top: 0, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
        // Scrolling alone moves the viewport but not the keyboard, which would leave focus on a
        // button that is now off-screen at the bottom of the page.
        const main = document.getElementById('main-content');
        if (main) main.focus({ preventScroll: true });
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

    // ---------------------------------------------------------------- copy to clipboard

    function copyCode(button) {
        // The code sample's own frame, or the install command in the home page's hero - both
        // hold exactly one <code>, which is what gets copied.
        const scope = button.closest('.code-block, .hero-install');
        const code = scope ? scope.querySelector('code') : null;
        if (!code) return;

        const label = button.querySelector('.code-block-copy-label');
        const flash = (text, cssClass) => {
            if (label) label.textContent = text;
            button.classList.add(cssClass);
            button.disabled = true;
            setTimeout(() => {
                if (label) label.textContent = 'Copy';
                button.classList.remove('is-done', 'is-failed');
                button.disabled = false;
            }, 1600);
        };

        if (!navigator.clipboard || !navigator.clipboard.writeText) return flash('Failed', 'is-failed');

        navigator.clipboard.writeText(code.textContent || '').then(
            () => flash('Copied', 'is-done'),
            () => flash('Failed', 'is-failed'));
    }

    // ---------------------------------------------------------------- headings & outline

    // A heading needs an id before it can be linked to or listed in the outline. Existing ids
    // are left exactly as they are - the docs cross-link to them by hand (service-worker#mode,
    // javascript-api#checkforupdate), and regenerating one would break those links - so this
    // only fills in the headings that never had one.
    function slugify(text) {
        return (text || '')
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .slice(0, 64);
    }

    function ensureHeadingIds(headings) {
        const used = new Set();
        for (const heading of headings) {
            if (heading.id) { used.add(heading.id); continue; }
            const base = slugify(headingLabel(heading)) || 'section';
            let id = base;
            let n = 2;
            while (used.has(id) || document.getElementById(id)) id = `${base}-${n++}`;
            heading.id = id;
            used.add(id);
        }
    }

    // A "#" affordance on every heading, so a reader can link a colleague to the exact
    // paragraph rather than to the top of a 300-line reference page. Hidden until the heading
    // is hovered (see app.css) but always reachable by keyboard.
    function addHeadingAnchors(headings) {
        for (const heading of headings) {
            if (heading.dataset.anchored === 'true') continue;
            heading.dataset.anchored = 'true';

            const link = document.createElement('a');
            link.className = 'heading-anchor';
            link.href = `#${heading.id}`;
            link.setAttribute('aria-label', `Link to this section: ${headingLabel(heading)}`);

            // Built rather than assigned as markup, per the innerHTML rule at the top of this
            // file. The glyph repeats the icon set's 24x24 grid and 1.5 stroke.
            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.setAttribute('viewBox', '0 0 24 24');
            svg.setAttribute('fill', 'none');
            svg.setAttribute('stroke', 'currentColor');
            svg.setAttribute('stroke-width', '1.5');
            svg.setAttribute('stroke-linecap', 'round');
            svg.setAttribute('stroke-linejoin', 'round');
            svg.setAttribute('aria-hidden', 'true');
            svg.setAttribute('focusable', 'false');
            for (const d of [
                'M10 13.8a4 4 0 0 0 5.7 0l3-3a4 4 0 1 0-5.7-5.7l-1.3 1.3',
                'M14 10.2a4 4 0 0 0-5.7 0l-3 3a4 4 0 1 0 5.7 5.7l1.3-1.3']) {
                const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                path.setAttribute('d', d);
                svg.appendChild(path);
            }
            link.appendChild(svg);
            heading.appendChild(link);
        }
    }

    // A heading's own words, without the "#" affordance appended above - whose accessible name
    // ("Link to this section: ...") would otherwise be read as part of the heading.
    //
    // Taken from a clone rather than from the first child node: plenty of headings here are a
    // mix of text and markup ("The <code>reload()</code> callback"), and reading only the first
    // text node turns that one into "The".
    function headingLabel(heading) {
        const copy = heading.cloneNode(true);
        for (const anchor of copy.querySelectorAll('.heading-anchor')) anchor.remove();
        return (copy.textContent || '').replace(/\s+/g, ' ').trim();
    }

    // The headings currently listed in the outline, newest build first, so the scroll handler
    // does not re-query the document on every frame.
    let tocEntries = [];

    function buildToc() {
        const toc = document.getElementById('docs-toc');
        if (!toc) { tocEntries = []; return; }

        const article = document.querySelector('.page-content');
        if (!article) return;

        const headings = Array.from(article.querySelectorAll('h2, h3'))
            // A heading inside a card or a callout is a label on that component, not a station
            // on the page - listing it would describe the markup rather than the document.
            .filter(h => !h.closest('.callout, .feature-card, .path-card, .playground-card, .mcp-pane'));

        ensureHeadingIds(headings);
        addHeadingAnchors(headings);

        // The guard that keeps this out of the MutationObserver's feedback loop: writing the
        // outline mutates the document, which calls hydrate() again, which lands back here.
        const signature = headings.map(h => h.id).join('|');
        if (toc.dataset.builtFor === signature) return;
        toc.dataset.builtFor = signature;
        toc.textContent = '';

        // One entry is not an outline, it is a restatement of the page title.
        if (headings.length < 2) { tocEntries = []; return; }

        const title = document.createElement('div');
        title.className = 'docs-toc-title';
        title.textContent = 'On this page';

        const list = document.createElement('ul');
        list.className = 'docs-toc-list';

        tocEntries = headings.map(heading => {
            const item = document.createElement('li');
            if (heading.tagName === 'H3') item.className = 'docs-toc-sub';

            const link = document.createElement('a');
            link.className = 'docs-toc-link';
            link.href = `#${heading.id}`;
            link.textContent = headingLabel(heading);

            item.appendChild(link);
            list.appendChild(item);
            return { heading: heading, link: link };
        });

        toc.append(title, list);
        markCurrentTocEntry();
    }

    // Which heading the reader is under: the last one whose top has passed under the sticky
    // header. Read from the live layout rather than tracked with an IntersectionObserver,
    // because "the current section" is about what is above the fold, not about what is visible.
    function markCurrentTocEntry() {
        if (tocEntries.length === 0) return;

        const boundary = (document.querySelector('.header')?.getBoundingClientRect().height || 56) + 24;
        let current = tocEntries[0];

        for (const entry of tocEntries) {
            if (entry.heading.getBoundingClientRect().top <= boundary) current = entry;
            else break;
        }

        // At the very bottom of the page the last section is often too short to ever reach the
        // boundary, so it would never light up without this.
        const atBottom = window.innerHeight + window.scrollY >= document.body.offsetHeight - 8;
        if (atBottom) current = tocEntries[tocEntries.length - 1];

        for (const entry of tocEntries) {
            const isCurrent = entry === current;
            entry.link.classList.toggle('is-current', isCurrent);
            if (isCurrent) entry.link.setAttribute('aria-current', 'true');
            else entry.link.removeAttribute('aria-current');
        }
    }

    // ------------------------------------------------------- deep link landing

    // The browser scrolls to a #fragment against the document as it stands at that instant -
    // which here is the PRERENDERED page. Blazor then makes it interactive, and this script adds
    // the outline and a "#" affordance to every heading; any of that can change the article's
    // height, and the browser does not re-anchor a scroll it has already performed. The result
    // is a deep link that lands a screenful away from the heading it named.
    //
    // So the landing is corrected here, from the same hydrate() pass that caused the shift.
    // Two things stop it becoming a scroll hijack: it gives up the moment the reader touches the
    // page, and it stops on its own shortly after load.
    let userScrolled = false;
    const landedAt = Date.now();

    for (const event of ['wheel', 'touchstart', 'keydown']) {
        window.addEventListener(event, () => { userScrolled = true; }, { passive: true, once: true });
    }

    function correctHashLanding() {
        if (userScrolled || Date.now() - landedAt > 5000) return;

        const hash = location.hash;
        if (hash.length < 2) return;

        let target = null;
        // A fragment is not required to be a valid CSS selector (#10.6.0 is not), and
        // querySelector throws on one that is not.
        try { target = document.querySelector(hash); } catch (err) { return; }
        if (!target) return;

        // Where scroll-margin-top on the headings puts it: clear of the sticky header.
        const wanted = (document.querySelector('.header')?.getBoundingClientRect().height || 56) + 24;
        if (Math.abs(target.getBoundingClientRect().top - wanted) < 4) return;

        target.scrollIntoView({ behavior: 'auto', block: 'start' });
    }

    // ---------------------------------------------------------------- scroll

    function onScroll() {
        const button = document.querySelector('.go-to-top');
        if (button) button.classList.toggle('is-visible', window.scrollY > 600);
        markCurrentTocEntry();
    }

    let scrollQueued = false;

    function queueScroll() {
        if (scrollQueued) return;
        scrollQueued = true;
        requestAnimationFrame(() => { scrollQueued = false; onScroll(); });
    }

    // ---------------------------------------------------------------- hydration

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
        buildToc();
        correctHashLanding();
        onScroll();
    }

    // Called once up front because the host prerenders on the server: on a direct load of
    // /playground the markup is ALREADY in the document when this script runs, so no mutation
    // would ever announce it and the status cards would sit at their "..." placeholders forever.
    hydrate();
    syncThemeColor((document.documentElement.getAttribute('data-theme') || '').endsWith('dark') ? 'dark' : 'light');

    // Blazor still renders/replaces page content after this script runs - when the prerendered
    // page becomes interactive, and again on every client-side navigation - so keep watching.
    if (typeof MutationObserver !== 'undefined') {
        new MutationObserver(hydrate).observe(document.documentElement, { childList: true, subtree: true });
    }

    window.addEventListener('scroll', queueScroll, { passive: true });
    window.addEventListener('resize', queueScroll, { passive: true });

    // ---------------------------------------------------------------- CSP-safe wiring

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
        if (e.key === 'Escape' && document.body.classList.contains('nav-panel-open')) {
            closeNavPanel();
            const btn = document.querySelector('.menu-btn');
            if (btn) btn.focus();
            return;
        }

        // Ctrl/Cmd+K and "/" jump to the docs search, the two shortcuts a reader arriving from
        // any other documentation site will already try. "/" is ignored while a field has
        // focus, or it would swallow the character someone is typing into it.
        const inField = e.target instanceof Element &&
            (e.target.matches('input, textarea, select') || e.target.isContentEditable);

        const isSearchKey = ((e.ctrlKey || e.metaKey) && !e.altKey && e.key.toLowerCase() === 'k') ||
            (e.key === '/' && !inField && !e.ctrlKey && !e.metaKey && !e.altKey);

        if (!isSearchKey) return;

        const box = document.querySelector('[data-demo-search]');
        if (!box) return;
        e.preventDefault();
        box.focus();
        box.select();
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
