var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The document selection, as seen from the window. Separate from the window module because a
    // page that reads or copies a selection is doing something quite different from one that asks
    // about the window itself, and the shadow-aware range mapping below is not small.
    butil.windowSelection = {
        getSelection,
        isComposedRangesSupported() { return typeof (window.getSelection() as any)?.getComposedRanges === 'function' },
        getComposedRanges,
        getSelectionText() { return window.getSelection()?.toString() ?? '' },
        clearSelection() { window.getSelection()?.removeAllRanges(); },
        selectElement(element: HTMLElement) {
            if (!element) return;
            // Inputs/textareas have their own select(), and trying to wrap them in a Range fails.
            if (typeof (element as any).select === 'function' && (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                (element as HTMLInputElement).select();
                return;
            }
            const sel = window.getSelection();
            if (!sel) return;
            sel.removeAllRanges();
            const range = document.createRange();
            try { range.selectNodeContents(element); sel.addRange(range); }
            catch { /* element may not be in the DOM */ }
        },
        async copySelection() {
            const text = window.getSelection()?.toString() ?? '';
            if (!text) return false;
            try { await navigator.clipboard.writeText(text); return true; }
            catch { return false; }
        },
    };

    function getSelection() {
        const sel = window.getSelection();
        if (!sel) return null;
        return {
            text: sel.toString(),
            isCollapsed: sel.isCollapsed,
            rangeCount: sel.rangeCount,
            type: (sel as any).type ?? null,
            anchorOffset: sel.anchorOffset,
            focusOffset: sel.focusOffset
        };
    }

    // An ordinary Selection reports the shadow host as its boundary, so a selection that starts or
    // ends inside a shadow tree cannot be described at all. getComposedRanges takes the roots it is
    // allowed to see into and reports the real boundary points within them.
    function getComposedRanges(hosts: HTMLElement[]) {
        const sel: any = window.getSelection();
        if (typeof sel?.getComposedRanges !== 'function') return [];

        const roots = (hosts ?? []).map(host => (host as any)?.shadowRoot).filter(root => !!root);

        let ranges: any[];
        try {
            ranges = sel.getComposedRanges({ shadowRoots: roots }) ?? [];
        } catch {
            // Older shape: the roots were passed as loose arguments rather than in an options bag.
            try { ranges = sel.getComposedRanges(...roots) ?? []; } catch { return []; }
        }

        return ranges.map((range: any) => ({
            startOffset: range.startOffset ?? 0,
            endOffset: range.endOffset ?? 0,
            collapsed: range.collapsed === true,
            // The containers themselves can't cross interop; what identifies them can.
            startContainerName: nodeName(range.startContainer),
            endContainerName: nodeName(range.endContainer),
            // True when a boundary sits inside one of the shadow roots we were given, i.e. when this
            // range is telling you something an ordinary Selection could not.
            crossesShadowBoundary: isInShadow(range.startContainer, roots) || isInShadow(range.endContainer, roots)
        }));
    }

    function nodeName(node: any) {
        if (!node) return '';
        return node.nodeType === Node.TEXT_NODE ? '#text' : (node.nodeName ?? '').toLowerCase();
    }

    function isInShadow(node: any, roots: any[]) {
        if (!node) return false;
        const root = node.getRootNode?.();
        return roots.some(candidate => candidate === root);
    }
}(BitButil));
