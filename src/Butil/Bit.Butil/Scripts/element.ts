var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The Element surface an ElementReference reaches directly. Deliberately only the core of it:
    // the aria members are elementAria, the DOM-manipulation ones elementDom, the reflected
    // properties elementState and the listeners elementEvents - the same split the C# extension
    // classes have. A page that only focuses an element downloads none of the other four.
    butil.element = {
        blur(element: HTMLElement) { element.blur() },
        checkVisibility,
        click(element: HTMLElement) { element.click() },
        closest(element: HTMLElement, selectors: string) { return !!element.closest(selectors) },
        focus(element: HTMLElement, options?: FocusOptions) { options ? element.focus(options) : element.focus() },
        getAttribute(element: HTMLElement, name: string) { return element.getAttribute(name) },
        getAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { return element.getAttributeNS(namespaceUri, localName) },
        getAttributeNames(element: HTMLElement) { return element.getAttributeNames() },
        getBoundingClientRect(element: HTMLElement) { return element.getBoundingClientRect() },
        hasAttribute(element: HTMLElement, name: string) { return element.hasAttribute(name) },
        hasAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { return element.hasAttributeNS(namespaceUri, localName) },
        hasAttributes(element: HTMLElement) { return element.hasAttributes() },
        hasPointerCapture(element: HTMLElement, pointerId: number) { return element.hasPointerCapture(pointerId) },
        matches(element: HTMLElement, selectors: string) { return element.matches(selectors) },
        releasePointerCapture(element: HTMLElement, pointerId: number) { element.releasePointerCapture(pointerId) },
        remove(element: HTMLElement) { element.remove() },
        removeAttribute(element: HTMLElement, name: string) { element.removeAttribute(name) },
        removeAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { element.removeAttributeNS(namespaceUri, localName) },
        requestFullScreen(element: HTMLElement, options?: FullscreenOptions) { return element.requestFullscreen(options) },
        requestPointerLock(element: HTMLElement) { return element.requestPointerLock() },
        scroll,
        scrollBy,
        scrollIntoView,
        scrollTo: scroll,
        setAttribute(element: HTMLElement, name: string, value: string) { return element.setAttribute(name, value) },
        setAttributeNS(element: HTMLElement, namespaceUri: string, qualifiedName: string, value: string) { element.setAttributeNS(namespaceUri, qualifiedName, value) },
        setPointerCapture(element: HTMLElement, pointerId: number) { element.setPointerCapture(pointerId) },
        toggleAttribute(element: HTMLElement, name: string, force?: boolean) { return element.toggleAttribute(name, force) },
        getAccessKey(element: HTMLElement) { return element.accessKey },
        setAccessKey(element: HTMLElement, key: string) { element.accessKey = key },
        // The class attribute, not the className property - which on an SVG element is an
        // SVGAnimatedString, so neither readable nor writable as a string there.
        getClassName(element: HTMLElement) { return element.getAttribute('class') ?? '' },
        setClassName(element: HTMLElement, className: string) { element.setAttribute('class', className) },
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
        setScrollLeft(element: HTMLElement, value: number) { element.scrollLeft = value },
        scrollTop(element: HTMLElement) { return element.scrollTop },
        setScrollTop(element: HTMLElement, value: number) { element.scrollTop = value },
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
        // No args from .NET means the no-argument C# overload: call the native no-arg form so the
        // browser applies its default (align-to-top) behavior. Passing null would instead be read
        // as an empty options object and change the alignment.
        if (alignToTop == null && options == null) {
            element.scrollIntoView();
            return;
        }
        element.scrollIntoView(alignToTop ?? options);
    }

    // checkVisibility shipped later than the rest of this module. Where it is missing, a laid-out
    // box is the whole answer, as it is natively: a visibility:hidden element still generates one,
    // and only counts as invisible when the caller opted into the visibility property.
    function checkVisibility(element: HTMLElement, options?: any) {
        const check = (element as any).checkVisibility;
        if (typeof check === 'function') return options ? check.call(element, options) : check.call(element);

        if (element.getClientRects().length === 0) return false;

        const checksVisibilityCss = options?.visibilityProperty === true || options?.checkVisibilityCSS === true;
        return checksVisibilityCss === false || getComputedStyle(element).visibility !== 'hidden';
    }
}(BitButil));
