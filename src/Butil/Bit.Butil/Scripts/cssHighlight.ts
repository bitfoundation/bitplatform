var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const CSSNS = () => (window as any).CSS;

    // The Custom Highlight API.
    butil.cssHighlight = {
        isHighlightAvailable() { return typeof (window as any).Highlight === 'function' && !!CSSNS()?.highlights; },

        // The Custom Highlight API: ranges the browser paints through ::highlight(name), without a
        // single element being added to the document. The reason it exists - wrapping matches in
        // <mark> mutates the DOM, which breaks a Blazor diff and any layout measured around it.
        highlightText(name: string, element: any, search: string, caseSensitive: boolean) {
            if (!element || !search) return 0;
            const HighlightCtor = (window as any).Highlight;
            const highlights = CSSNS()?.highlights;
            if (typeof HighlightCtor !== 'function' || !highlights) return -1;

            const needle = caseSensitive ? search : search.toLowerCase();
            const ranges: Range[] = [];
            const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);

            while (walker.nextNode()) {
                const node = walker.currentNode as Text;
                const haystack = caseSensitive ? node.data : node.data.toLowerCase();

                let index = haystack.indexOf(needle);
                while (index >= 0) {
                    const range = document.createRange();
                    range.setStart(node, index);
                    range.setEnd(node, index + needle.length);
                    ranges.push(range);
                    index = haystack.indexOf(needle, index + needle.length);
                }
            }

            highlights.set(name, new HighlightCtor(...ranges));
            return ranges.length;
        },

        clearHighlight(name: string) {
            CSSNS()?.highlights?.delete(name);
        },
    };
}(BitButil));
