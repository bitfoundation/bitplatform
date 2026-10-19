// Rewrites wa-sqlite example imports so they resolve from a provider package.
//
// IDBBatchAtomicVFS also pulls WebLocksMixin, which OPFS CoopSync does not. The rewrite is
// mechanical and checksummed against the untouched download.

import { readFileSync, writeFileSync } from 'node:fs';

const [, , input, output] = process.argv;

if (!input || !output) {
  throw new Error('Usage: rewrite-imports.mjs <upstream.js> <wwwroot.js>');
}

const replacements = [
  ["from '../FacadeVFS.js'", "from '../Bit.Bsqlite/engine/FacadeVFS.js'"],
  ["from '../VFS.js'", "from '../Bit.Bsqlite/engine/VFS.js'"],
  ["from '../WebLocksMixin.js'", "from '../Bit.Bsqlite/engine/WebLocksMixin.js'"],
];

let source = readFileSync(input, 'utf8');

for (const [from, to] of replacements) {
  if (!source.includes(from)) {
    throw new Error(`Expected import ${from} was not in the upstream VFS. The pin may have moved.`);
  }

  source = source.replaceAll(from, to);
}

writeFileSync(output, source);
