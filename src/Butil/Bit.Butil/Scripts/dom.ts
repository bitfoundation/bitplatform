var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The root a query runs against: the document, a handle, or a shadow root registered by the
    // shadowDom module (which stores its roots in the same domHandles registry).
    function rootOf(id: string | null) {
        return id ? butil.domHandles.nodeOf(id) : document;
    }

    butil.dom = {
        isSupported() { return typeof document.querySelector === 'function'; },

        query(rootId: string | null, selector: string) {
            const root = rootOf(rootId);
            if (!root) return null;
            try { return butil.domHandles.adopt(root.querySelector(selector)); } catch { return null; } // invalid selector
        },

        queryAll(rootId: string | null, selector: string) {
            const root = rootOf(rootId);
            if (!root) return [];
            try { return butil.domHandles.trackAll(root.querySelectorAll(selector)); } catch { return []; }
        },

        byId(elementId: string) { return butil.domHandles.adopt(document.getElementById(elementId)); },

        body() { return butil.domHandles.adopt(document.body); },
        head() { return butil.domHandles.adopt(document.head); },
        documentElement() { return butil.domHandles.adopt(document.documentElement); },

        create(tagName: string, namespaceUri: string | null) {
            try {
                const element = namespaceUri
                    ? document.createElementNS(namespaceUri, tagName)
                    : document.createElement(tagName);
                return butil.domHandles.adopt(element);
            } catch {
                return null;   // not a valid element name
            }
        },

        // --- traversal ------------------------------------------------------------------------
        // Element-wise rather than node-wise: parentElement skips the document, and the *Element
        // siblings skip the whitespace text nodes that make raw node traversal so tedious.
        parent(id: string) { return butil.domHandles.adopt(butil.domHandles.nodeOf(id)?.parentElement); },
        children(id: string) { return butil.domHandles.trackAll(butil.domHandles.nodeOf(id)?.children); },
        firstChild(id: string) { return butil.domHandles.adopt(butil.domHandles.nodeOf(id)?.firstElementChild); },
        lastChild(id: string) { return butil.domHandles.adopt(butil.domHandles.nodeOf(id)?.lastElementChild); },
        nextSibling(id: string) { return butil.domHandles.adopt(butil.domHandles.nodeOf(id)?.nextElementSibling); },
        previousSibling(id: string) { return butil.domHandles.adopt(butil.domHandles.nodeOf(id)?.previousElementSibling); },
        closest(id: string, selector: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node?.closest) return null;
            try { return butil.domHandles.adopt(node.closest(selector)); } catch { return null; }
        },

        // --- placement ------------------------------------------------------------------------
        // The placement operations all catch: a move the tree refuses - putting an element inside
        // its own descendant, or inside itself - throws a HierarchyRequestError, and these answer
        // false like every other refusal in this module rather than throwing across the interop
        // boundary out of a method whose result is a bool.
        append(parentId: string, childId: string) {
            const parent = butil.domHandles.nodeOf(parentId);
            const child = butil.domHandles.nodeOf(childId);
            if (!parent || !child) return false;
            try { parent.appendChild(child); return true; } catch { return false; }
        },

        appendTo(element: any, childId: string) {
            const child = butil.domHandles.nodeOf(childId);
            if (!element || !child) return false;
            try { element.appendChild(child); return true; } catch { return false; }
        },

        prepend(parentId: string, childId: string) {
            const parent = butil.domHandles.nodeOf(parentId);
            const child = butil.domHandles.nodeOf(childId);
            if (!parent || !child?.nodeType) return false;
            try { parent.insertBefore(child, parent.firstChild); return true; } catch { return false; }
        },

        insertBefore(referenceId: string, childId: string) {
            const reference = butil.domHandles.nodeOf(referenceId);
            const child = butil.domHandles.nodeOf(childId);
            if (!reference?.parentNode || !child) return false;
            try { reference.parentNode.insertBefore(child, reference); return true; } catch { return false; }
        },

        remove(id: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node?.remove) return false;
            node.remove();
            return true;
        },

        // --- reading and writing --------------------------------------------------------------
        tagName(id: string) { return (butil.domHandles.nodeOf(id)?.tagName ?? '').toLowerCase(); },
        text(id: string) { return butil.domHandles.nodeOf(id)?.textContent ?? ''; },
        setText(id: string, value: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node) return false;
            node.textContent = value;
            return true;
        },
        html(id: string) { return butil.domHandles.nodeOf(id)?.innerHTML ?? ''; },
        setHtml(id: string, html: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node) return false;
            node.innerHTML = html;
            return true;
        },
        attribute(id: string, name: string) { return butil.domHandles.nodeOf(id)?.getAttribute?.(name) ?? null; },
        setAttribute(id: string, name: string, value: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node?.setAttribute) return false;
            try { node.setAttribute(name, value); return true; } catch { return false; } // invalid name
        },
        removeAttribute(id: string, name: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node?.removeAttribute) return false;
            node.removeAttribute(name);
            return true;
        },
        matches(id: string, selector: string) {
            const node = butil.domHandles.nodeOf(id);
            if (!node?.matches) return false;
            try { return node.matches(selector); } catch { return false; }
        }
    };
}(BitButil));
