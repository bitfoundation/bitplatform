var BitButil = BitButil || {};

(function (butil: any) {
    butil.element = {
        blur,
        getAttribute,
        getAttributeNames,
        getBoundingClientRect,
        releasePointerCapture,
        remove,
        removeAttribute,
        requestFullScreen,
        scroll,
        scrollBy,
        scrollIntoView,
        setAttribute,
        setPointerCapture,
        getAccessKey,
        setAccessKey
    };

    function blur(element: HTMLElement) {
        element.blur();
    }

    function getAttribute(element: HTMLElement, name: string) {
        return element.getAttribute(name);
    }

    function getAttributeNames(element: HTMLElement) {
        return element.getAttributeNames();
    }

    function getBoundingClientRect(element: HTMLElement) {
        return element.getBoundingClientRect();
    }

    function releasePointerCapture(element: HTMLElement, pointerId: number) {
        element.releasePointerCapture(pointerId);
    }

    function remove(element: HTMLElement) {
        element.remove();
    }

    function removeAttribute(element: HTMLElement, name: string) {
        return element.removeAttribute(name);
    }

    function requestFullScreen(element: HTMLElement, options?: FullscreenOptions) {
        return element.requestFullscreen(options);
    }

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

    function setAttribute(element: HTMLElement, name: string, value: string) {
        return element.setAttribute(name, value);
    }

    function setPointerCapture(element: HTMLElement, pointerId: number) {
        element.setPointerCapture(pointerId);
    }

    function getAccessKey(element: HTMLElement) {
        return element.accessKey
    }

    function setAccessKey(element: HTMLElement, key: string) {
        element.accessKey = key;
    }

}(BitButil));