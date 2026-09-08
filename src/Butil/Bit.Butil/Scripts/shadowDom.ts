var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Shadow roots are registered in the dom module's registry, so one set of query and traversal
    // functions serves both - a shadow root answers querySelector exactly as the document does.
    function adopt(root: any) { return root ? butil.dom.adopt(root) : null; }

    function attach(host: any, open: boolean, delegatesFocus: boolean) {
        if (!host?.attachShadow) return null;
        try {
            return adopt(host.attachShadow({ mode: open ? 'open' : 'closed', delegatesFocus }));
        } catch {
            // Already has a root, or the element is one of the many that cannot have one at all -
            // <input>, <img>, <textarea> and the rest.
            return null;
        }
    }

    butil.shadowDom = {
        isSupported() { return typeof Element.prototype.attachShadow === 'function'; },

        attachToElement(host: any, open: boolean, delegatesFocus: boolean) { return attach(host, open, delegatesFocus); },
        attachToHandle(hostId: string, open: boolean, delegatesFocus: boolean) { return attach(butil.dom.nodeOf(hostId), open, delegatesFocus); },

        // Only an open root is reachable this way. A closed one is closed to the page as well as to
        // you - the element simply reports null, which is the whole point of closing it.
        fromElement(host: any) { return adopt(host?.shadowRoot); },
        fromHandle(hostId: string) { return adopt(butil.dom.nodeOf(hostId)?.shadowRoot); },

        host(rootId: string) { return adopt(butil.dom.nodeOf(rootId)?.host); },

        // No html/setHtml here: shadow roots live in the dom module's registry, so its own html and
        // setHtml already serve them, and ShadowRootHandle calls those.

        // Styles inside a shadow root are scoped to it: the page's stylesheet does not reach in, and
        // this does not leak out. That is what makes a shadow root worth attaching.
        addStyle(rootId: string, css: string) {
            const root = butil.dom.nodeOf(rootId);
            if (!root) return false;
            const style = document.createElement('style');
            style.textContent = css;
            root.appendChild(style);
            return true;
        },

        mode(rootId: string) { return butil.dom.nodeOf(rootId)?.mode ?? ''; }
    };
}(BitButil));
