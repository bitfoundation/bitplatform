var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The reflected properties of an element: identity, layout metrics and the content attributes
    // that mirror onto it. All of them plain property reads and writes, which is why they weigh
    // nothing on their own and should not be carried by an app that never asks for them.
    butil.elementState = {
        hasShadowRoot(element: HTMLElement) { return !!element.shadowRoot },
        accessKeyLabel(element: HTMLElement) { return element.accessKeyLabel },
        assignedSlotName(element: HTMLElement) { return element.assignedSlot?.name ?? null },
        getAutocapitalize(element: HTMLElement) { return element.autocapitalize },
        setAutocapitalize(element: HTMLElement, value: string) { element.autocapitalize = value },
        // Undefined where the feature does not exist, and an undefined result comes back as a JSON
        // null that has no bool to deserialize into - so answer false, which is how those engines behave.
        getAutocorrect(element: HTMLElement) { return (element as any).autocorrect ?? false },
        setAutocorrect(element: HTMLElement, value: boolean) { (element as any).autocorrect = value },
        getAutofocus(element: HTMLElement) { return element.autofocus },
        setAutofocus(element: HTMLElement, value: boolean) { element.autofocus = value },
        childElementCount(element: HTMLElement) { return element.childElementCount },
        // Chromium-only; 1 is the value every other engine behaves as if it had.
        currentCSSZoom(element: HTMLElement) { return (element as any).currentCSSZoom ?? 1 },
        getDraggable(element: HTMLElement) { return element.draggable },
        setDraggable(element: HTMLElement, value: boolean) { element.draggable = value },
        getElementTiming(element: HTMLElement) { return (element as any).elementTiming ?? element.getAttribute('elementtiming') },
        setElementTiming(element: HTMLElement, value: string) { element.setAttribute('elementtiming', value) },
        getLang(element: HTMLElement) { return element.lang },
        setLang(element: HTMLElement, value: string) { element.lang = value },
        localName(element: HTMLElement) { return element.localName },
        namespaceURI(element: HTMLElement) { return element.namespaceURI },
        getNonce(element: HTMLElement) { return element.nonce ?? null },
        setNonce(element: HTMLElement, value: string) { element.nonce = value },
        offsetParentTagName(element: HTMLElement) { return element.offsetParent?.tagName ?? null },
        getOuterText(element: HTMLElement) { return element.outerText },
        setOuterText(element: HTMLElement, value: string) { element.outerText = value },
        getPart(element: HTMLElement) { return Array.from(element.part) },
        setPart(element: HTMLElement, value: string) { element.setAttribute('part', value) },
        getPopover(element: HTMLElement) { return element.popover ?? null },
        setPopover(element: HTMLElement, value: string) { element.popover = value },
        prefix(element: HTMLElement) { return element.prefix },
        // Firefox-only, and its definition everywhere else is the difference of the two box widths.
        scrollLeftMax(element: HTMLElement) { return (element as any).scrollLeftMax ?? (element.scrollWidth - element.clientWidth) },
        scrollTopMax(element: HTMLElement) { return (element as any).scrollTopMax ?? (element.scrollHeight - element.clientHeight) },
        getSlot(element: HTMLElement) { return element.slot },
        setSlot(element: HTMLElement, value: string) { element.slot = value },
        getSpellcheck(element: HTMLElement) { return element.spellcheck },
        setSpellcheck(element: HTMLElement, value: boolean) { element.spellcheck = value },
        getTitle(element: HTMLElement) { return element.title },
        setTitle(element: HTMLElement, value: string) { element.title = value },
        getTranslate(element: HTMLElement) { return element.translate },
        setTranslate(element: HTMLElement, value: boolean) { element.translate = value },
        getVirtualKeyboardPolicy(element: HTMLElement) { return (element as any).virtualKeyboardPolicy ?? null },
        setVirtualKeyboardPolicy(element: HTMLElement, value: string) { (element as any).virtualKeyboardPolicy = value },
        getWritingSuggestions(element: HTMLElement) { return (element as any).writingSuggestions ?? null },
        setWritingSuggestions(element: HTMLElement, value: string) { (element as any).writingSuggestions = value },
    };
}(BitButil));
