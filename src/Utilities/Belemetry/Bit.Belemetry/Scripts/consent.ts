import type { ConsentConfig } from './config';
import { Features } from './features';
import type { TelemetryRecord } from './model';
import { countDropped } from './stats';
import { readItem, removeItem, StorageKeys, writeItem } from './storage';

/** Called after every consent change with the records held before the decision that the new consent allows. */
export type ConsentListener = (previous: number, next: number, released: TelemetryRecord[]) => void;

/**
 * The consent a request was built under (C# `BelemetryConsentMark`): the features its records belong to, and how many
 * times they had been revoked by then (`ConsentManager.revocations`). Once one of them is revoked again it is withdrawn -
 * neither sent, retried nor stored - as it may hold the revoked feature's records.
 */
export interface ConsentMark {
    features: number;
    revocations: number;
}

/**
 * The consent state machine. Before a decision only `consent.default` is granted and records of the other
 * features are held (bounded) in memory. After the decision the held records the user granted are released and
 * the rest are dropped.
 */
export class ConsentManager {
    private decision: number | null = null;
    private pending: TelemetryRecord[] = [];
    private listeners: ConsentListener[] = [];
    /** How many times each feature (by bit) was revoked. */
    private revoked: number[] = [];

    /**
     * @param available The configured features (`config.features`). The grant never exceeds them: a grant of a
     * feature the configuration switched off is not recorded, and a default or restored decision is narrowed to them.
     */
    constructor(public config: ConsentConfig, private available: number = Features.All) { }

    /** Reads the persisted choice (if `persist`). */
    load(): void {
        if (!this.config.persist) return;
        const features = parseChoice(readItem('local', StorageKeys.consent));
        if (features !== null) this.decision = features & Features.All & this.available;
    }

    /**
     * Adopts the choice another tab of the app persisted (the `storage` event's new value): a banner answered there is
     * answered here, and a feature revoked there stops being collected here as well. A removed or unreadable entry
     * changes nothing.
     */
    adopt(raw: string | null): void {
        const features = this.config.persist ? parseChoice(raw) : null;
        if (features === null) return;
        const decision = features & Features.All & this.available;
        if (decision === this.decision) return;
        const previous = this.granted();
        this.decision = decision;
        this.changed(previous);
    }

    get decided(): boolean {
        return this.decision !== null;
    }

    /** The features denied by the browser's Global Privacy Control signal: the analytics group, never granted. */
    gpcMask(): number {
        const nav = typeof navigator !== 'undefined' ? navigator as Navigator & { globalPrivacyControl?: boolean } : null;
        return this.config.honorGlobalPrivacyControl && nav && nav.globalPrivacyControl === true ? Features.Analytics : 0;
    }

    /** The granted features: always within the configured ones, so exactly what is collected. */
    granted(): number {
        const value = this.decision !== null ? this.decision : this.config.default;
        return value & Features.All & this.available & ~this.gpcMask();
    }

    /** The features worth collecting now: everything grantable before a decision (to hold it), the grant after. */
    collectable(): number {
        return this.decision !== null ? this.granted() : Features.All & this.available & ~this.gpcMask();
    }

    isGranted(feature: number): boolean {
        return (this.granted() & feature) !== 0;
    }

    /**
     * How many times any of `features` was revoked so far. Counted per feature, so a revoke of another one withdraws
     * nothing: a request of error logs survives the user turning off interactions, and revoking Persistence or Sessions,
     * which no record belongs to, withdraws nothing in flight.
     */
    revocations(features: number): number {
        let count = 0;
        for (let bit = 0; bit < this.revoked.length; bit++) {
            if (features & (1 << bit)) count += this.revoked[bit] || 0;
        }
        return count;
    }

    /** The mark of a request built now from records of `features`. */
    mark(features: number): ConsentMark {
        return { features, revocations: this.revocations(features) };
    }

    /** Holds a record until the decision. The oldest record is dropped when `pendingLimit` is exceeded. */
    hold(record: TelemetryRecord): void {
        if (this.config.pendingLimit <= 0) {
            countDropped('pending_full');
            return;
        }
        this.pending.push(record);
        if (this.pending.length > this.config.pendingLimit) {
            this.pending.shift();
            countDropped('pending_full');
        }
    }

    get pendingCount(): number {
        return this.pending.length;
    }

    /** Records the decision, persists it, clears the storage of revoked features and notifies the listeners. */
    set(features: number): void {
        const previous = this.granted();
        this.decision = (features | 0) & Features.All & this.available;
        if (this.config.persist) {
            writeItem('local', StorageKeys.consent, JSON.stringify({ v: 1, features: this.decision, time: Date.now() }));
        }
        this.changed(previous);
    }

    /** Applies new consent settings (a repeated `start`). Before a decision a new default changes the grant. */
    update(config: ConsentConfig): void {
        const previous = this.granted();
        this.config = config;
        this.changed(previous);
    }

    onChange(listener: ConsentListener): void {
        this.listeners.push(listener);
    }

    private changed(previous: number): void {
        const next = this.granted();
        const revoked = previous & ~next;
        for (let bit = 0; bit < 31; bit++) {
            if (revoked & (1 << bit)) this.revoked[bit] = (this.revoked[bit] || 0) + 1;
        }
        purgeStorage(revoked);
        // After a decision nothing stays held. Before one (a new default), only what became granted leaves.
        const released = this.pending.filter(r => (next & r.feature) !== 0);
        if (this.decided) countDropped('not_consented', this.pending.length - released.length);
        this.pending = this.decided ? [] : this.pending.filter(r => (next & r.feature) === 0);
        for (const listener of this.listeners) {
            try {
                listener(previous, next, released);
            } catch {
                // Never throw into the host page.
            }
        }
    }
}

/** The features of a persisted choice (`bit-belemetry-consent`), or null when the entry is not one. */
function parseChoice(raw: string | null): number | null {
    if (!raw) return null;
    try {
        const stored = JSON.parse(raw);
        return stored && stored.v === 1 && typeof stored.features === 'number' ? stored.features : null;
    } catch {
        // A corrupt entry counts as "no decision".
        return null;
    }
}

/** Clears what is stored on the device for the revoked features. */
export function purgeStorage(revoked: number): void {
    if (revoked) removeItem('local', StorageKeys.queue);
    if (revoked & Features.Sessions) removeItem('session', StorageKeys.session);
    if (revoked & Features.UserTracking) {
        removeItem('local', StorageKeys.pseudoId);
        removeItem('session', StorageKeys.userId);
    }
}
