// Waits out a sync access handle a closed tab has not let go of yet.
//
// OPFSCoopSyncVFS takes a file's Web Lock and then its sync access handles. When a tab closes, Firefox
// releases the tab's Web Locks at once but the access handles its worker held only later - usually within a
// couple of seconds, now and then far later - so the connection that wins the lock can still find the file
// taken, and createSyncAccessHandle throws NoModificationAllowedError. Holding the lock means no live
// connection has the file, so the error is that dead handle: waiting for the browser to drop it beats failing
// the statement. The VFS is vendored unchanged, so the wait wraps the browser API the VFS calls instead.

/** Anything with the browser's createSyncAccessHandle - in practice FileSystemFileHandle.prototype. */
export interface SyncAccessHandleSource {
    createSyncAccessHandle(...args: unknown[]): Promise<unknown>;
}

export interface StaleHandleRetryOptions {
    /** How long to keep trying before the browser's own error is let through. */
    timeoutMs: number;
    /** The first wait; each later one doubles, up to maxDelayMs. */
    firstDelayMs: number;
    maxDelayMs: number;
}

const DEFAULTS: StaleHandleRetryOptions = { timeoutMs: 15_000, firstDelayMs: 25, maxDelayMs: 1_000 };

const PATCHED = Symbol.for('bit-bsqlite.retryStaleAccessHandles');

/**
 * Makes createSyncAccessHandle on `source` retry a NoModificationAllowedError with backoff, for up to
 * `timeoutMs`. Any other error, and the last one once the time is up, is thrown as the browser threw it.
 * Installing it twice is a no-op.
 */
export function retryStaleAccessHandles(source: SyncAccessHandleSource, options: Partial<StaleHandleRetryOptions> = {}): void {
    const target = source as SyncAccessHandleSource & { [PATCHED]?: true };
    if (target[PATCHED]) {
        return;
    }

    const { timeoutMs, firstDelayMs, maxDelayMs } = { ...DEFAULTS, ...options };
    const create = target.createSyncAccessHandle;

    target.createSyncAccessHandle = async function (this: unknown, ...args: unknown[]): Promise<unknown> {
        const deadline = Date.now() + timeoutMs;
        for (let delay = firstDelayMs; ; delay = Math.min(delay * 2, maxDelayMs)) {
            try {
                return await create.apply(this, args);
            } catch (error) {
                if ((error as { name?: unknown } | null)?.name !== 'NoModificationAllowedError' || Date.now() + delay > deadline) {
                    throw error;
                }

                await new Promise(resolve => setTimeout(resolve, delay));
            }
        }
    };

    target[PATCHED] = true;
}
