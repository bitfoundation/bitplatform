import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ConsentManager } from '../../Bit.Belemetry/Scripts/consent.ts';
import { Features } from '../../Bit.Belemetry/Scripts/features.ts';
import { log } from './helpers.js';

const config = (overrides = {}) => ({ default: 0, persist: true, honorGlobalPrivacyControl: true, pendingLimit: 500, ...overrides });

function setGpc(value) {
    Object.defineProperty(navigator, 'globalPrivacyControl', { value, configurable: true });
}

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    setGpc(undefined);
});

afterEach(() => setGpc(undefined));

describe('feature bits', () => {
    it('match the contract', () => {
        expect(Features).toMatchObject({
            Errors: 1, PageViews: 2, Interactions: 4, Performance: 8, Dependencies: 16, Sessions: 32, UserTracking: 64, Logs: 128,
            Traces: 256, Metrics: 512, Persistence: 1024, Events: 2048, All: 4095,
        });
        expect(Features.Diagnostics).toBe(1 | 128 | 256 | 16);
        expect(Features.Analytics).toBe(2 | 4 | 32 | 64 | 2048);
        expect(Features.Monitoring).toBe(8 | 512);
    });
});

describe('ConsentManager', () => {
    it('grants only the default before a decision', () => {
        const consent = new ConsentManager(config({ default: Features.Errors }));
        consent.load();
        expect(consent.decided).toBe(false);
        expect(consent.granted()).toBe(Features.Errors);
        expect(consent.collectable()).toBe(Features.All);
    });

    it('never grants more than the configured features', () => {
        const available = Features.Errors | Features.Logs;
        const consent = new ConsentManager(config({ default: Features.All }), available);
        expect(consent.granted(), 'The default is narrowed.').toBe(available);
        expect(consent.collectable()).toBe(available);

        consent.set(Features.All);
        expect(consent.granted()).toBe(available);
        expect(consent.isGranted(Features.PageViews)).toBe(false);
        expect(JSON.parse(localStorage.getItem('bit-belemetry-consent')).features, 'Only the available part is recorded.').toBe(available);

        // A decision stored before the configuration narrowed is narrowed when read back.
        localStorage.setItem('bit-belemetry-consent', JSON.stringify({ v: 1, features: Features.All, time: 1 }));
        const reloaded = new ConsentManager(config(), Features.Errors);
        reloaded.load();
        expect(reloaded.granted()).toBe(Features.Errors);
    });

    it('persists the decision and reads it back', () => {
        const consent = new ConsentManager(config());
        consent.set(Features.Diagnostics);
        const stored = JSON.parse(localStorage.getItem('bit-belemetry-consent'));
        expect(stored).toMatchObject({ v: 1, features: Features.Diagnostics });
        expect(typeof stored.time).toBe('number');

        const reloaded = new ConsentManager(config());
        reloaded.load();
        expect(reloaded.decided).toBe(true);
        expect(reloaded.granted()).toBe(Features.Diagnostics);
        expect(reloaded.collectable()).toBe(Features.Diagnostics);
    });

    it('neither persists nor reads without persist', () => {
        localStorage.setItem('bit-belemetry-consent', JSON.stringify({ v: 1, features: 4095, time: 1 }));
        const consent = new ConsentManager(config({ persist: false }));
        consent.load();
        expect(consent.decided).toBe(false);
        consent.set(Features.Errors);
        expect(JSON.parse(localStorage.getItem('bit-belemetry-consent')).features).toBe(4095);
    });

    it('ignores a corrupt stored entry', () => {
        localStorage.setItem('bit-belemetry-consent', '{nope');
        const consent = new ConsentManager(config());
        consent.load();
        expect(consent.decided).toBe(false);
    });

    it('releases the held records the decision grants and drops the rest', () => {
        const consent = new ConsentManager(config());
        const released = [];
        consent.onChange((previous, next, records) => released.push(...records));
        const pageView = log({ feature: Features.PageViews });
        const error = log({ feature: Features.Errors });
        consent.hold(pageView);
        consent.hold(error);
        expect(consent.pendingCount).toBe(2);
        consent.set(Features.Errors);
        expect(released).toEqual([error]);
        expect(consent.pendingCount).toBe(0);
    });

    it('bounds the pending buffer by dropping the oldest', () => {
        const consent = new ConsentManager(config({ pendingLimit: 2 }));
        const records = [1, 2, 3].map(i => log({ body: String(i), feature: Features.Events }));
        records.forEach(r => consent.hold(r));
        const released = [];
        consent.onChange((p, n, r) => released.push(...r));
        consent.set(Features.All);
        expect(released.map(r => r.body)).toEqual(['2', '3']);
    });

    it('never grants the analytics group under Global Privacy Control', () => {
        setGpc(true);
        const consent = new ConsentManager(config());
        expect(consent.collectable()).toBe(Features.All & ~Features.Analytics);
        consent.set(Features.All);
        expect(consent.granted()).toBe(Features.All & ~Features.Analytics);
        expect(consent.isGranted(Features.PageViews)).toBe(false);
        expect(consent.isGranted(Features.Errors)).toBe(true);
    });

    it('ignores GPC when not honored', () => {
        setGpc(true);
        const consent = new ConsentManager(config({ honorGlobalPrivacyControl: false }));
        consent.set(Features.All);
        expect(consent.granted()).toBe(Features.All);
    });

    it('clears the storage of revoked features', () => {
        const consent = new ConsentManager(config());
        consent.set(Features.All);
        localStorage.setItem('bit-belemetry-queue', '[]');
        sessionStorage.setItem('bit-belemetry-session', '{}');
        localStorage.setItem('bit-belemetry-pseudo-id', 'a'.repeat(32));
        sessionStorage.setItem('bit-belemetry-user-id', 'alice');

        consent.set(Features.All & ~Features.Persistence);
        expect(localStorage.getItem('bit-belemetry-queue')).toBeNull();
        expect(sessionStorage.getItem('bit-belemetry-session')).not.toBeNull();

        consent.set(Features.Errors);
        expect(sessionStorage.getItem('bit-belemetry-session')).toBeNull();
        expect(localStorage.getItem('bit-belemetry-pseudo-id')).toBeNull();
        expect(sessionStorage.getItem('bit-belemetry-user-id')).toBeNull();
        expect(localStorage.getItem('bit-belemetry-consent')).not.toBeNull();
    });

    it('notifies listeners with the previous and next grant', () => {
        const consent = new ConsentManager(config({ default: Features.Errors }));
        const changes = [];
        consent.onChange((previous, next) => changes.push([previous, next]));
        consent.set(Features.All);
        consent.set(Features.Logs);
        expect(changes).toEqual([[Features.Errors, Features.All], [Features.All, Features.Logs]]);
    });
});
