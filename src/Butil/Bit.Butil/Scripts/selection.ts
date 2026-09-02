var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: () => void } = {};

    function selection() {
        return typeof window.getSelection === 'function' ? window.getSelection() : null;
    }

    function firstRange(): Range | null {
        const current = selection();
        return current && current.rangeCount > 0 ? current.getRangeAt(0) : null;
    }

    function rectOf(rect: DOMRect) {
        return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
    }

    // Character offsets are counted over the element's text nodes only, which is what a caller who
    // asks "where in this text is the caret" means - element boundaries are not characters.
    function textNodesOf(element: Node) {
        const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
        const nodes: Text[] = [];
        let node: Node | null;
        while ((node = walker.nextNode())) nodes.push(node as Text);
        return nodes;
    }

    function pointAt(element: Node, offset: number): [Node, number] | null {
        let remaining = offset;
        for (const node of textNodesOf(element)) {
            const length = node.data.length;
            if (remaining <= length) return [node, remaining];
            remaining -= length;
        }
        return null;
    }

    function offsetOf(element: Node, node: Node, nodeOffset: number): number {
        let total = 0;
        for (const text of textNodesOf(element)) {
            if (text === node) return total + nodeOffset;
            total += text.data.length;
        }
        // The boundary is outside the element (or on an element node): report the end of its text
        // rather than a negative, so a caller reading a pair of offsets always gets a usable range.
        return total;
    }

    butil.selection = {
        isSupported() { return typeof window.getSelection === 'function'; },
        get() {
            const current = selection();
            if (!current) return null;
            return {
                text: current.toString(),
                isCollapsed: current.isCollapsed,
                rangeCount: current.rangeCount,
                type: (current as any).type ?? '',
                anchorOffset: current.anchorOffset,
                focusOffset: current.focusOffset
            };
        },
        getText() { return selection()?.toString() ?? ''; },
        // The selected markup rather than its text: cloneContents gives a fragment, and a detached
        // container is the only way to serialize a fragment to a string.
        getHtml() {
            const range = firstRange();
            if (!range) return '';
            const container = document.createElement('div');
            container.appendChild(range.cloneContents());
            return container.innerHTML;
        },
        // One rect per line box - a selection spanning wrapped text is not a rectangle.
        getRects() {
            const range = firstRange();
            if (!range) return [];
            const rects = range.getClientRects();
            const result = [];
            for (let i = 0; i < rects.length; i++) result.push(rectOf(rects[i]));
            return result;
        },
        getBoundingRect() {
            const range = firstRange();
            return range ? rectOf(range.getBoundingClientRect()) : null;
        },
        containsElement(element: Node, partly: boolean) {
            const current = selection();
            if (!current || !element) return false;
            return typeof current.containsNode === 'function' ? current.containsNode(element, partly) : false;
        },

        selectElement(element: Node) {
            const current = selection();
            if (!current || !element) return false;
            const range = document.createRange();
            range.selectNode(element);
            current.removeAllRanges();
            current.addRange(range);
            return true;
        },
        selectElementContents(element: Node) {
            const current = selection();
            if (!current || !element) return false;
            const range = document.createRange();
            range.selectNodeContents(element);
            current.removeAllRanges();
            current.addRange(range);
            return true;
        },
        selectRange(element: Node, start: number, end: number) {
            const current = selection();
            if (!current || !element) return false;
            const from = pointAt(element, start);
            const to = pointAt(element, end);
            if (!from || !to) return false;
            const range = document.createRange();
            range.setStart(from[0], from[1]);
            range.setEnd(to[0], to[1]);
            current.removeAllRanges();
            current.addRange(range);
            return true;
        },
        // Where the selection sits inside one element, in characters. What a text editor needs to
        // restore a caret after re-rendering its content.
        getRangeIn(element: Node) {
            const range = firstRange();
            if (!range || !element) return null;
            return {
                start: offsetOf(element, range.startContainer, range.startOffset),
                end: offsetOf(element, range.endContainer, range.endOffset)
            };
        },
        removeAll() {
            selection()?.removeAllRanges();
        },
        collapse(toStart: boolean) {
            const current = selection();
            if (!current || current.rangeCount === 0) return false;
            if (toStart) current.collapseToStart(); else current.collapseToEnd();
            return true;
        },

        // surroundContents throws when the selection's ends are in different elements (half a
        // paragraph and half of the next one), which is a normal thing for a user to have selected -
        // so it is reported as false rather than allowed to escape.
        surround(tagName: string, className: string | null, style: string | null) {
            const range = firstRange();
            if (!range) return false;
            let wrapper: HTMLElement;
            try { wrapper = document.createElement(tagName || 'span'); } catch { return false; }
            if (className) wrapper.className = className;
            if (style) wrapper.setAttribute('style', style);
            try { range.surroundContents(wrapper); return true; } catch { return false; }
        },
        replaceWithText(text: string) {
            const range = firstRange();
            if (!range) return false;
            range.deleteContents();
            range.insertNode(document.createTextNode(text ?? ''));
            return true;
        },
        deleteContents() {
            const range = firstRange();
            if (!range) return false;
            range.deleteContents();
            return true;
        },

        // Two spellings of the same feature: the standard one, and WebKit's older
        // caretRangeFromPoint. Both are read the same way once we have a node and an offset.
        isCaretFromPointSupported() {
            return typeof (document as any).caretPositionFromPoint === 'function'
                || typeof (document as any).caretRangeFromPoint === 'function';
        },
        caretFromPoint(x: number, y: number) {
            const doc = document as any;
            let node: Node | null = null;
            let offset = 0;

            if (typeof doc.caretPositionFromPoint === 'function') {
                const position = doc.caretPositionFromPoint(x, y);
                if (!position) return null;
                node = position.offsetNode;
                offset = position.offset;
            } else if (typeof doc.caretRangeFromPoint === 'function') {
                const range = doc.caretRangeFromPoint(x, y);
                if (!range) return null;
                node = range.startContainer;
                offset = range.startOffset;
            } else {
                return null;
            }

            const text = node?.nodeType === Node.TEXT_NODE ? (node as Text).data : '';
            return {
                offset,
                nodeName: node?.nodeName ?? '',
                text,
                elementTag: (node?.nodeType === Node.ELEMENT_NODE ? node : node?.parentElement)?.nodeName?.toLowerCase() ?? ''
            };
        },

        onChange(dotNetRef: any, method: string, id: string) {
            const handler = () => butil.utils.dispatch(dotNetRef, method, id);
            _listeners[id] = handler;
            document.addEventListener('selectionchange', handler);
        },
        offChange(id: string) {
            const handler = _listeners[id];
            if (!handler) return;
            delete _listeners[id];
            document.removeEventListener('selectionchange', handler);
        },
        disposeAll() {
            for (const id in _listeners) {
                document.removeEventListener('selectionchange', _listeners[id]);
                delete _listeners[id];
            }
        }
    };
}(BitButil));
