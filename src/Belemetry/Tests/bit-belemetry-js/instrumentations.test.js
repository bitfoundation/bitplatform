import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { installErrors } from '../../Bit.Belemetry/Scripts/instrumentations/errors.ts';
import { describeTarget, installInteractions, resolveTarget, xpath } from '../../Bit.Belemetry/Scripts/instrumentations/interactions.ts';
import { installPageViews, resetPageViewState, trackPageView, trackPageVisitTime } from '../../Bit.Belemetry/Scripts/instrumentations/page-views.ts';
import { ClsAccumulator, InpAccumulator, rate, THRESHOLDS } from '../../Bit.Belemetry/Scripts/instrumentations/web-vitals.ts';
import { installConsole } from '../../Bit.Belemetry/Scripts/instrumentations/console.ts';
import { fakeContext } from './helpers.js';

let uninstall = null;

afterEach(() => {
    if (uninstall) uninstall();
    uninstall = null;
    vi.useRealTimers();
    document.body.innerHTML = '';
});

describe('page views', () => {
    beforeEach(() => {
        resetPageViewState();
        history.replaceState(null, '', '/start');
        document.title = 'Start';
    });

    it('sends the initial page view with the semantic attributes', () => {
        const ctx = fakeContext();
        uninstall = installPageViews(ctx);
        expect(ctx.records).toHaveLength(1);
        expect(ctx.records[0]).toMatchObject({ eventName: 'browser.page_view', body: 'Start', feature: 2, severityNumber: 9 });
        expect(ctx.records[0].attributes).toMatchObject({
            'url.full': location.origin + '/start', 'url.path': '/start', 'page.title': 'Start', 'browser.navigation.type': 'initial',
        });
    });

    it('tracks pushState once per URL change and ignores same-URL replaceState', async () => {
        vi.useFakeTimers();
        const ctx = fakeContext();
        uninstall = installPageViews(ctx);
        history.pushState(null, '', '/a');
        await vi.advanceTimersByTimeAsync(1);
        history.replaceState({ scroll: 1 }, '', '/a');
        await vi.advanceTimersByTimeAsync(1);
        history.replaceState(null, '', '/b');
        await vi.advanceTimersByTimeAsync(1);
        expect(ctx.records.map(r => [r.attributes['url.path'], r.attributes['browser.navigation.type']])).toEqual([
            ['/start', 'initial'], ['/a', 'push'], ['/b', 'replace'],
        ]);
        expect(ctx.records[1].attributes['page.referrer']).toBe(location.origin + '/start');
    });

    it('dedupes the same URL within 100 ms', () => {
        const ctx = fakeContext();
        let t = 1000;
        vi.spyOn(performance, 'now').mockImplementation(() => t);
        expect(trackPageView(ctx, 'push', { url: '/p' })).toBe(true);
        t += 50;
        expect(trackPageView(ctx, 'manual', { url: '/p' })).toBe(false);
        t += 100;
        expect(trackPageView(ctx, 'manual', { url: '/p' })).toBe(true);
        expect(trackPageView(ctx, 'pop', { url: '/p', onlyIfChanged: true })).toBe(false);
        expect(trackPageView(ctx, 'manual', { url: '/q', name: 'Q' })).toBe(true);
        expect(ctx.records.map(r => r.body)).toEqual(['Start', 'Start', 'Q']);
    });

    it('ignores fragment-only changes unless trackHashChanges', () => {
        const ctx = fakeContext();
        trackPageView(ctx, 'push', { url: '/h' });
        expect(trackPageView(ctx, 'hash', { url: '/h#x', onlyIfChanged: true })).toBe(false);
        const tracking = fakeContext({ pageViews: { trackHashChanges: true } });
        resetPageViewState();
        trackPageView(tracking, 'push', { url: '/h' });
        expect(trackPageView(tracking, 'hash', { url: '/h#x', onlyIfChanged: true })).toBe(true);
    });

    it('restores history on uninstall', () => {
        const push = history.pushState;
        uninstall = installPageViews(fakeContext());
        expect(history.pushState).not.toBe(push);
        uninstall();
        uninstall = null;
        expect(history.pushState).toBe(push);
    });

    it('records no visit time unless trackVisitTime', () => {
        const ctx = fakeContext();
        let t = 1000;
        vi.spyOn(performance, 'now').mockImplementation(() => t);
        trackPageView(ctx, 'push', { url: '/a' });
        t += 5000;
        trackPageView(ctx, 'push', { url: '/b' });
        expect(ctx.records.map(r => r.eventName)).toEqual(['browser.page_view', 'browser.page_view']);
        expect(trackPageVisitTime(ctx)).toBe(false);
    });

    it('records the previous page\'s visit time when the next page view starts', () => {
        const ctx = fakeContext({ pageViews: { trackVisitTime: true } });
        let t = 1000;
        vi.spyOn(performance, 'now').mockImplementation(() => t);
        document.title = 'A';
        trackPageView(ctx, 'push', { url: '/a' });
        t += 5000;
        document.title = 'B';
        trackPageView(ctx, 'push', { url: '/b' });
        expect(ctx.records.map(r => r.eventName)).toEqual(['browser.page_view', 'browser.page_visit_time', 'browser.page_view']);
        const visit = ctx.records[1];
        expect(visit).toMatchObject({ body: 'A', feature: 2, severityNumber: 9, scope: 'bit.belemetry.page_views' });
        expect(visit.attributes).toEqual({ 'url.full': location.origin + '/a', 'url.path': '/a', 'page.title': 'A', 'page.visit.duration': 5000 });
        // The visit ends where the next page view starts: same timestamp.
        expect(visit.time).toBe(ctx.records[2].time);
    });

    it('records the visit so far when the page is hidden, and starts a new one', () => {
        const ctx = fakeContext({ pageViews: { trackVisitTime: true } });
        let t = 1000;
        vi.spyOn(performance, 'now').mockImplementation(() => t);
        uninstall = installPageViews(ctx);
        t += 2000;
        const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
        document.dispatchEvent(new Event('visibilitychange'));
        // Two hours in the background are nobody's visit.
        t += 7200000;
        visibility.mockReturnValue('visible');
        document.dispatchEvent(new Event('visibilitychange'));
        t += 3000;
        trackPageView(ctx, 'push', { url: '/next' });
        const durations = ctx.records.filter(r => r.eventName === 'browser.page_visit_time').map(r => r.attributes['page.visit.duration']);
        expect(durations).toEqual([2000, 3000]);
    });
});

