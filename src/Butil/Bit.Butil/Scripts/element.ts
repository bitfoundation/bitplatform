var BitButil = BitButil || {};

(function (butil: any) {
    butil.element = {
        blur(element: HTMLElement) { element.blur() },
        getAttribute(element: HTMLElement, name: string) { return element.getAttribute(name) },
        getAttributeNames(element: HTMLElement) { return element.getAttributeNames() },
        getBoundingClientRect(element: HTMLElement) { return element.getBoundingClientRect() },
        hasAttribute(element: HTMLElement, name: string) { return element.hasAttribute(name) },
        hasAttributes(element: HTMLElement) { return element.hasAttributes() },
        hasPointerCapture(element: HTMLElement, pointerId: number) { return element.hasPointerCapture(pointerId) },
        matches(element: HTMLElement, selectors: string) { return element.matches(selectors) },
        releasePointerCapture(element: HTMLElement, pointerId: number) { element.releasePointerCapture(pointerId) },
        remove(element: HTMLElement) { element.remove() },
        removeAttribute(element: HTMLElement, name: string) { element.removeAttribute(name) },
        requestFullScreen(element: HTMLElement, options?: FullscreenOptions) { return element.requestFullscreen(options) },
        requestPointerLock(element: HTMLElement) { return element.requestPointerLock() },
        scroll,
        scrollBy,
        scrollIntoView,
        setAttribute(element: HTMLElement, name: string, value: string) { return element.setAttribute(name, value) },
        setPointerCapture(element: HTMLElement, pointerId: number) { element.setPointerCapture(pointerId) },
        toggleAttribute(element: HTMLElement, name: string, force?: boolean) { return element.toggleAttribute(name, force) },
        getAccessKey(element: HTMLElement) { return element.accessKey },
        setAccessKey(element: HTMLElement, key: string) { element.accessKey = key },
        getClassName(element: HTMLElement) { return element.className },
        setClassName(element: HTMLElement, className: string) { element.className = className },
        clientHeight(element: HTMLElement) { return element.clientHeight },
        clientLeft(element: HTMLElement) { return element.clientLeft },
        clientTop(element: HTMLElement) { return element.clientTop },
        clientWidth(element: HTMLElement) { return element.clientWidth },
        getId(element: HTMLElement) { return element.id },
        setId(element: HTMLElement, id: string) { element.id = id },
        getInnerHTML(element: HTMLElement) { return element.innerHTML },
        setInnerHTML(element: HTMLElement, innerHTML: string) { element.innerHTML = innerHTML },
        getOuterHTML(element: HTMLElement) { return element.outerHTML },
        setOuterHTML(element: HTMLElement, outerHTML: string) { element.outerHTML = outerHTML },
        scrollHeight(element: HTMLElement) { return element.scrollHeight },
        scrollLeft(element: HTMLElement) { return element.scrollLeft },
        scrollTop(element: HTMLElement) { return element.scrollTop },
        scrollWidth(element: HTMLElement) { return element.scrollWidth },
        tagName(element: HTMLElement) { return element.tagName },
        getContentEditable(element: HTMLElement) { return element.contentEditable },
        setContentEditable(element: HTMLElement, value: string) { return element.contentEditable = value },
        isContentEditable(element: HTMLElement) { return element.isContentEditable },
        getDir(element: HTMLElement) { return element.dir },
        setDir(element: HTMLElement, value: string) { element.dir = value },
        getEnterKeyHint(element: HTMLElement) { return element.enterKeyHint },
        setEnterKeyHint(element: HTMLElement, value: string) { element.enterKeyHint = value },
        getHidden(element: HTMLElement) { return element.hidden },
        setHidden(element: HTMLElement, value: boolean) { element.hidden = value },
        getInert(element: HTMLElement) { return element.inert },
        setInert(element: HTMLElement, value: boolean) { element.inert = value },
        getInnerText(element: HTMLElement) { return element.innerText },
        setInnerText(element: HTMLElement, value: string) { element.innerText = value },
        getInputMode(element: HTMLElement) { return element.inputMode },
        setInputMode(element: HTMLElement, value: string) { element.inputMode = value },
        offsetHeight(element: HTMLElement) { return element.offsetHeight },
        offsetLeft(element: HTMLElement) { return element.offsetLeft },
        offsetTop(element: HTMLElement) { return element.offsetTop },
        offsetWidth(element: HTMLElement) { return element.offsetWidth },
        getTabIndex(element: HTMLElement) { return element.tabIndex },
        setTabIndex(element: HTMLElement, value: number) { element.tabIndex = value },
    };

    function scroll(element: HTMLElement, options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            element.scroll(options);
        } else {
            element.scroll(x, y);
        }
    }

    function scrollBy(element: HTMLElement, options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            element.scrollBy(options);
        } else {
            element.scrollBy(x, y);
        }
    }

    function scrollIntoView(element: HTMLElement, alignToTop?: boolean, options?: ScrollIntoViewOptions) {
        element.scrollIntoView(alignToTop ?? options);
    }
}(BitButil));