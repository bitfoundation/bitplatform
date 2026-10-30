import { test, expect } from 'vitest';
import { retryStaleAccessHandles } from '../../Bit.Bsqlite/Scripts/bit-bsqlite-opfs-handles.ts';

function domError(name) {
    const error = new Error(name);
    error.name = name;
    return error;
}

/** A stand-in for FileSystemFileHandle.prototype whose createSyncAccessHandle fails `failures` times first. */
function handleSource(failures, error = domError('NoModificationAllowedError')) {
    const source = {
        calls: 0,
        async createSyncAccessHandle(...args) {
            source.calls++;
            if (source.calls <= failures) {
                throw error;
            }

            return { handle: this, args };
        },
    };
    return source;
}

const fast = { timeoutMs: 1_000, firstDelayMs: 1, maxDelayMs: 4 };

test('a handle a closed tab still holds is waited out', async () => {
    const source = handleSource(3);
    retryStaleAccessHandles(source, fast);

    const result = await source.createSyncAccessHandle('opts');

    expect(source.calls).toBe(4);
    // Called on the handle it was called on, with what it was called with.
    expect(result).toEqual({ handle: source, args: ['opts'] });
});

test('any other error is thrown at once', async () => {
    const source = handleSource(1, domError('NotFoundError'));
    retryStaleAccessHandles(source, fast);

    await expect(source.createSyncAccessHandle()).rejects.toThrow('NotFoundError');
    expect(source.calls).toBe(1);
});

test('the browser\'s own error is let through once the time is up', async () => {
    const source = handleSource(Number.POSITIVE_INFINITY);
    retryStaleAccessHandles(source, { timeoutMs: 30, firstDelayMs: 5, maxDelayMs: 5 });

    const started = Date.now();
    await expect(source.createSyncAccessHandle()).rejects.toMatchObject({ name: 'NoModificationAllowedError' });

    expect(Date.now() - started).toBeLessThan(1_000);
    expect(source.calls).toBeGreaterThan(1);
});

test('installing it twice does not stack the retries', async () => {
    const source = handleSource(0);
    retryStaleAccessHandles(source, fast);
    const once = source.createSyncAccessHandle;
    retryStaleAccessHandles(source, fast);

    expect(source.createSyncAccessHandle).toBe(once);
});
