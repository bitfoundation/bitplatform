// Storage keys are a contract (see "Storage keys" in CLAUDE.md). Storage access can throw (sandboxed iframes,
// disabled cookies, quota), so every access is guarded and failures degrade to "not stored".

export const StorageKeys = {
    consent: 'bit-belemetry-consent',
    session: 'bit-belemetry-session',
    pseudoId: 'bit-belemetry-pseudo-id',
    userId: 'bit-belemetry-user-id',
    queue: 'bit-belemetry-queue',
} as const;

export type StorageKind = 'local' | 'session';

function store(kind: StorageKind): Storage | null {
    try {
        return kind === 'local' ? localStorage : sessionStorage;
    } catch {
        return null;
    }
}

export function readItem(kind: StorageKind, key: string): string | null {
    try {
        const s = store(kind);
        return s ? s.getItem(key) : null;
    } catch {
        return null;
    }
}

export function writeItem(kind: StorageKind, key: string, value: string): boolean {
    try {
        const s = store(kind);
        if (!s) return false;
        s.setItem(key, value);
        return true;
    } catch {
        return false;
    }
}

export function removeItem(kind: StorageKind, key: string): void {
    try {
        const s = store(kind);
        if (s) s.removeItem(key);
    } catch {
        // Nothing to do.
    }
}
