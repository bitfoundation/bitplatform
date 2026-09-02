var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Element-scoped event handlers, indexed by listenerId so element teardown can find them.
    const _elementHandlers: { [listenerId: string]: { element: HTMLElement, eventName: string, handler: any, options: any } } = {};

    butil.element = {
        after(element: HTMLElement, nodes: string[]) { element.after(...nodes) },
        append(element: HTMLElement, nodes: string[]) { element.append(...nodes) },
        ariaNotify,
        before(element: HTMLElement, nodes: string[]) { element.before(...nodes) },
        blur(element: HTMLElement) { element.blur() },
        checkVisibility,
        classListAdd(element: HTMLElement, tokens: string[]) { element.classList.add(...tokens) },
        classListContains(element: HTMLElement, token: string) { return element.classList.contains(token) },
        classListRemove(element: HTMLElement, tokens: string[]) { element.classList.remove(...tokens) },
        classListReplace(element: HTMLElement, oldToken: string, newToken: string) { return element.classList.replace(oldToken, newToken) },
        classListToggle(element: HTMLElement, token: string, force?: boolean) { return element.classList.toggle(token, force ?? undefined) },
        click(element: HTMLElement) { element.click() },
        closest(element: HTMLElement, selectors: string) { return !!element.closest(selectors) },
        focus(element: HTMLElement, options?: FocusOptions) { options ? element.focus(options) : element.focus() },
        getAttribute(element: HTMLElement, name: string) { return element.getAttribute(name) },
        getAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { return element.getAttributeNS(namespaceUri, localName) },
        getAttributeNames(element: HTMLElement) { return element.getAttributeNames() },
        getBoundingClientRect(element: HTMLElement) { return element.getBoundingClientRect() },
        getClassList(element: HTMLElement) { return Array.from(element.classList) },
        getClientRects(element: HTMLElement) { return Array.from(element.getClientRects()).map(r => ({ x: r.x, y: r.y, width: r.width, height: r.height })) },
        getData(element: HTMLElement, key: string) { return element.dataset[key] ?? null },
        getDataNames(element: HTMLElement) { return Object.keys(element.dataset) },
        getHTML,
        hasAttribute(element: HTMLElement, name: string) { return element.hasAttribute(name) },
        hasAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { return element.hasAttributeNS(namespaceUri, localName) },
        hasAttributes(element: HTMLElement) { return element.hasAttributes() },
        hasPointerCapture(element: HTMLElement, pointerId: number) { return element.hasPointerCapture(pointerId) },
        hasShadowRoot(element: HTMLElement) { return !!element.shadowRoot },
        hidePopover,
        insertAdjacentHTML(element: HTMLElement, position: string, html: string) { element.insertAdjacentHTML(position as InsertPosition, html) },
        insertAdjacentText(element: HTMLElement, position: string, text: string) { element.insertAdjacentText(position as InsertPosition, text) },
        matches(element: HTMLElement, selectors: string) { return element.matches(selectors) },
        prepend(element: HTMLElement, nodes: string[]) { element.prepend(...nodes) },
        querySelectorAllCount(element: HTMLElement, selectors: string) { return element.querySelectorAll(selectors).length },
        querySelectorMatches(element: HTMLElement, selectors: string) { return !!element.querySelector(selectors) },
        releasePointerCapture(element: HTMLElement, pointerId: number) { element.releasePointerCapture(pointerId) },
        remove(element: HTMLElement) { element.remove() },
        removeAttribute(element: HTMLElement, name: string) { element.removeAttribute(name) },
        removeAttributeNS(element: HTMLElement, namespaceUri: string, localName: string) { element.removeAttributeNS(namespaceUri, localName) },
        removeData(element: HTMLElement, key: string) { delete element.dataset[key] },
        replaceChildren(element: HTMLElement, nodes: string[]) { element.replaceChildren(...nodes) },
        replaceWith(element: HTMLElement, nodes: string[]) { element.replaceWith(...nodes) },
        requestFullScreen(element: HTMLElement, options?: FullscreenOptions) { return element.requestFullscreen(options) },
        requestPointerLock(element: HTMLElement) { return element.requestPointerLock() },
        scroll,
        scrollBy,
        scrollIntoView,
        scrollTo: scroll,
        setAttribute(element: HTMLElement, name: string, value: string) { return element.setAttribute(name, value) },
        setAttributeNS(element: HTMLElement, namespaceUri: string, qualifiedName: string, value: string) { element.setAttributeNS(namespaceUri, qualifiedName, value) },
        setData(element: HTMLElement, key: string, value: string) { element.dataset[key] = value },
        setHTML,
        setHTMLUnsafe,
        setPointerCapture(element: HTMLElement, pointerId: number) { element.setPointerCapture(pointerId) },
        showPopover,
        toggleAttribute(element: HTMLElement, name: string, force?: boolean) { return element.toggleAttribute(name, force) },
        togglePopover,
        getAccessKey(element: HTMLElement) { return element.accessKey },
        setAccessKey(element: HTMLElement, key: string) { element.accessKey = key },
        accessKeyLabel(element: HTMLElement) { return element.accessKeyLabel },
        getAria(element: HTMLElement, name: string) { return (element as any)[name] ?? null },
        setAria(element: HTMLElement, name: string, value: string) { (element as any)[name] = value },
        assignedSlotName(element: HTMLElement) { return element.assignedSlot?.name ?? null },
        getAutocapitalize(element: HTMLElement) { return element.autocapitalize },
        setAutocapitalize(element: HTMLElement, value: string) { element.autocapitalize = value },
        // Undefined where the feature does not exist, and an undefined result comes back as a JSON
        // null that has no bool to deserialize into - so answer false, which is how those engines behave.
        getAutocorrect(element: HTMLElement) { return (element as any).autocorrect ?? false },
        setAutocorrect(element: HTMLElement, value: boolean) { (element as any).autocorrect = value },
        getAutofocus(element: HTMLElement) { return element.autofocus },
        setAutofocus(element: HTMLElement, value: boolean) { element.autofocus = value },
        // The class attribute, not the className property - which on an SVG element is an
        // SVGAnimatedString, so neither readable nor writable as a string there.
        getClassName(element: HTMLElement) { return element.getAttribute('class') ?? '' },
        setClassName(element: HTMLElement, className: string) { element.setAttribute('class', className) },
        childElementCount(element: HTMLElement) { return element.childElementCount },
        clientHeight(element: HTMLElement) { return element.clientHeight },
        clientLeft(element: HTMLElement) { return element.clientLeft },
        clientTop(element: HTMLElement) { return element.clientTop },
        clientWidth(element: HTMLElement) { return element.clientWidth },
        // Chromium-only; 1 is the value every other engine behaves as if it had.
        currentCSSZoom(element: HTMLElement) { return (element as any).currentCSSZoom ?? 1 },
        getDraggable(element: HTMLElement) { return element.draggable },
        setDraggable(element: HTMLElement, value: boolean) { element.draggable = value },
        getElementTiming(element: HTMLElement) { return (element as any).elementTiming ?? element.getAttribute('elementtiming') },
        setElementTiming(element: HTMLElement, value: string) { element.setAttribute('elementtiming', value) },
        getId(element: HTMLElement) { return element.id },
        setId(element: HTMLElement, id: string) { element.id = id },
        getInnerHTML(element: HTMLElement) { return element.innerHTML },
        setInnerHTML(element: HTMLElement, innerHTML: string) { element.innerHTML = innerHTML },
        getLang(element: HTMLElement) { return element.lang },
        setLang(element: HTMLElement, value: string) { element.lang = value },
        localName(element: HTMLElement) { return element.localName },
        namespaceURI(element: HTMLElement) { return element.namespaceURI },
        getNonce(element: HTMLElement) { return element.nonce ?? null },
        setNonce(element: HTMLElement, value: string) { element.nonce = value },
        offsetParentTagName(element: HTMLElement) { return element.offsetParent?.tagName ?? null },
        getOuterHTML(element: HTMLElement) { return element.outerHTML },
        setOuterHTML(element: HTMLElement, outerHTML: string) { element.outerHTML = outerHTML },
        getOuterText(element: HTMLElement) { return element.outerText },
        setOuterText(element: HTMLElement, value: string) { element.outerText = value },
        getPart(element: HTMLElement) { return Array.from(element.part) },
        setPart(element: HTMLElement, value: string) { element.setAttribute('part', value) },
        getPopover(element: HTMLElement) { return element.popover ?? null },
        setPopover(element: HTMLElement, value: string) { element.popover = value },
        prefix(element: HTMLElement) { return element.prefix },
        scrollHeight(element: HTMLElement) { return element.scrollHeight },
        scrollLeft(element: HTMLElement) { return element.scrollLeft },
        setScrollLeft(element: HTMLElement, value: number) { element.scrollLeft = value },
        // Firefox-only, and its definition everywhere else is the difference of the two box widths.
        scrollLeftMax(element: HTMLElement) { return (element as any).scrollLeftMax ?? (element.scrollWidth - element.clientWidth) },
        scrollTop(element: HTMLElement) { return element.scrollTop },
        setScrollTop(element: HTMLElement, value: number) { element.scrollTop = value },
        scrollTopMax(element: HTMLElement) { return (element as any).scrollTopMax ?? (element.scrollHeight - element.clientHeight) },
        scrollWidth(element: HTMLElement) { return element.scrollWidth },
        getSlot(element: HTMLElement) { return element.slot },
        setSlot(element: HTMLElement, value: string) { element.slot = value },
        getSpellcheck(element: HTMLElement) { return element.spellcheck },
        setSpellcheck(element: HTMLElement, value: boolean) { element.spellcheck = value },
        getStyleProperty(element: HTMLElement, name: string) { return element.style.getPropertyValue(name) },
        setStyleProperty(element: HTMLElement, name: string, value: string, priority?: string) { element.style.setProperty(name, value, priority ?? undefined) },
        removeStyleProperty(element: HTMLElement, name: string) { return element.style.removeProperty(name) },
        getStyleText(element: HTMLElement) { return element.style.cssText },
        setStyleText(element: HTMLElement, value: string) { element.style.cssText = value },
        tagName(element: HTMLElement) { return element.tagName },
        getTitle(element: HTMLElement) { return element.title },
        setTitle(element: HTMLElement, value: string) { element.title = value },
        getTranslate(element: HTMLElement) { return element.translate },
        setTranslate(element: HTMLElement, value: boolean) { element.translate = value },
        getVirtualKeyboardPolicy(element: HTMLElement) { return (element as any).virtualKeyboardPolicy ?? null },
        setVirtualKeyboardPolicy(element: HTMLElement, value: string) { (element as any).virtualKeyboardPolicy = value },
        getWritingSuggestions(element: HTMLElement) { return (element as any).writingSuggestions ?? null },
        setWritingSuggestions(element: HTMLElement, value: string) { (element as any).writingSuggestions = value },
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

    // Experimental and Chromium-only: a no-op elsewhere rather than a throw, because an
    // announcement that does not happen is not a failure of the page that asked for it.
    function ariaNotify(element: HTMLElement, message: string, options?: any) {
        const notify = (element as any).ariaNotify;
        if (typeof notify === 'function') notify.call(element, message, options ?? undefined);
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
