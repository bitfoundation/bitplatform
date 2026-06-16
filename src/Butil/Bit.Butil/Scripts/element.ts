var BitButil = BitButil || {};

(function (butil: any) {
    // Element-scoped event handlers, indexed by listenerId so element teardown can find them.
    const _elementHandlers: { [listenerId: string]: { element: HTMLElement, eventName: string, handler: any, options: any } } = {};

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
        subscribeEvent,
        unsubscribeEvent,
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

    function subscribeEvent(element: HTMLElement, elementId: string, eventName: string, methodName: string,
        dotNetRef: any, listenerId: string, argsMembers: string[], options: AddEventListenerOptions | boolean,
        preventDefault: boolean, stopPropagation: boolean) {
        if (!element) return;
        // When { once: true } is set the browser auto-detaches after the first call; mirror that by
        // dropping our tracking entry so the listenerId doesn't linger after it fires.
        const once = typeof options === 'object' && options.once === true;
        const handler = (e: any) => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            if (once) delete _elementHandlers[listenerId];
            butil.utils.dispatch(dotNetRef, methodName, listenerId, butil.events.mapEvent(e, argsMembers));
        };
        _elementHandlers[listenerId] = { element, eventName, handler, options };
        element.addEventListener(eventName, handler, options);
    }

    function unsubscribeEvent(elementId: string, eventName: string, listenerId: string, options: AddEventListenerOptions | boolean) {
        const entry = _elementHandlers[listenerId];
        if (!entry) return;
        delete _elementHandlers[listenerId];
        try {
            entry.element.removeEventListener(entry.eventName, entry.handler, entry.options);
        } catch { /* element may already be detached */ }
    }
}(BitButil));