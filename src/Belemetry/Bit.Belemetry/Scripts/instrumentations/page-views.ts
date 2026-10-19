import { now } from '../clock';
import { Features } from '../features';
import { onHidden } from '../lifecycle';
import { Severity, type Attributes } from '../model';
import { createLog } from '../records';
import { absoluteUrl, guard, type InstrumentationContext, type Uninstall } from './context';

export type NavigationType = 'initial' | 'push' | 'replace' | 'pop' | 'hash' | 'manual';

export interface PageViewOptions {
    name?: string | null;
    url?: string | null;
    attributes?: Attributes | null;
    /** Only emit when the URL differs from the last page view (replaceState, popstate, enhancedload). */
    onlyIfChanged?: boolean;
}

export const PAGE_VIEW_SCOPE = 'bit.belemetry.page_views';

/** The event name of the time spent on a page, sent when the next page view starts or the page is hidden. */
export const PAGE_VISIT_TIME_EVENT = 'browser.page_visit_time';

/** The attribute carrying a page visit's length, in milliseconds. */
export const PAGE_VISIT_DURATION = 'page.visit.duration';

// pushState followed by Blazor's enhancedload (or a router that pushes twice) must count once.
export const DEDUPE_MS = 100;

const state = { key: '', url: '', path: '', title: '', time: -Infinity, visitStart: -Infinity, initialSent: false };

/** Resets the module state (tests only). */
export function resetPageViewState(): void {
    state.key = '';
    state.url = '';
    state.path = '';
    state.title = '';
    state.time = -Infinity;
    state.visitStart = -Infinity;
    state.initialSent = false;
}

function pageKey(url: string, trackHash: boolean): string {
    return trackHash ? url : url.split('#')[0];
}

/**
 * Emits how long the current page was visited, from the page view (or the last time this ran) until `time`, and
 * starts the next visit at `time` - which `installPageViews` moves forward again when a hidden page is shown, so a
 * page hidden and shown again is two visits, with the hidden time in neither.
 */
export function trackPageVisitTime(ctx: InstrumentationContext, time = now()): boolean {
    if (!ctx.config.pageViews.trackVisitTime || !state.url || state.visitStart === -Infinity) return false;
    const duration = time - state.visitStart;
    state.visitStart = time;
    if (!(duration > 0)) return false;
    const attributes: Attributes = {};
    attributes['url.full'] = state.url;
    attributes['url.path'] = state.path;
    attributes['page.title'] = state.title;
    attributes[PAGE_VISIT_DURATION] = Math.round(duration);
    ctx.emit(createLog(PAGE_VIEW_SCOPE, Features.PageViews, Severity.Info, state.title || state.path, PAGE_VISIT_TIME_EVENT, attributes, time));
    return true;
}

/** Emits a page view unless it duplicates the previous one. Returns whether it was emitted. */
export function trackPageView(ctx: InstrumentationContext, type: NavigationType, options: PageViewOptions = {}): boolean {
    const url = absoluteUrl(options.url || location.href);
    const key = pageKey(url, ctx.config.pageViews.trackHashChanges);
    const time = now();
    if (key === state.key && (options.onlyIfChanged || time - state.time < DEDUPE_MS)) return false;
    // The previous page's visit ends where the next one starts.
    trackPageVisitTime(ctx, time);
    const referrer = state.url || document.referrer;
    state.key = key;
    state.url = url;
    state.time = time;
    state.visitStart = time;
    if (type === 'initial') state.initialSent = true;

    let path = url;
    try {
        path = new URL(url).pathname;
    } catch {
        // Keep the full URL.
    }
    const title = document.title || '';
    state.path = path;
    state.title = title;
    const attributes: Attributes = { ...(options.attributes || {}) };
    attributes['url.full'] = url;
    attributes['url.path'] = path;
    attributes['page.title'] = title;
    if (referrer) attributes['page.referrer'] = referrer;
    attributes['browser.navigation.type'] = type;
    ctx.emit(createLog(PAGE_VIEW_SCOPE, Features.PageViews, Severity.Info, options.name || title || path, 'browser.page_view', attributes, time));
    return true;
}

interface BlazorEvents {
    addEventListener?(name: string, callback: () => void): void;
    removeEventListener?(name: string, callback: () => void): void;
}

/** The initial page view, then history (push/replace/pop), optional hash changes, and Blazor enhanced loads. */
export function installPageViews(ctx: InstrumentationContext): Uninstall {
    if (!state.initialSent) trackPageView(ctx, 'initial');

    let active = true;
    const h = history;
    const originalPush = h.pushState;
    const originalReplace = h.replaceState;
    // Deferred a tick so routers that set document.title right after pushState are picked up.
    const later = (type: NavigationType, onlyIfChanged: boolean) => setTimeout(guard(() => {
        if (active) trackPageView(ctx, type, { onlyIfChanged });
    }), 0);

    const pushState = function (this: History, ...args: Parameters<History['pushState']>) {
        const result = originalPush.apply(this, args);
        try {
            if (active) later('push', false);
        } catch {
            // Never throw into the host page.
        }
        return result;
    };
    const replaceState = function (this: History, ...args: Parameters<History['replaceState']>) {
        const result = originalReplace.apply(this, args);
        try {
            // Routers call replaceState with the same URL to store scroll positions: only a new URL counts.
            if (active) later('replace', true);
        } catch {
            // Never throw into the host page.
        }
        return result;
    };
    h.pushState = pushState;
    h.replaceState = replaceState;

    const onPop = guard(() => trackPageView(ctx, 'pop', { onlyIfChanged: true }));
    const onHash = guard(() => trackPageView(ctx, 'hash', { onlyIfChanged: true }));
    const onEnhancedLoad = guard(() => trackPageView(ctx, 'push', { onlyIfChanged: true }));
    addEventListener('popstate', onPop);
    if (ctx.config.pageViews.trackHashChanges) addEventListener('hashchange', onHash);

    // The last page's visit has no next page view to end it; the hide is the only chance, and it comes before the
    // unload flush (first phase) so the record leaves with it. Time spent hidden is nobody's visit: the next one
    // starts when the page is shown again.
    const trackVisits = ctx.config.pageViews.trackVisitTime;
    const offHidden = trackVisits ? onHidden(guard(() => trackPageVisitTime(ctx)), 'first') : null;
    const onVisible = guard(() => {
        if (document.visibilityState === 'visible' && state.url) state.visitStart = now();
    });
    if (trackVisits) document.addEventListener('visibilitychange', onVisible);

    let blazor: BlazorEvents | null = null;
    const hookBlazor = guard(() => {
        const candidate = (window as unknown as { Blazor?: BlazorEvents }).Blazor;
        if (active && !blazor && candidate && typeof candidate.addEventListener === 'function') {
            blazor = candidate;
            candidate.addEventListener('enhancedload', onEnhancedLoad);
        }
    });
    hookBlazor();
    // The Blazor script may load after this module.
    if (!blazor) addEventListener('load', hookBlazor);

    return () => {
        active = false;
        if (h.pushState === pushState) h.pushState = originalPush;
        if (h.replaceState === replaceState) h.replaceState = originalReplace;
        removeEventListener('popstate', onPop);
        removeEventListener('hashchange', onHash);
        removeEventListener('load', hookBlazor);
        if (offHidden) offHidden();
        document.removeEventListener('visibilitychange', onVisible);
        const b = blazor as BlazorEvents | null;
        if (b && typeof b.removeEventListener === 'function') b.removeEventListener('enhancedload', onEnhancedLoad);
    };
}
