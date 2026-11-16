import { test, expect } from 'vitest';
import { displayName, pathOf } from '../../Bit.Bsqlite/Scripts/bit-bsqlite-idb-admin.ts';

// The IndexedDB admin listed a database by its storage key, so `My Files.db` came back as `My%20Files.db` - a name
// the open refuses, as it is the key's own spelling. A listed name is now the one the database was opened under.
test.each([
    'app.db',
    'My Files.db',
    'café.db',
    'データ.db',
    'dir/My Files.db',
    '50%off.db',
    'a%41.db',
    'a%2Fb.db',
    '100%.db',
])('lists %s under the name it was opened with', name => {
    expect(displayName(pathOf(name))).toBe(name);
});

// A segment is decoded only when the decoded text keys back to it, so keys that differ only by an escape the URL
// parser leaves alone stay apart.
test('never lists two keys under one name', () => {
    const keys = ['/a%20b.db', '/aA.db', '/a%41.db', '/a/b.db', '/a%2Fb.db', '/dir/a%20b.db', '/caf%C3%A9.db', '/50%off.db'];

    expect(keys.map(displayName)).toEqual(['a b.db', 'aA.db', 'a%41.db', 'a/b.db', 'a%2Fb.db', 'dir/a b.db', 'café.db', '50%off.db']);
});
