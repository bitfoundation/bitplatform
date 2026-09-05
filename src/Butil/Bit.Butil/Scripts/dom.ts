var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Elements .NET is holding a handle to. Every DOM API in the rest of Butil is anchored to an
    // ElementReference Blazor already rendered; this module is the exception - it hands back
    // handles to elements Blazor knows nothing about, so it needs a registry of its own.
    const _nodes: { [id: string]: any } = {};

    function track(node: any) {
        if (!node) return null;
        const id = butil.utils.randomUUID();
        _nodes[id] = node;
        return { id, tagName: (node.tagName ?? '').toLowerCase() };
    }

    function trackAll(nodes: any) {
        return Array.from(nodes ?? []).map(track).filter(Boolean);
    }

    // The root a query runs against: the document, a handle, or a shadow root registered by the
    // shadowDom module (which stores its roots in this same registry).
    function rootOf(id: string | null) {
        return id ? _nodes[id] : document;
    }

    butil.dom = {
        isSupported() { return typeof document.querySelector === 'function'; },

        query(rootId: string | null, selector: string) {
            const root = rootOf(rootId);
            if (!root) return null;
            try { return track(root.querySelector(selector)); } catch { return null; } // invalid selector
        },

        queryAll(rootId: string | null, selector: string) {
            const root = rootOf(rootId);
            if (!root) return [];
            try { return trackAll(root.querySelectorAll(selector)); } catch { return []; }
        },

        byId(elementId: string) { return track(document.getElementById(elementId)); },

        body() { return track(document.body); },
        head() { return track(document.head); },
        documentElement() { return track(document.documentElement); },

        create(tagName: string, namespaceUri: string | null) {
            try {
                const element = namespaceUri
                    ? document.createElementNS(namespaceUri, tagName)
                    : document.createElement(tagName);
                return track(element);
            } catch {
                return null;   // not a valid element name
            }
        },

        // --- traversal ------------------------------------------------------------------------
        // Element-wise rather than node-wise: parentElement skips the document, and the *Element
        // siblings skip the whitespace text nodes that make raw node traversal so tedious.
        parent(id: string) { return track(_nodes[id]?.parentElement); },
        children(id: string) { return trackAll(_nodes[id]?.children); },
        firstChild(id: string) { return track(_nodes[id]?.firstElementChild); },
        lastChild(id: string) { return track(_nodes[id]?.lastElementChild); },
        nextSibling(id: string) { return track(_nodes[id]?.nextElementSibling); },
        previousSibling(id: string) { return track(_nodes[id]?.previousElementSibling); },
        closest(id: string, selector: string) {
            const node = _nodes[id];
            if (!node?.closest) return null;
            try { return track(node.closest(selector)); } catch { return null; }
        },

        // --- placement ------------------------------------------------------------------------
        // The placement operations all catch: a move the tree refuses - putting an element inside
        // its own descendant, or inside itself - throws a HierarchyRequestError, and these answer
        // false like every other refusal in this module rather than throwing across the interop
        // boundary out of a method whose result is a bool.
        append(parentId: string, childId: string) {
            const parent = _nodes[parentId];
            const child = _nodes[childId];
            if (!parent || !child) return false;
            try { parent.appendChild(child); return true; } catch { return false; }
        },

        appendTo(element: any, childId: string) {
            const child = _nodes[childId];
            if (!element || !child) return false;
            try { element.appendChild(child); return true; } catch { return false; }
        },

        prepend(parentId: string, childId: string) {
            const parent = _nodes[parentId];
            const child = _nodes[childId];
            if (!parent || !child?.nodeType) return false;
            try { parent.insertBefore(child, parent.firstChild); return true; } catch { return false; }
        },

        insertBefore(referenceId: string, childId: string) {
            const reference = _nodes[referenceId];
            const child = _nodes[childId];
            if (!reference?.parentNode || !child) return false;
            try { reference.parentNode.insertBefore(child, reference); return true; } catch { return false; }
        },

        remove(id: string) {
            const node = _nodes[id];
            if (!node?.remove) return false;
            node.remove();
            return true;
        },

        // --- reading and writing --------------------------------------------------------------
        tagName(id: string) { return (_nodes[id]?.tagName ?? '').toLowerCase(); },
        text(id: string) { return _nodes[id]?.textContent ?? ''; },
        setText(id: string, value: string) {
            const node = _nodes[id];
            if (!node) return false;
            node.textContent = value;
            return true;
        },
        html(id: string) { return _nodes[id]?.innerHTML ?? ''; },
        setHtml(id: string, html: string) {
            const node = _nodes[id];
            if (!node) return false;
            node.innerHTML = html;
            return true;
        },
        attribute(id: string, name: string) { return _nodes[id]?.getAttribute?.(name) ?? null; },
        setAttribute(id: string, name: string, value: string) {
            const node = _nodes[id];
            if (!node?.setAttribute) return false;
            try { node.setAttribute(name, value); return true; } catch { return false; } // invalid name
        },
        removeAttribute(id: string, name: string) {
            const node = _nodes[id];
            if (!node?.removeAttribute) return false;
            node.removeAttribute(name);
            return true;
        },
        matches(id: string, selector: string) {
            const node = _nodes[id];
            if (!node?.matches) return false;
            try { return node.matches(selector); } catch { return false; }
        },

        // Whether the element is still in the document. A handle survives its element being
        // removed - it just stops being connected, which is a different thing from being gone.
        isConnected(id: string) { return _nodes[id]?.isConnected === true; },

        // Stamps the attribute Blazor's own ElementReference lookup searches for, so an element
        // this module found or created can be passed to every ElementReference extension in the
        // rest of Butil.
        //
        // This is the one place Butil depends on a Blazor internal: element references resolve
        // through `document.querySelector('[_bl_<id>]')`. The coupling is deliberate and covered by
        // a test - if a future Blazor changes the convention, that test fails rather than a
        // consumer discovering it. Two consequences worth knowing: the lookup does not pierce
        // shadow roots, and it needs the element to be in the document.
        // Stamped once per element: an element that already carries a reference attribute - from an
        // earlier call, or from Blazor's own rendering - hands its id back instead of collecting
        // another attribute on every call.
        elementReferenceId(id: string) {
            const node = _nodes[id];
            if (!node?.setAttribute) return null;

            const existing = Array.from(node.attributes ?? [])
                .map((attr: any) => attr.name as string)
                .find(name => name.startsWith('_bl_'));
            if (existing) return existing.slice('_bl_'.length);

            const referenceId = butil.utils.randomUUID();
            node.setAttribute(`_bl_${referenceId}`, '');
            return referenceId;
        },

        // Drops the registry entry. The element itself is untouched - releasing a handle is not
        // removing an element, and a handle to something still on the page is simply forgotten.
        release(id: string) { delete _nodes[id]; },

        releaseAll() {
            for (const id of Object.keys(_nodes)) delete _nodes[id];
        },

        // For other modules (shadowDom): the live node behind an id, and a way to register one.
        nodeOf(id: string) { return _nodes[id]; },
        adopt(node: any) { return track(node); }
    };
}(BitButil));
