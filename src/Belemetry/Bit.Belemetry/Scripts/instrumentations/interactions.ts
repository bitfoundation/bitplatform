import type { InteractionsConfig } from '../config';
import { Features } from '../features';
import { Severity, type Attributes } from '../model';
import { createLog } from '../records';
import { guard, type InstrumentationContext, type Uninstall } from './context';

export const INTERACTION_SCOPE = 'bit.belemetry.interactions';

const INTERACTIVE = 'a,button,[role=button],input[type=submit],input[type=button],[data-belemetry-name]';

/** The nearest interactive ancestor of an event target, or null when there is none or it is under `[data-belemetry-ignore]`. */
export function resolveTarget(target: EventTarget | null): Element | null {
    let node = target as Node | null;
    if (node && node.nodeType !== 1) node = node.parentElement;
    const element = node as Element | null;
    if (!element || typeof element.closest !== 'function') return null;
    if (element.closest('[data-belemetry-ignore]')) return null;
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

/** The click attributes. Input values are never read; the text only with `captureText`. */
export function describeTarget(element: Element, eventType: string, config: InteractionsConfig): Attributes {
    const attributes: Attributes = {
        'event.type': eventType,
        'target.element': element.nodeName.toLowerCase(),
    };
    if (element.id) attributes['target.id'] = element.id;
    const name = element.getAttribute('data-belemetry-name') || element.getAttribute('aria-label') || element.getAttribute('name');
    if (name) attributes['target.name'] = name;
    if (config.captureText && element.nodeName !== 'INPUT') {
        const text = (element.textContent || '').replace(/\s+/g, ' ').trim();
        if (text) attributes['target.text'] = text.slice(0, Math.max(0, config.maxTextLength));
    }
    attributes['target.xpath'] = xpath(element);
    return attributes;
}

/** Capture-phase listeners for the configured events (default: click). */
export function installInteractions(ctx: InstrumentationContext): Uninstall {
    const config = ctx.config.interactions;
    const events = config.events && config.events.length ? config.events : ['click'];
    const handler = guard((event: Event) => {
        const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
        const element = resolveTarget(path.length ? path[0] : event.target);
        if (!element) return;
        const attributes = describeTarget(element, event.type, config);
        const label = (attributes['target.name'] || attributes['target.id'] || attributes['target.element']) as string;
        ctx.emit(createLog(INTERACTION_SCOPE, Features.Interactions, Severity.Info, event.type + ' ' + label, 'browser.user_interaction', attributes));
    });
    for (const type of events) document.addEventListener(type, handler, true);
    return () => {
        for (const type of events) document.removeEventListener(type, handler, true);
    };
}
