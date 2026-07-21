/* Copy-to-clipboard buttons for code blocks.
   Decoration is done from JS (a MutationObserver re-decorates after every Blazor render or
   navigation) so the ~50 existing `.bb-code` blocks need no markup changes. The button is
   appended INSIDE the <pre> and absolutely positioned; Blazor's diffing never targets it
   because it renders the pre's static content wholesale. */
(function () {
    'use strict';

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

    function decorateAll(root) {
        (root.querySelectorAll ? root : document).querySelectorAll('pre.bb-code').forEach(decorate);
    }

    function start() {
        decorateAll(document);
        new MutationObserver(function () { decorateAll(document); })
            .observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