describe('interactions', () => {
    it('resolves the nearest interactive ancestor', () => {
        document.body.innerHTML = '<div id="root"><button id="buy" aria-label="Buy now"><span id="inner">Buy</span></button><p id="plain">text</p>'
            + '<div data-bit-belemetry-name="card"><i id="icon"></i></div><div role="button"><b id="rb"></b></div></div>';
        expect(resolveTarget(document.getElementById('inner')).id).toBe('buy');
        expect(resolveTarget(document.getElementById('plain'))).toBeNull();
        expect(resolveTarget(document.getElementById('icon')).getAttribute('data-bit-belemetry-name')).toBe('card');
        expect(resolveTarget(document.getElementById('rb')).getAttribute('role')).toBe('button');
        expect(resolveTarget(document.getElementById('inner').firstChild).id).toBe('buy');
    });

    it('skips elements under [data-bit-belemetry-ignore]', () => {
        document.body.innerHTML = '<section data-bit-belemetry-ignore><a id="secret" href="#">x</a></section>';
        expect(resolveTarget(document.getElementById('secret'))).toBeNull();
    });

    it('describes the target without values, text only with captureText', () => {
        document.body.innerHTML = '<form id="f"><input type="submit" name="go" value="secret"><button data-bit-belemetry-name="save">  Save\n  all  </button></form>';
        const input = document.querySelector('input');
        const button = document.querySelector('button');
        const config = { events: ['click'], captureText: false, maxTextLength: 4 };
        expect(describeTarget(input, 'click', config)).toEqual({
            'event.type': 'click', 'target.element': 'input', 'target.name': 'go', 'target.xpath': '//*[@id="f"]/input[1]',
        });
        expect(describeTarget(button, 'click', { ...config, captureText: true })['target.text']).toBe('Save');
        expect(describeTarget(button, 'click', config)['target.text']).toBeUndefined();
        expect(describeTarget(input, 'click', { ...config, captureText: true })['target.text']).toBeUndefined();
    });

    it('builds xpaths from the root without ids', () => {
        document.body.innerHTML = '<div></div><div><a></a><a></a></div>';
        expect(xpath(document.querySelectorAll('a')[1])).toBe('/html[1]/body[1]/div[2]/a[2]');
    });

    it('emits a user interaction log on click', () => {
        document.body.innerHTML = '<button id="b">Go</button>';
        const ctx = fakeContext();
        uninstall = installInteractions(ctx);
        document.getElementById('b').click();
        expect(ctx.records).toHaveLength(1);
        expect(ctx.records[0]).toMatchObject({ eventName: 'browser.user_interaction', feature: 4, attributes: { 'target.id': 'b', 'event.type': 'click' } });
    });
});

