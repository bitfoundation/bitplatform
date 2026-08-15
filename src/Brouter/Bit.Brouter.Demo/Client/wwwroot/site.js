/* Progressive decoration for the docs chrome: copy-to-clipboard buttons on code blocks, and the
   "on this page" rail.

   Both are done from JS rather than from markup because both would otherwise be a change to every
   page: the ~50 existing `.bb-code` blocks and the cards of every docs page. A MutationObserver
   re-runs them after each Blazor render or navigation. Blazor's diffing never fights this - it
   compares its own render trees, not the DOM, and neither the <pre> elements nor the rail's
   <nav> ever change shape in the render tree, so the nodes added here are simply never visited. */
(function () {
    'use strict';

    // ── Copy buttons ─────────────────────────────────────────────────────────────────────────

    function codeTextOf(pre) {
        var clone = pre.cloneNode(true);
        var button = clone.querySelector('.bb-copy-btn');
        if (button) button.remove();
        return clone.textContent;
    }

    function copy(text) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        }
        // Fallback for non-secure contexts.
        var area = document.createElement('textarea');
        area.value = text;
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        try { document.execCommand('copy'); } catch (_) { }
        area.remove();
        return Promise.resolve();
    }

    function decorate(pre) {
        if (pre.querySelector('.bb-copy-btn')) return;

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'bb-copy-btn';
        button.title = 'Copy to clipboard';
        button.setAttribute('aria-label', 'Copy code to clipboard');
        button.innerHTML =
            '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
            '<rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>' +
            '<path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>';

        var resetTimer = 0;
        button.addEventListener('click', function () {
            copy(codeTextOf(pre)).then(function () {
                button.classList.add('bb-copied');
                clearTimeout(resetTimer);
                resetTimer = setTimeout(function () { button.classList.remove('bb-copied'); }, 1500);
            });
        });

        pre.appendChild(button);
    }

    function decorateAll() {
        document.querySelectorAll('pre.bb-code').forEach(decorate);
    }

    // ── "On this page" rail ──────────────────────────────────────────────────────────────────

    /* The entries are the page's own section headings. A docs page is built from `.bb-card`
       blocks whose `h3.bb-card-title` is the section name, so those are the rail's rows; an `h2`
       (used by the longer prose pages) outranks them and pushes the h3s in as sub-entries. */
    var HEADINGS = 'h2, h3.bb-card-title';

    function slugify(text) {
        return text.toLowerCase().trim()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .slice(0, 60);
    }

    /* The scroll target is the whole card, not just its heading: scrolling to the heading would
       put the card's top padding above the viewport and the section would look clipped. */
    function targetOf(heading) {
        var card = heading.closest('.bb-card, .bb-section');
        return card || heading;
    }

    function headingsOf(container) {
        var found = [];
        var used = Object.create(null);
        container.querySelectorAll(HEADINGS).forEach(function (heading) {
            var title = heading.textContent.trim();
            if (title.length === 0) return;

            var target = targetOf(heading);
            if (target.id.length === 0) {
                // Two sections can legitimately share a title; the suffix keeps the anchors
                // distinct so the rail's second entry does not scroll to the first one.
                var slug = slugify(title) || 'section';
                var id = slug;
                for (var n = 2; used[id]; n++) id = slug + '-' + n;
                used[id] = true;
                target.id = id;
            } else {
                used[target.id] = true;
            }

            found.push({ id: target.id, title: title, sub: heading.tagName === 'H3' });
        });

        // With no h2 on the page the h3s are the top level, not sub-entries - the rail should
        // reflect the page's own hierarchy, not the tag names it happened to use.
        var hasTop = found.some(function (item) { return item.sub === false; });
        if (hasTop === false) {
            found.forEach(function (item) { item.sub = false; });
        }
        return found;
    }

    var railItems = [];
    var railActiveId = null;

    function buildRail() {
        var rail = document.querySelector('nav.bb-rail');
        if (rail === null) {
            railItems = [];
            return;
        }

        var container = document.querySelector('.bb-docs-content') || rail.parentElement;
        var items = headingsOf(container);
        var signature = items.map(function (i) { return i.id; }).join('|');

        // The observer that calls this fires for our own writes too; rebuilding only when the
        // set of sections actually changed is what keeps that from looping.
        if (rail.dataset.signature === signature) {
            railItems = items;
            return;
        }
        rail.dataset.signature = signature;
        railItems = items;
        railActiveId = null;

        if (items.length < 2) {
            // A single section is not an outline of anything.
            rail.textContent = '';
            railItems = [];
            return;
        }

        var title = document.createElement('p');
        title.className = 'bb-rail-title';
        title.textContent = 'On this page';

        var list = document.createElement('ul');
        items.forEach(function (item) {
            var li = document.createElement('li');
            if (item.sub) li.className = 'bb-rail-sub';
            li.dataset.for = item.id;

            var link = document.createElement('a');
            link.href = '#' + item.id;
            link.textContent = item.title;
            link.addEventListener('click', function (e) {
                e.preventDefault();
                var target = document.getElementById(item.id);
                if (target) {
                    target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    // Written without a jump: replaceState keeps the fragment shareable but does
                    // not add a history entry per heading the reader passes through.
                    history.replaceState(history.state, '', '#' + item.id);
                }
            });

            li.appendChild(link);
            list.appendChild(li);
        });

        rail.textContent = '';
        rail.appendChild(title);
        rail.appendChild(list);
        syncRail();
    }

    /* The active row is the last section whose top has passed under the header. The bottom-of-page
       special case exists because the final sections are often shorter than the remaining
       viewport, so without it the last one or two entries could never light up. */
    function syncRail() {
        if (railItems.length === 0) return;

        var offset = 96;
        var atBottom = window.innerHeight + window.scrollY >= document.body.scrollHeight - 8;
        var activeId = railItems[0].id;

        if (atBottom) {
            activeId = railItems[railItems.length - 1].id;
        } else {
            for (var i = 0; i < railItems.length; i++) {
                var element = document.getElementById(railItems[i].id);
                if (element && element.getBoundingClientRect().top <= offset) {
                    activeId = railItems[i].id;
                }
            }
        }

        if (activeId === railActiveId) return;
        railActiveId = activeId;

        document.querySelectorAll('nav.bb-rail li').forEach(function (li) {
            li.classList.toggle('bb-active', li.dataset.for === activeId);
        });
    }

    // ── Wiring ───────────────────────────────────────────────────────────────────────────────

    function refresh() {
        decorateAll();
        buildRail();
    }

    function start() {
        refresh();

        var pending = false;
        new MutationObserver(function () {
            if (pending) return;
            pending = true;
            // Coalesced to one pass per frame: a Blazor render can produce a burst of mutations,
            // and re-scanning the page for each of them would be the expensive part.
            requestAnimationFrame(function () {
                pending = false;
                refresh();
            });
        }).observe(document.body, { childList: true, subtree: true });

        var ticking = false;
        window.addEventListener('scroll', function () {
            if (ticking) return;
            ticking = true;
            requestAnimationFrame(function () {
                ticking = false;
                syncRail();
            });
        }, { passive: true });
    }

    window.bitBrouterSite = {
        goToTop: function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
