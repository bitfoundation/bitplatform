import { test, expect } from 'vitest';
import { listen } from '../../Bit.Bsqlite/Scripts/bit-bsqlite-host.ts';

// `listen` is the seam between the host's notifications and the .NET transport. It cannot be
// exercised from the browser suite, which has no .NET side, so its filtering is pinned here.
// A host is only `onNotify` as far as this function is concerned.
function fakeHost() {
    const listeners = new Set();
    return {
        onNotify(listener) {
            listeners.add(listener);
            return () => listeners.delete(listener);
        },
        emit(payload, info) {
            for (const listener of listeners) {
                listener(payload, info);
            }
        },
    };
}

function fakeTarget() {
    const calls = [];
    return {
        calls,
        invokeMethodAsync(method, ...args) {
            calls.push({ method, args });
            return Promise.resolve();
        },
    };
}

const write = (databaseName, tables) => ({ kind: 'notify', databaseName, tables });

test('another tab\'s write reaches .NET', () => {
    const host = fakeHost();
    const target = fakeTarget();
    listen(host, target, 'app.db');

    host.emit(write('app.db', ['product']), { local: false });

    expect(target.calls).toEqual([{ method: 'OnTablesChanged', args: [['product']] }]);
});

// The worker is the source of truth for this tab's own writes too: it knows the exact tables from
// SQLite's update hook and reports them only once committed. The .NET transport declares
// ReportsLocalWrites so the command layer does not raise the same write again from the SQL text.
test("this tab's own write is relayed as well", () => {
    const host = fakeHost();
    const target = fakeTarget();
    listen(host, target, 'app.db');

    host.emit(write('app.db', ['product']), { local: true });

    expect(target.calls).toEqual([{ method: 'OnTablesChanged', args: [['product']] }]);
});

// The broadcast channel is origin-wide, so it carries every database's traffic.
test('a write to a different database is not relayed', () => {
    const host = fakeHost();
    const target = fakeTarget();
    listen(host, target, 'app.db');

    host.emit(write('other.db', ['product']), { local: false });

    expect(target.calls).toEqual([]);
});

test('the method name is overridable and unsubscribing stops the relay', () => {
    const host = fakeHost();
    const target = fakeTarget();
    const stop = listen(host, target, 'app.db', 'Custom');

    host.emit(write('app.db', ['product']), { local: false });
    stop();
    host.emit(write('app.db', ['customer']), { local: false });

    expect(target.calls).toEqual([{ method: 'Custom', args: [['product']] }]);
});

test('a notification with no tables still calls through with an empty list', () => {
    const host = fakeHost();
    const target = fakeTarget();
    listen(host, target, 'app.db');

    host.emit({ kind: 'notify', databaseName: 'app.db' }, { local: false });

    expect(target.calls).toEqual([{ method: 'OnTablesChanged', args: [[]] }]);
});
