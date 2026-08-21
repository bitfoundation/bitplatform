/* Progressive decoration for the site chrome, plus the two things a Blazor component cannot do
   for itself: listen at the document for a keyboard shortcut, and lock the page behind a dialog.

   The decoration - copy buttons on code blocks, anchor links on section headings, and the "on
   this page" rail - is done from here rather than from markup because each of them would
   otherwise be a change to every page: the ~85 `.bb-code` blocks and the ~200 sections of the
   documentation. A MutationObserver re-runs them after each Blazor render or navigation.

   Blazor's diffing never fights this. It compares its own render trees rather than the DOM, and
   tracks the children it owns by reference rather than by position, so a node appended here is
   never visited: neither the <pre> elements nor the rail's <nav> ever change shape in the render
   tree. The one case that could move an injected node - Blazor appending a NEW child after it -
   is handled by re-appending on the next pass, which is a move rather than a duplicate. */
(function () {
    'use strict';

    var SVG_NS = 'http://www.w3.org/2000/svg';

    function svg(paths, size) {
        var element = document.createElementNS(SVG_NS, 'svg');
        element.setAttribute('viewBox', '0 0 24 24');
        element.setAttribute('fill', 'none');
        element.setAttribute('stroke', 'currentColor');
        element.setAttribute('stroke-width', '2');
        element.setAttribute('stroke-linecap', 'round');
        element.setAttribute('stroke-linejoin', 'round');
        element.setAttribute('aria-hidden', 'true');
        if (size) {
            element.setAttribute('width', size);
            element.setAttribute('height', size);
        }
        element.innerHTML = paths;
        return element;
    }

    // ── Copy buttons ─────────────────────────────────────────────────────────────────────────

    var COPY_ICON =
        '<rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>' +
        '<path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>';

    var CHECK_ICON = '<polyline points="20 6 9 17 4 12"></polyline>';

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
        var existing = pre.querySelector('.bb-copy-btn');
        if (existing) {
            // Keep it the last child even if Blazor has appended something after it.
            if (existing !== pre.lastChild) pre.appendChild(existing);
            return;
        }

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'bb-copy-btn';
        button.title = 'Copy to clipboard';
        button.setAttribute('aria-label', 'Copy code to clipboard');
        button.appendChild(svg(COPY_ICON));

        var resetTimer = 0;
        button.addEventListener('click', function () {
            copy(codeTextOf(pre)).then(function () {
                button.classList.add('bb-copied');
                button.replaceChildren(svg(CHECK_ICON));
                button.title = 'Copied';
                clearTimeout(resetTimer);
                resetTimer = setTimeout(function () {
                    button.classList.remove('bb-copied');
                    button.replaceChildren(svg(COPY_ICON));
                    button.title = 'Copy to clipboard';
                }, 1600);
            });
        });

        pre.appendChild(button);
    }

    function decorateAll() {
        document.querySelectorAll('pre.bb-code').forEach(decorate);
    }

    // ── "On this page" rail, and the anchor links that share its headings ────────────────────

    /* The entries are the page's own section headings. A documentation page is built from
       `.bb-card` blocks whose `h3.bb-card-title` names the section, so those are the rail's rows;
       an `h2` (which the longer reference pages use to group several sections) outranks them and
       pushes the h3s in as sub-entries. */
    var HEADINGS = 'h2, h3.bb-card-title';

    var ANCHOR_ICON =
        '<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"></path>' +
        '<path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"></path>';

    function slugify(text) {
        return text.toLowerCase().trim()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .slice(0, 60);
    }

    /* The scroll target is the whole section, not just its heading: scrolling to the heading of a
       framed section would put its top padding above the viewport and the section would look
       clipped. */
    function targetOf(heading) {
        return heading.closest('.bb-card, .bb-section') || heading;
    }

    /* A link to the heading itself, so a reader can hand someone else the exact section. Kept as
       the heading's last child; if Blazor has since appended something after it, re-appending
       moves it back rather than adding a second one. */
    function anchorFor(heading, id) {
        var existing = heading.querySelector(':scope > .bb-anchor');
        if (existing) {
            existing.setAttribute('href', '#' + id);
            if (existing !== heading.lastChild) heading.appendChild(existing);
            return;
        }

        var link = document.createElement('a');
        link.className = 'bb-anchor';
        link.href = '#' + id;
        link.title = 'Link to this section';
        link.setAttribute('aria-label', 'Link to this section');
        link.appendChild(svg(ANCHOR_ICON));
        link.addEventListener('click', function (e) {
            e.preventDefault();
            scrollToId(id);
        });

        heading.appendChild(link);
    }

    function scrollToId(id) {
        var target = document.getElementById(id);
        if (target === null) return;

        target.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'start' });
        // Written without a jump: replaceState keeps the fragment shareable but does not add a
        // history entry for every heading the reader passes through.
        history.replaceState(history.state, '', '#' + id);
    }

    function prefersReducedMotion() {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
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

            anchorFor(heading, target.id);

            found.push({ id: target.id, title: title, sub: heading.tagName === 'H3' });
        });

        // With no h2 on the page the h3s ARE the top level rather than sub-entries - the rail
        // should reflect the page's own hierarchy, not the tag names it happened to use.
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
            // Not a docs page. The headings still get their anchors.
            var article = document.querySelector('article');
            if (article) headingsOf(article);
            railItems = [];
            return;
        }

        var container = document.querySelector('.bb-docs-content') || rail.parentElement;
        var items = headingsOf(container);
        var signature = items.map(function (i) { return i.id; }).join('|');

        // The observer that calls this fires for our own writes too; rebuilding only when the set
        // of sections actually changed is what keeps that from looping.
        if (rail.dataset.signature === signature) {
            railItems = items;
            return;
        }
        rail.dataset.signature = signature;
        railItems = items;
        railActiveId = null;

        if (items.length < 2) {
            // A single section is not an outline of anything.
            rail.replaceChildren();
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
                scrollToId(item.id);
            });

            li.appendChild(link);
            list.appendChild(li);
        });

        var top = document.createElement('button');
        top.type = 'button';
        top.className = 'bb-rail-top';
        top.appendChild(svg('<line x1="12" y1="19" x2="12" y2="5"></line><polyline points="5 12 12 5 19 12"></polyline>'));
        top.appendChild(document.createTextNode('Back to top'));
        top.addEventListener('click', goToTop);

        rail.replaceChildren(title, list, top);
        syncRail();
    }

    /* The active row is the last section whose top has passed under the header. The
       bottom-of-page special case exists because the final sections are often shorter than the
       remaining viewport, so without it the last one or two entries could never light up. */
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

    // ── Command palette ──────────────────────────────────────────────────────────────────────

    /* The shortcut has to be heard at the document, which is the one place a Blazor component
       cannot put a handler: @onkeydown only fires for keys pressed inside the element it is on,
       and the whole point of Ctrl+K is that the reader has not clicked anything first. The
       component registers itself here on its first render and is called back by reference. */
    var palette = null;

    function isEditable(element) {
        if (element === null) return false;
        var name = element.tagName;
        return name === 'INPUT' || name === 'TEXTAREA' || name === 'SELECT' || element.isContentEditable;
    }

    function onShortcut(e) {
        if (palette === null) return;

        var isK = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k';
        // "/" is the other convention, but only when the reader is not typing into something -
        // otherwise it would swallow a slash in a URL they are pasting into a demo.
        var isSlash = e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey && isEditable(document.activeElement) === false;

        if (isK === false && isSlash === false) return;

        e.preventDefault();
        palette.invokeMethodAsync('OpenFromShortcut');
    }

    function goToTop() {
        window.scrollTo({ top: 0, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
    }

    // ── Wiring ───────────────────────────────────────────────────────────────────────────────

    // ── Nav panel ────────────────────────────────────────────────────────────────────────────

    /* The documentation nav is taller than its own column, so on a deep-linked page the selected
       row can start scrolled out of sight - and a nav panel whose selection you cannot see is a
       list of links rather than a map of where you are.

       Only the PANEL is scrolled, never the page, and only when the row is actually out of view:
       scrollIntoView would scroll every ancestor including the document, which on arrival would
       throw away the scroll-to-top the router just performed. */
    var navScrolledFor = null;

    function syncSidebar() {
        var panel = document.querySelector('.bb-docs-sidebar');
        if (panel === null) {
            navScrolledFor = null;
            return;
        }

        var active = panel.querySelector('a.active');
        if (active === null) return;

        // Once per selected row: re-running it would fight a reader scrolling the panel by hand.
        var key = active.getAttribute('href');
        if (key === navScrolledFor) return;
        navScrolledFor = key;

        var panelBox = panel.getBoundingClientRect();
        var rowBox = active.getBoundingClientRect();

        if (rowBox.top >= panelBox.top && rowBox.bottom <= panelBox.bottom) return;

        panel.scrollTop += (rowBox.top - panelBox.top) - (panelBox.height - rowBox.height) / 2;
    }

    function refresh() {
        decorateAll();
        buildRail();
        syncSidebar();
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

        document.addEventListener('keydown', onShortcut);
    }

    window.bitBrouterSite = {
        goToTop: goToTop,

        /** Registers the palette component and reports whether to label the shortcut for a Mac. */
        registerPalette: function (reference) {
            palette = reference;
            return /Mac|iPhone|iPad|iPod/i.test(navigator.platform || navigator.userAgent);
        },

        unregisterPalette: function () {
            palette = null;
        },

        /** Holds the page still behind a modal, so scrolling the dialog does not scroll the page. */
        lockScroll: function (locked) {
            document.documentElement.style.overflow = locked ? 'hidden' : '';
        },

        /**
         * Keeps the palette's keyboard cursor inside its scrolling results list. The arrow keys
         * move the cursor in C#; nothing about a class change scrolls anything, so without this
         * the cursor walks off the bottom of the list and Enter opens a row nobody can see.
         *
         * Called after the render that moved the cursor, so the row is already marked active.
         */
        revealPaletteOption: function (index) {
            var list = document.querySelector('.bb-palette-results');
            if (list === null) return;

            var option = document.getElementById('bb-palette-option-' + index);
            if (option === null) return;

            var listBox = list.getBoundingClientRect();
            var optionBox = option.getBoundingClientRect();

            // Going up onto the first row of a group, the group's own heading comes with it -
            // scrolling to the row alone would leave the reader without the label above it.
            var above = option.previousElementSibling;
            var top = above !== null && above.classList.contains('bb-palette-group')
                ? above.getBoundingClientRect().top
                : optionBox.top;

            if (top < listBox.top) {
                list.scrollTop += top - listBox.top;
            } else if (optionBox.bottom > listBox.bottom) {
                list.scrollTop += optionBox.bottom - listBox.bottom;
            }
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
