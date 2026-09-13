// Runs an assembled Bit.Butil script and reports what it registers - the browser's half of the
// publish-time tree shaking.
//
// The C# side (ScriptBundling.cs) works out *which* modules belong in a bundle and concatenates the
// chunks; everything that can go wrong after that is only visible by evaluating the result: a chunk
// cut short by a bad concatenation, a bundle assembled in an order its modules cannot tolerate, a
// module whose guard stopped protecting it so that loading two overlapping lazy files resets the
// first one's state. None of that shows up in a byte comparison, and all of it surfaces in a
// consumer's browser as "BitButil.x is undefined" or as listeners that silently stop firing.
//
// Given the scripts to evaluate (in order, in one sandbox) and the namespaces they are expected to
// register between them, this checks that:
//
//   1. every script evaluates without throwing;
//   2. window.BitButil ends up holding exactly the expected namespaces - no more (a module that was
//      supposed to be trimmed away) and no fewer (a dependency the closure missed);
//   3. every namespace is usable (an object of callable members, or the version string);
//   4. evaluating the same scripts a second time changes nothing: the per-chunk guards make a
//      re-registration a no-op, which is what lets the lazy module files overlap and what keeps a
//      module's private state (listener registries and the like) alive across a second load.
//
// Usage: node verify-bundle.mjs <expected-key1,key2,...> <script.js> [<script.js> ...]
// Prints one "PASS <what>" / "FAIL <what>: <detail>" line per check; exits non-zero if any failed.

import { readFileSync } from 'node:fs';
import { createContext, runInContext } from 'node:vm';

const [, , expectedArgument, ...scripts] = process.argv;

if (expectedArgument === undefined || scripts.length === 0) {
    console.error('Usage: node verify-bundle.mjs <expected-key1,key2,...> <script.js> [<script.js> ...]');
    process.exit(2);
}

const expected = [...new Set(expectedArgument.split(',').filter(Boolean))].sort();
const label = scripts.map(script => script.split(/[\\/]/).pop()).join(' + ');

let failed = 0;

function check(passed, what, detail) {
    if (passed) {
        console.log(`PASS ${what}`);
        return true;
    }

    console.log(`FAIL ${what}: ${detail}`);
    failed++;
    return false;
}

// The scripts are written for a browser, so they need a window to attach themselves to and enough of
// the DOM for the handful of modules that register a listener as they load. Nothing here has to
// *work* - the modules are only being registered, not called.
function createSandbox() {
    const noop = () => { };
    const sandbox = {};
    sandbox.window = sandbox;
    sandbox.globalThis = sandbox;
    sandbox.self = sandbox;
    sandbox.addEventListener = noop;
    sandbox.removeEventListener = noop;
    sandbox.navigator = { mediaDevices: {}, locks: {} };
    sandbox.location = {};
    sandbox.matchMedia = () => ({ matches: false, media: '', addEventListener: noop });
    sandbox.document = { addEventListener: noop, removeEventListener: noop, createElement: () => ({}), documentElement: {} };
    sandbox.performance = { now: () => 0 };
    sandbox.crypto = { randomUUID: () => '', subtle: {} };
    sandbox.URL = { createObjectURL: () => '', revokeObjectURL: noop };
    sandbox.Blob = function () { };
    sandbox.Response = function () { };
    sandbox.console = console;
    sandbox.localStorage = {};
    sandbox.sessionStorage = {};
    createContext(sandbox);
    return sandbox;
}

function evaluateAll(sandbox, what) {
    for (const script of scripts) {
        try {
            runInContext(readFileSync(script, 'utf8'), sandbox);
        } catch (error) {
            check(false, what, `${script} threw ${error.message}`);
            return false;
        }
    }

    return check(true, what);
}

function registered(sandbox) {
    return Object.keys(sandbox.BitButil ?? {}).sort();
}

function describe(actual) {
    const surplus = actual.filter(key => !expected.includes(key));
    const missing = expected.filter(key => !actual.includes(key));

    return [
        missing.length > 0 ? `missing [${missing.join(', ')}]` : null,
        surplus.length > 0 ? `unexpected [${surplus.join(', ')}]` : null
    ].filter(Boolean).join(', ');
}

const sandbox = createSandbox();

if (evaluateAll(sandbox, `${label} evaluates in a browser-like sandbox`) === false) process.exit(1);

const keys = registered(sandbox);
check(keys.length === expected.length && keys.every((key, index) => key === expected[index]),
    `${label} registers exactly the ${expected.length} expected BitButil namespaces`,
    describe(keys));

// A namespace that is there but empty is the shape a half-written chunk leaves behind, and it reads
// as "present" to every check that only looks at the key.
const hollow = keys.filter(key => {
    const value = sandbox.BitButil[key];
    if (key === 'version') return typeof value !== 'string' || value.length === 0;
    return value === null || typeof value !== 'object' || Object.keys(value).length === 0;
});
check(hollow.length === 0, `${label} registers usable namespaces`, `empty or malformed: [${hollow.join(', ')}]`);

// Mark every namespace object, then run the whole sequence again. A chunk whose guard no longer holds
// re-runs its body, which assigns a fresh object over the namespace - taking the mark, and in a real
// app the module's listener bookkeeping, with it.
const sentinel = '__butilReevaluationSentinel';
// Only namespaces that are objects can carry a mark. Anything else is already reported as hollow above,
// and marking it would throw here or make the reset check below say something it did not measure.
const markable = keys.filter(key => key !== 'version' && sandbox.BitButil[key] !== null && typeof sandbox.BitButil[key] === 'object');
for (const key of markable) {
    sandbox.BitButil[key][sentinel] = true;
}

if (evaluateAll(sandbox, `${label} evaluates a second time without throwing`)) {
    const after = registered(sandbox);
    check(after.length === keys.length && after.every((key, index) => key === keys[index]),
        `${label} registers the same namespaces on a second evaluation`,
        describe(after));

    const reset = markable.filter(key => sandbox.BitButil[key] === null
        || typeof sandbox.BitButil[key] !== 'object'
        || sandbox.BitButil[key][sentinel] !== true);
    check(reset.length === 0,
        `${label} leaves already-registered namespaces untouched on a second evaluation`,
        `re-registered (their guard did not hold): [${reset.join(', ')}]`);
}

process.exit(failed === 0 ? 0 : 1);