describe('web vitals', () => {
    it('rates values with the official thresholds', () => {
        expect(THRESHOLDS).toEqual({ LCP: [2500, 4000], INP: [200, 500], CLS: [0.1, 0.25], FCP: [1800, 3000], TTFB: [800, 1800] });
        expect(rate('LCP', 2500)).toBe('good');
        expect(rate('LCP', 2501)).toBe('needs-improvement');
        expect(rate('LCP', 4000)).toBe('needs-improvement');
        expect(rate('LCP', 4001)).toBe('poor');
        expect(rate('INP', 200)).toBe('good');
        expect(rate('INP', 600)).toBe('poor');
        expect(rate('CLS', 0.1)).toBe('good');
        expect(rate('CLS', 0.2)).toBe('needs-improvement');
        expect(rate('CLS', 0.26)).toBe('poor');
        expect(rate('FCP', 1900)).toBe('needs-improvement');
        expect(rate('TTFB', 1801)).toBe('poor');
    });

    it('computes CLS as the largest session window', () => {
        const cls = new ClsAccumulator();
        cls.add(0, 0.05);
        cls.add(500, 0.05); // same window: 0.1
        cls.add(2000, 0.02); // gap > 1 s: new window
        cls.add(2500, 0.02);
        expect(cls.value).toBeCloseTo(0.1);
        const capped = new ClsAccumulator();
        for (let t = 0; t <= 6000; t += 900) capped.add(t, 0.01); // windows capped at 5 s
        expect(capped.value).toBeCloseTo(0.06);
    });

    it('computes INP from the longest interactions', () => {
        const inp = new InpAccumulator();
        expect(inp.value()).toBeNull();
        inp.add(1, 80);
        inp.add(1, 120); // same interaction: max
        inp.add(2, 60);
        inp.add(0, 999); // not an interaction
        expect(inp.value()).toBe(120);
        for (let i = 3; i < 103; i++) inp.add(i, 10);
        expect(inp.count).toBe(102);
        expect(inp.value()).toBe(10); // p98: skips the 2 longest per 100 interactions
    });
});

describe('errors', () => {
    it('reports errors and rejections, skipping ignored messages', () => {
        const ctx = fakeContext();
        uninstall = installErrors(ctx);
        const error = new TypeError('x is undefined');
        window.dispatchEvent(new ErrorEvent('error', { message: 'x is undefined', error }));
        window.dispatchEvent(new ErrorEvent('error', { message: 'ResizeObserver loop limit exceeded' }));
        const rejection = new Event('unhandledrejection');
        rejection.reason = 'nope';
        window.dispatchEvent(rejection);
        expect(ctx.records).toHaveLength(2);
        expect(ctx.records[0]).toMatchObject({ eventName: 'exception', severityNumber: 17, severityText: 'ERROR', feature: 1 });
        expect(ctx.records[0].attributes).toMatchObject({ 'exception.type': 'TypeError', 'exception.message': 'x is undefined' });
        expect(ctx.records[0].attributes['exception.stacktrace']).toContain('TypeError');
        expect(ctx.records[1].attributes).toMatchObject({ 'exception.type': 'UnhandledRejection', 'exception.message': 'nope' });
    });
});

describe('console capture', () => {
    it('maps error to Errors and warn to Logs, still calling the original', () => {
        const original = vi.fn();
        const warn = vi.fn();
        vi.spyOn(console, 'error').mockImplementation(original);
        vi.spyOn(console, 'warn').mockImplementation(warn);
        const ctx = fakeContext({ logs: { captureConsole: true, consoleLevels: ['error', 'warn'] } });
        uninstall = installConsole(ctx);
        console.error('bad', { a: 1 });
        console.warn('careful');
        expect(original).toHaveBeenCalledWith('bad', { a: 1 });
        expect(warn).toHaveBeenCalledOnce();
        expect(ctx.records.map(r => [r.feature, r.severityNumber, r.body])).toEqual([[1, 17, 'bad {"a":1}'], [128, 13, 'careful']]);
    });
});
