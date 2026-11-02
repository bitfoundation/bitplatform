import type { InteractionsConfig } from '../config';
import { Features } from '../features';
import { Severity, type Attributes } from '../model';
import { createLog } from '../records';
import { guard, type InstrumentationContext, type Uninstall } from './context';

export const INTERACTION_SCOPE = 'bit.belemetry.interactions';

const INTERACTIVE = 'a,button,[role=button],input[type=submit],input[type=button],[data-bit-belemetry-name]';

/** The nearest interactive ancestor of an event target, or null when there is none or it is under `[data-bit-belemetry-ignore]`. */
export function resolveTarget(target: EventTarget | null): Element | null {
    let node = target as Node | null;
    if (node && node.nodeType !== 1) node = node.parentElement;
    const element = node as Element | null;
    if (!element || typeof element.closest !== 'function') return null;
    if (element.closest('[data-bit-belemetry-ignore]')) return null;
    return element.closest(INTERACTIVE);
}

/** An XPath to the element: anchored at the closest ancestor with an id, otherwise from the root. */
export function xpath(element: Element): string {
    const parts: string[] = [];
    let node: Element | null = element;
    for (let depth = 0; node && node.nodeType === 1 && depth < 32; depth++) {
        if (node.id) {
            parts.unshift('//*[@id="' + node.id + '"]');
            return parts.join('/');
        }
        let index = 1;
        for (let sibling = node.previousElementSibling; sibling; sibling = sibling.previousElementSibling) {
            if (sibling.nodeName === node.nodeName) index++;
        }
        parts.unshift(node.nodeName.toLowerCase() + '[' + index + ']');
        node = node.parentElement;
    }
    return '/' + parts.join('/');
}

function textOutsideIgnored(root: Node): string {
    let text = '';
    let node: Node | null = root.firstChild;
    while (node) {
        if (node.nodeType === 3) text += node.nodeValue || '';
        if (node.nodeType === 1 && node.firstChild && !(node as Element).hasAttribute('data-bit-belemetry-ignore')) {
            node = node.firstChild;
            continue;
        }
        while (node && node !== root && !node.nextSibling) node = node.parentNode;
        node = node && node !== root ? node.nextSibling : null;
    }
    return text;
}

function inIgnoredRegion(path: EventTarget[]): boolean {
    for (const target of path) {
        const element = target as Element;
        if (element.nodeType === 1 && element.hasAttribute('data-bit-belemetry-ignore')) return true;
    }
    return false;
}

/** The click attributes. Input values are never read; the text only with `captureText`. */
export function describeTarget(element: Element, eventType: string, config: InteractionsConfig): Attributes {
    const attributes: Attributes = {
        'event.type': eventType,
        'target.element': element.nodeName.toLowerCase(),
    };
    if (element.id) attributes['target.id'] = element.id;
    const name = element.getAttribute('data-bit-belemetry-name') || element.getAttribute('aria-label') || element.getAttribute('name');
    if (name) attributes['target.name'] = name;
    if (config.captureText && element.nodeName !== 'INPUT') {
        const raw = element.querySelector('[data-bit-belemetry-ignore]') ? textOutsideIgnored(element) : element.textContent;
        const text = (raw || '').replace(/\s+/g, ' ').trim();
        if (text) attributes['target.text'] = text.slice(0, Math.max(0, config.maxTextLength));
    }
    attributes['target.xpath'] = xpath(element);
    return attributes;
}

/** The event of a burst of clicks on one element: the user expected something to happen, and it did not. */
export const RAGE_CLICK_EVENT = 'browser.rage_click';

/** Clicks on one element within this window that make a rage click (the frustration signal of Datadog and Faro). */
export const RAGE_CLICKS = 3;
export const RAGE_WINDOW_MS = 1000;

/** Capture-phase listeners for the configured events (default: click), and the rage-click detection. */
export function installInteractions(ctx: InstrumentationContext): Uninstall {
    const config = ctx.config.interactions;
    const events = config.events && config.events.length ? config.events : ['click'];
    let burst: { element: Element; start: number; count: number } | null = null;
    const handler = guard((event: Event) => {
        const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
        if (inIgnoredRegion(path)) return;
        const element = resolveTarget(path.length ? path[0] : event.target);
        if (!element) return;
        const attributes = describeTarget(element, event.type, config);
        const label = (attributes['target.name'] || attributes['target.id'] || attributes['target.element']) as string;
        ctx.emit(createLog(INTERACTION_SCOPE, Features.Interactions, Severity.Info, event.type + ' ' + label, 'browser.user_interaction', attributes));

        if (event.type !== 'click' || config.rageClicks === false) return;
        const time = event.timeStamp || performance.now();
        if (burst && burst.element === element && time - burst.start <= RAGE_WINDOW_MS) burst.count++;
        else burst = { element, start: time, count: 1 };
        // Once per burst, at the click that makes it one.
        if (burst.count === RAGE_CLICKS) {
            const rage: Attributes = { ...attributes };
            rage['event.type'] = 'rage_click';
            rage['interaction.click_count'] = RAGE_CLICKS;
            ctx.emit(createLog(INTERACTION_SCOPE, Features.Interactions, Severity.Info, 'rage click ' + label, RAGE_CLICK_EVENT, rage));
        }
    });
    for (const type of events) document.addEventListener(type, handler, true);
    return () => {
        burst = null;
        for (const type of events) document.removeEventListener(type, handler, true);
    };
}
