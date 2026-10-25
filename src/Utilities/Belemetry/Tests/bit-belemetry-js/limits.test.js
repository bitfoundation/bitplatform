import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { ErrorLimiter, errorSignature, limitRecord, RateLimiter, truncate } from '../../Bit.Belemetry/Scripts/limits.ts';
import { log, span } from './helpers.js';

const error = (message, stack = 'TypeError: ' + message + '\n    at loop (app.js:1:2)\n    at tick (app.js:3:4)', overrides = {}) => log({
    scope: 'bit.belemetry.errors',
    feature: 1,
    severityNumber: 17,
    body: message,
    eventName: 'exception',
    attributes: { 'exception.type': 'TypeError', 'exception.message': message, 'exception.stacktrace': stack },
    ...overrides,
});

describe('RateLimiter', () => {
    it('allows a burst, then refills over the period, never past the capacity', () => {
        let now = 0;
        const limiter = new RateLimiter(10, 10000, () => now);
        expect(limiter.take(25)).toBe(10);
        expect(limiter.take(1)).toBe(0);
        now += 3000;
        expect(limiter.take(5)).toBe(3);
        now += 60000;
        expect(limiter.take(100)).toBe(10);
        expect(limiter.take(0)).toBe(0);
    });
});

describe('ErrorLimiter', () => {
    const config = (overrides = {}) => ({ ...resolveConfig().errors, ...overrides });

    it('records an error once per duplicate window', () => {
        let now = 0;
        const limiter = new ErrorLimiter(config(), () => now);
        expect(limiter.allow(error('boom'))).toBe(true);
        now += 4999;
        expect(limiter.allow(error('boom'))).toBe(false);
        expect(limiter.allow(error('other'))).toBe(true);
        now += 1; // the window counts from the last recorded occurrence
        expect(limiter.allow(error('boom'))).toBe(true);
    });

    it('tells errors apart by their first frame, not by the rest of the stack', () => {
        const limiter = new ErrorLimiter(config(), () => 0);
        expect(limiter.allow(error('boom', 'TypeError: boom\n    at a (app.js:1:2)\n    at caller1'))).toBe(true);
        expect(limiter.allow(error('boom', 'TypeError: boom\n    at a (app.js:1:2)\n    at caller2'))).toBe(false);
        expect(limiter.allow(error('boom', 'TypeError: boom\n    at b (app.js:9:9)'))).toBe(true);
    });

    it('caps errors per minute', () => {
        let now = 0;
        const limiter = new ErrorLimiter(config({ duplicateWindowMs: 0, maxPerMinute: 3 }), () => now);
        expect([1, 2, 3, 4].map(i => limiter.allow(error('e' + i)))).toEqual([true, true, true, false]);
        now += 20000; // a third of the minute: one more
        expect([5, 6].map(i => limiter.allow(error('e' + i)))).toEqual([true, false]);
    });

    it('does not open a duplicate window for an error the cap dropped', () => {
        let now = 0;
        const limiter = new ErrorLimiter(config({ duplicateWindowMs: 120000, maxPerMinute: 1 }), () => now);
        expect(limiter.allow(error('first'))).toBe(true);
        now += 1000;
        expect(limiter.allow(error('second'))).toBe(false); // the cap is spent
        now += 61000; // a token is back, within two minutes of the dropped occurrence
        expect(limiter.allow(error('second'))).toBe(true);
        expect(limiter.allow(error('second'))).toBe(false); // now a duplicate
    });

    it('can be switched off', () => {
        const limiter = new ErrorLimiter(config({ duplicateWindowMs: 0, maxPerMinute: 0 }), () => 0);
        expect(Array.from({ length: 500 }, () => limiter.allow(error('boom'))).every(Boolean)).toBe(true);
    });

    it('signs an error like the C# side does', () => {
        expect(errorSignature(error('boom', 'TypeError: boom\r\n   at A()\r\n   at B()')))
            .toBe(['bit.belemetry.errors', 'TypeError', 'boom', 'TypeError: boom\r\n   at A()'].join('\u0001'));
        expect(errorSignature(log({ scope: 'bit.belemetry.console', body: 'failed', attributes: {} })))
            .toBe(['bit.belemetry.console', '', 'failed', ''].join('\u0001'));
    });
});

describe('truncate and limitRecord', () => {
    it('cuts strings without splitting a surrogate pair', () => {
        expect(truncate('abcdef', 4)).toBe('abcd');
        expect(truncate('abc😀', 4)).toBe('abc');
        expect(truncate('abc', 0)).toBe('abc');
    });

    it('cuts the attributes, the body, the status message and the span events', () => {
        const short = ['ok', 'fine'];
        const entry = log({ body: 'x'.repeat(10), attributes: { long: 'y'.repeat(10), list: ['z'.repeat(10), 'ok'], short, n: 12345678901 } });
        limitRecord(entry, 4);
        expect(entry.body).toBe('xxxx');
        expect(entry.attributes).toEqual({ long: 'yyyy', list: ['zzzz', 'ok'], short: ['ok', 'fine'], n: 12345678901 });
        expect(entry.attributes.short).toBe(short); // nothing to cut: not copied

        const record = span({ status: { code: 2, message: 'm'.repeat(10) }, events: [{ name: 'e', time: 1, attributes: { a: 'a'.repeat(10) } }] });
        limitRecord(record, 4);
        expect(record.status.message).toBe('mmmm');
        expect(record.events[0].attributes.a).toBe('aaaa');
    });
});

describe('the pipeline', () => {
    let api;

    beforeEach(async () => {
        localStorage.clear();
        sessionStorage.clear();
        vi.resetModules();
        api = await import('../../Bit.Belemetry/Scripts/bit-belemetry.ts');
        api.start({ consent: { default: 4095 }, transport: 'relay', performance: { webVitals: false, documentLoad: false } });
    });

    afterEach(() => api._reset());

    it('keeps one of a loop of identical errors, and every distinct one', () => {
        const before = api._getState().pipeline.size;
        for (let i = 0; i < 50; i++) api.trackException('TypeError', 'boom', 'TypeError: boom\n    at loop (app.js:1:2)');
        api.trackException('RangeError', 'other');
        expect(api._getState().pipeline.size - before).toBe(2);
    });

    it('cuts runaway strings after scrubbing them', () => {
        const state = api._getState();
        state.config.maxValueLength = 20;
        api.trackEvent('big', { payload: 'jane@example.com ' + 'x'.repeat(100) });
        const queued = state.pipeline.queue[state.pipeline.queue.length - 1];
        expect(queued.attributes.payload).toBe('[email] ' + 'x'.repeat(12));
    });
});
