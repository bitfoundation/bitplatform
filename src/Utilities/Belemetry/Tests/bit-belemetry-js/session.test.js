import { beforeEach, describe, expect, it } from 'vitest';
import { isSessionSampled, SessionManager } from '../../Bit.Belemetry/Scripts/session.ts';

const config = { inactivityTimeoutMinutes: 30, maxDurationMinutes: 240 };
const MIN = 60000;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
});

describe('SessionManager', () => {
    it('keeps the id while active and rotates it after inactivity', () => {
        const session = new SessionManager(config);
        let changes = 0;
        session.onChange = () => changes++;
        const first = session.id(0);
        expect(first).toMatch(/^[0-9a-f]{32}$/);
        expect(session.id(29 * MIN)).toBe(first);
        expect(session.id(58 * MIN)).toBe(first);
        const second = session.id(89 * MIN);
        expect(second).not.toBe(first);
        expect(changes).toBe(1);
    });

    it('rotates after the maximum duration even when active', () => {
        const session = new SessionManager(config);
        const first = session.id(0);
        let t = 0;
        while (t < 239 * MIN) {
            t += 20 * MIN;
            if (t <= 240 * MIN) expect(session.id(t)).toBe(first);
        }
        expect(session.id(241 * MIN)).not.toBe(first);
    });

    it('stores the session only when persistent and adopts a stored valid one', () => {
        const memory = new SessionManager(config);
        memory.id(0);
        expect(sessionStorage.getItem('bit-belemetry-session')).toBeNull();

        const persistent = new SessionManager(config);
        persistent.setPersistent(true, 0);
        const id = persistent.id(1000);
        const stored = JSON.parse(sessionStorage.getItem('bit-belemetry-session'));
        expect(stored).toMatchObject({ id, start: 1000 });

        const reloaded = new SessionManager(config);
        reloaded.setPersistent(true, 5 * MIN);
        expect(reloaded.id(5 * MIN)).toBe(id);

        const expired = new SessionManager(config);
        expired.setPersistent(true, 60 * MIN);
        expect(expired.id(60 * MIN)).not.toBe(id);

        expired.setPersistent(false);
        expect(sessionStorage.getItem('bit-belemetry-session')).toBeNull();
    });

    it('keeps user ids only with user tracking', () => {
        const session = new SessionManager(config);
        expect(session.setUser('alice')).toBe(false);
        expect(session.userId).toBeNull();
        expect(session.pseudoId).toBeNull();

        session.setUserTracking(true);
        expect(session.pseudoId).toMatch(/^[0-9a-f]{32}$/);
        expect(localStorage.getItem('bit-belemetry-pseudo-id')).toBe(session.pseudoId);
        expect(session.setUser('alice')).toBe(true);
        expect(sessionStorage.getItem('bit-belemetry-user-id')).toBe('alice');

        const again = new SessionManager(config);
        again.setUserTracking(true);
        expect(again.pseudoId).toBe(session.pseudoId);
        expect(again.userId).toBe('alice');

        again.setUserTracking(false);
        expect(again.pseudoId).toBeNull();
        expect(again.userId).toBeNull();
        expect(localStorage.getItem('bit-belemetry-pseudo-id')).toBeNull();
        expect(sessionStorage.getItem('bit-belemetry-user-id')).toBeNull();
    });
});

describe('session sampling', () => {
    it('is deterministic per session id', () => {
        expect(isSessionSampled('00000000aaaaaaaaaaaaaaaaaaaaaaaa', 0.5)).toBe(true);
        expect(isSessionSampled('7fffffffaaaaaaaaaaaaaaaaaaaaaaaa', 0.5)).toBe(true);
        expect(isSessionSampled('80000000aaaaaaaaaaaaaaaaaaaaaaaa', 0.5)).toBe(false);
        expect(isSessionSampled('ffffffffaaaaaaaaaaaaaaaaaaaaaaaa', 0.999)).toBe(false);
        const id = '3a1b2c3d4e5f60718293a4b5c6d7e8f9';
        const first = isSessionSampled(id, 0.3);
        for (let i = 0; i < 10; i++) expect(isSessionSampled(id, 0.3)).toBe(first);
    });

    it('keeps everything at 1 and nothing at 0', () => {
        expect(isSessionSampled('ffffffffffffffffffffffffffffffff', 1)).toBe(true);
        expect(isSessionSampled('00000000000000000000000000000000', 0)).toBe(false);
    });

    it('samples roughly the ratio over random ids', () => {
        const session = () => new SessionManager(config).id(0);
        let kept = 0;
        for (let i = 0; i < 2000; i++) if (isSessionSampled(session(), 0.25)) kept++;
        expect(kept / 2000).toBeGreaterThan(0.2);
        expect(kept / 2000).toBeLessThan(0.3);
    });
});
