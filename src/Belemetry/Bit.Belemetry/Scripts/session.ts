import { now } from './clock';
import type { SessionConfig } from './config';
import { newTraceId } from './ids';
import { readItem, removeItem, StorageKeys, writeItem } from './storage';

interface SessionState {
    id: string;
    start: number;
    last: number;
}

/**
 * The session id (rotated after inactivity or a maximum duration) and the user ids. The session survives reloads
 * in sessionStorage only with `Sessions` consent; the pseudo id and user id exist only with `UserTracking`.
 */
export class SessionManager {
    private state: SessionState | null = null;
    private persistent = false;
    private lastSaved = 0;
    private tracking = false;
    private pseudo: string | null = null;
    private user: string | null = null;

    /** Called when the session id or the user ids change. */
    onChange: (() => void) | null = null;

    constructor(public config: SessionConfig) { }

    /** Turns sessionStorage persistence on or off. Turning it on at start adopts a stored, still valid session. */
    setPersistent(on: boolean, time = now()): void {
        if (on === this.persistent) return;
        this.persistent = on;
        if (!on) {
            removeItem('session', StorageKeys.session);
            return;
        }
        if (!this.state) {
            const raw = readItem('session', StorageKeys.session);
            try {
                const stored = raw ? JSON.parse(raw) as SessionState : null;
                if (stored && typeof stored.id === 'string' && /^[0-9a-f]{32}$/.test(stored.id) && !this.expired(stored, time)) {
                    this.state = { id: stored.id, start: +stored.start, last: +stored.last };
                }
            } catch {
                // A corrupt entry starts a new session.
            }
        }
        this.save();
    }

    /** The current session id, rotating it when it expired. Every call counts as activity. */
    id(time = now()): string {
        const state = this.state;
        if (!state || this.expired(state, time)) {
            this.state = { id: newTraceId(), start: time, last: time };
            this.save();
            if (state) this.notify();
            return this.state.id;
        }
        if (time > state.last) state.last = time;
        // Throttled: storage writes are synchronous, and a stale "last" by a second is harmless.
        if (time - this.lastSaved > 1000) this.save();
        return state.id;
    }

    /** The current session id without counting as activity (creates one if there is none yet). */
    peek(): string {
        return this.state ? this.state.id : this.id();
    }

    private expired(state: SessionState, time: number): boolean {
        return time - state.last > this.config.inactivityTimeoutMinutes * 60000
            || time - state.start > this.config.maxDurationMinutes * 60000;
    }

    private save(): void {
        if (!this.persistent || !this.state) return;
        this.lastSaved = this.state.last;
        writeItem('session', StorageKeys.session, JSON.stringify(this.state));
    }

    get userTracking(): boolean {
        return this.tracking;
    }

    /** Turns user tracking on (loading or creating the pseudo id) or off (forgetting both ids). */
    setUserTracking(on: boolean): void {
        if (on === this.tracking) return;
        this.tracking = on;
        if (on) {
            let pseudo = readItem('local', StorageKeys.pseudoId);
            if (!pseudo || !/^[0-9a-f]{32}$/.test(pseudo)) {
                pseudo = newTraceId();
                writeItem('local', StorageKeys.pseudoId, pseudo);
            }
            this.pseudo = pseudo;
            if (this.user === null) this.user = readItem('session', StorageKeys.userId);
        } else {
            this.pseudo = null;
            this.user = null;
            removeItem('local', StorageKeys.pseudoId);
            removeItem('session', StorageKeys.userId);
        }
        this.notify();
    }

    get pseudoId(): string | null {
        return this.pseudo;
    }

    get userId(): string | null {
        return this.user;
    }

    /** Sets the authenticated user id. It is only kept with user tracking; returns whether it was kept. */
    setUser(id: string | null): boolean {
        const value = this.tracking && id ? String(id) : null;
        const changed = value !== this.user;
        this.user = value;
        if (value) writeItem('session', StorageKeys.userId, value);
        else removeItem('session', StorageKeys.userId);
        if (changed) this.notify();
        return value !== null;
    }

    private notify(): void {
        try {
            if (this.onChange) this.onChange();
        } catch {
            // Never throw into the host page.
        }
    }
}

/**
 * Session sampling, decided deterministically from the session id: the first 32 bits of the (random) id as a
 * fraction of 2^32, compared with the ratio. Every record of a session is either kept or dropped.
 */
export function isSessionSampled(sessionId: string, ratio: number): boolean {
    if (!(ratio < 1)) return true;
    if (!(ratio > 0)) return false;
    const bucket = parseInt(sessionId.slice(0, 8), 16);
    return !isNaN(bucket) && bucket / 4294967296 < ratio;
}
