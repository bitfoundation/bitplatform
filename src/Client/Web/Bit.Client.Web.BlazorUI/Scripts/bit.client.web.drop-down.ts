﻿class BitDropDown {
    private static getParentElementByClass(element: Element, className: string): Element | null {
        if (element.parentElement == null) {
            return null;
        }
        if (element.parentElement.classList.contains(className)) {
            return element.parentElement;
        } else {
            return BitDropDown.getParentElementByClass(element.parentElement, className);
        }
    }

    private static getChildElementByClass(element: Element, className: string): Element | null {
        for (var index = 0; index < element.children.length; index++) {
            if (element.children[index].classList.contains(className)) {
                return element.children[index];
            } else {
                BitDropDown.getChildElementByClass(element.children[index], className);
            }
        }
        return null;
    }

    private static checkElementHasClassLike(element: Element, value: string): boolean {
        for (var i = 0; i < element.classList.length; i++) {
            var classParts = element.classList[i].split('-');
            for (var partsIndex = 0; partsIndex < classParts.length; partsIndex++) {
                if (classParts[partsIndex] == value) {
                    return true;
                }
            }
        }
        return false;
    }

    static handleCalloutChangeState(eventArg: MouseEvent) {
        var clickedElement = eventArg.target as Element;
        var calloutParent = clickedElement.classList.contains('bit-cal-com')
            ? clickedElement
            : BitDropDown.getParentElementByClass(clickedElement, 'bit-cal-com');
        if (calloutParent != null) {
            if (BitDropDown.checkElementHasClassLike(calloutParent, 'disabled')) {
                return;
            }
            var callout = BitDropDown.getChildElementByClass(calloutParent, 'bit-callout');
            if (callout != null) {
                if (callout.hasAttribute('style')) {
                    callout.removeAttribute('style');
                } else {
                    callout.setAttribute('style', 'display: none;');
                }
            }
        } else {
            var allCallouts = document.getElementsByClassName('bit-callout');
            for (var index = 0; index < allCallouts.length; index++) {
                if (!allCallouts[index].hasAttribute('style')) {
                    allCallouts[index].setAttribute('style', 'display: none;');
                }
            }
        }
    }
}

document.addEventListener('click', e => {
    BitDropDown.handleCalloutChangeState(e);
})