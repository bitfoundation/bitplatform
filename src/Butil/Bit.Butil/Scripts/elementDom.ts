var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Manipulating an element's content, classes, dataset, inline style and popover state.
    butil.elementDom = {
        after(element: HTMLElement, nodes: string[]) { element.after(...nodes) },
        append(element: HTMLElement, nodes: string[]) { element.append(...nodes) },
        before(element: HTMLElement, nodes: string[]) { element.before(...nodes) },
        classListAdd(element: HTMLElement, tokens: string[]) { element.classList.add(...tokens) },
        classListContains(element: HTMLElement, token: string) { return element.classList.contains(token) },
        classListRemove(element: HTMLElement, tokens: string[]) { element.classList.remove(...tokens) },
        classListReplace(element: HTMLElement, oldToken: string, newToken: string) { return element.classList.replace(oldToken, newToken) },
        classListToggle(element: HTMLElement, token: string, force?: boolean) { return element.classList.toggle(token, force ?? undefined) },
        getClassList(element: HTMLElement) { return Array.from(element.classList) },
        getClientRects(element: HTMLElement) { return Array.from(element.getClientRects()).map(r => ({ x: r.x, y: r.y, width: r.width, height: r.height })) },
        getData(element: HTMLElement, key: string) { return element.dataset[key] ?? null },
        getDataNames(element: HTMLElement) { return Object.keys(element.dataset) },
        getHTML,
        hidePopover,
        insertAdjacentHTML(element: HTMLElement, position: string, html: string) { element.insertAdjacentHTML(position as InsertPosition, html) },
        insertAdjacentText(element: HTMLElement, position: string, text: string) { element.insertAdjacentText(position as InsertPosition, text) },
        isMoveBeforeSupported() { return typeof (Element.prototype as any).moveBefore === 'function' },
        moveBefore(parent: HTMLElement, node: HTMLElement, reference: HTMLElement | null) {
            const move = (parent as any)?.moveBefore;
            if (typeof move !== 'function' || !node) return false;
            try {
                // Unlike insertBefore, this moves without disconnecting: an iframe keeps its document,
                // a video keeps playing, an animation keeps running, and focus stays where it was.
                move.call(parent, node, reference ?? null);
                return true;
            } catch {
                // Throws when the move would be across documents, or into the node's own subtree.
                return false;
            }
        },
        prepend(element: HTMLElement, nodes: string[]) { element.prepend(...nodes) },
        querySelectorAllCount(element: HTMLElement, selectors: string) { return element.querySelectorAll(selectors).length },
        querySelectorMatches(element: HTMLElement, selectors: string) { return !!element.querySelector(selectors) },
        removeData(element: HTMLElement, key: string) { delete element.dataset[key] },
        replaceChildren(element: HTMLElement, nodes: string[]) { element.replaceChildren(...nodes) },
        replaceWith(element: HTMLElement, nodes: string[]) { element.replaceWith(...nodes) },
        setData(element: HTMLElement, key: string, value: string) { element.dataset[key] = value },
        setHTML,
        setHTMLUnsafe,
        showPopover,
        togglePopover,
        getStyleProperty(element: HTMLElement, name: string) { return element.style.getPropertyValue(name) },
        setStyleProperty(element: HTMLElement, name: string, value: string, priority?: string) { element.style.setProperty(name, value, priority ?? undefined) },
        removeStyleProperty(element: HTMLElement, name: string) { return element.style.removeProperty(name) },
        getStyleText(element: HTMLElement) { return element.style.cssText },
        setStyleText(element: HTMLElement, value: string) { element.style.cssText = value },
    };

    function getHTML(element: HTMLElement, options?: any) {
        const get = (element as any).getHTML;
        return typeof get === 'function' ? get.call(element, options ?? undefined) : element.innerHTML;
    }

    // setHTML sanitizes; setHTMLUnsafe does not. Falling back from the sanitizing one to innerHTML
    // would turn a safe call into an unsafe one silently, so it reports the gap instead.
    function setHTML(element: HTMLElement, html: string, options?: any) {
        const set = (element as any).setHTML;
        if (typeof set !== 'function') throw new Error('Element.setHTML is not supported by this browser.');

        set.call(element, html, options ?? undefined);
    }

    function setHTMLUnsafe(element: HTMLElement, html: string) {
        const set = (element as any).setHTMLUnsafe;
        if (typeof set === 'function') set.call(element, html);
        else element.innerHTML = html;
    }

    function showPopover(element: HTMLElement) {
        if (typeof element.showPopover === 'function') element.showPopover();
    }

    function hidePopover(element: HTMLElement) {
        if (typeof element.hidePopover === 'function') element.hidePopover();
    }

    function togglePopover(element: HTMLElement, force?: boolean) {
        if (typeof element.togglePopover !== 'function') return false;

        return element.togglePopover(force ?? undefined);
    }
}(BitButil));
