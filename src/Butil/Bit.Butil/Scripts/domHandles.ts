var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Elements .NET is holding a handle to. Every DOM API in the rest of Butil is anchored to an
    // ElementReference Blazor already rendered; the dom module is the exception - it hands back
    // handles to elements Blazor knows nothing about, so it needs a registry of its own.
    //
    // The registry is its own module because shadowDom stores its roots in it and resolves them
    // through it, and needs nothing else the dom module does: a lazy-loaded module file inlines its
    // dependencies, so keeping the two together would put every query and mutation in shadowDom's
    // download.
    const _nodes: { [id: string]: any } = {};

    butil.domHandles = {
        adopt: track,
        trackAll,
        nodeOf(id: string) { return _nodes[id]; },

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
    };

    function track(node: any) {
        if (!node) return null;
        const id = butil.utils.randomUUID();
        _nodes[id] = node;
        return { id, tagName: (node.tagName ?? '').toLowerCase() };
    }

    function trackAll(nodes: any) {
        return Array.from(nodes ?? []).map(track).filter(Boolean);
    }
}(BitButil));
