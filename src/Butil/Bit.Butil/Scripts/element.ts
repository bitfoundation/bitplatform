var BitButil = BitButil || {};

(function (butil: any) {
    butil.element = {
        blur,
        getAttribute,
        getAttributeNames,
        getBoundingClientRect,
        hasAttribute,
        hasAttributes,
        hasPointerCapture,
        matches,
        releasePointerCapture,
        remove,
        removeAttribute,
        requestFullScreen,
        requestPointerLock,
        scroll,
        scrollBy,
        scrollIntoView,
        setAttribute,
        setPointerCapture,
        toggleAttribute,
        getAccessKey, setAccessKey,
        getClassName, setClassName,
        clientHeight,
        clientLeft,
        clientTop,
        clientWidth,
        getId, setId,
        getInnerHTML, setInnerHTML,
        getOuterHTML, setOuterHTML,
        scrollHeight,
        scrollLeft,
        scrollTop,
        scrollWidth,
        tagName
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

    function hasAttribute(element: HTMLElement, name: string) {
        return element.hasAttribute(name);
    }

    function hasAttributes(element: HTMLElement) {
        return element.hasAttributes();
    }

    function hasPointerCapture(element: HTMLElement, pointerId: number) {
        return element.hasPointerCapture(pointerId);
    }

    function matches(element: HTMLElement, selectors: string) {
        return element.matches(selectors);
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

    function requestPointerLock(element: HTMLElement) {
        return element.requestPointerLock();
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

    function toggleAttribute(element: HTMLElement, name: string, force?: boolean) {
        return element.toggleAttribute(name, force);
    }

    function getAccessKey(element: HTMLElement) {
        return element.accessKey;
    }

    function setAccessKey(element: HTMLElement, key: string) {
        element.accessKey = key;
    }

    function getClassName(element: HTMLElement) {
        return element.className;
    }

    function setClassName(element: HTMLElement, className: string) {
        element.className = className;
    }

    function clientHeight(element: HTMLElement) {
        return element.clientHeight;
    }

    function clientLeft(element: HTMLElement) {
        return element.clientLeft;
    }

    function clientTop(element: HTMLElement) {
        return element.clientTop;
    }

    function clientWidth(element: HTMLElement) {
        return element.clientWidth;
    }

    function getId(element: HTMLElement) {
        return element.id;
    }

    function setId(element: HTMLElement, id: string) {
        element.id = id;
    }

    function getInnerHTML(element: HTMLElement) {
        return element.innerHTML;
    }

    function setInnerHTML(element: HTMLElement, innerHTML: string) {
        element.innerHTML = innerHTML;
    }

    function getOuterHTML(element: HTMLElement) {
        return element.outerHTML;
    }

    function setOuterHTML(element: HTMLElement, outerHTML: string) {
        element.outerHTML = outerHTML;
    }

    function scrollHeight(element: HTMLElement) {
        return element.scrollHeight;
    }

    function scrollLeft(element: HTMLElement) {
        return element.scrollLeft;
    }

    function scrollTop(element: HTMLElement) {
        return element.scrollTop;
    }

    function scrollWidth(element: HTMLElement) {
        return element.scrollWidth;
    }

    function tagName(element: HTMLElement) {
        return element.tagName;
    }
}(BitButil));