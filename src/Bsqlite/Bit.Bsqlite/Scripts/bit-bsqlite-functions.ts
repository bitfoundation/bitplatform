// The function set EF Core's SQLite provider expects to find on every connection.
//
// EF only installs these when the connection is a real Microsoft.Data.Sqlite connection. Bsqlite
// supplies its own, so the worker has to. The names, arities, and results match
// `BsqliteFunctions` on the .NET side - S4 is the oracle, and the browser suite repeats it here.
//
// Decimals are TEXT. Arithmetic goes through `Decimal`, never through JavaScript numbers: a float
// would make 1/3 and 0.1 silently wrong, and EF's equality is a string compare against the canonical
// form this module writes back.

// Imported dynamically so this module's cache key travels with it: see the note in the worker.
const { Decimal }: typeof import('./bit-bsqlite-decimal.js') =
    await import('./bit-bsqlite-decimal.js' + new URL(import.meta.url).search);
import * as SQLite from './engine/sqlite-constants.js';
import type { Pointer, SQLiteAPI, SQLiteModule } from './engine/sqlite-api.js';
import type { Decimal as DecimalValue } from './bit-bsqlite-decimal.js';

/** The three callbacks an `ef_*` aggregate is made of, over a per-group state object. */
interface Aggregate<TState> {
    create: () => TState;
    step: (state: TState, value: DecimalValue | null) => void;
    final: (state: TState | null) => DecimalValue | null | undefined;
}

const FUNCTION_FLAGS = SQLite.SQLITE_UTF8 | SQLite.SQLITE_DETERMINISTIC | SQLite.SQLITE_INNOCUOUS;

/** One WASM function pointer per module - re-registering the collation must not leak a new one. */
const collationPointers = new WeakMap<SQLiteModule, Pointer>();

/** Per-aggregate instance state, keyed by the pointer `sqlite3_aggregate_context` hands back. */
const aggregateState = new Map<Pointer, unknown>();

/**
 * Forgets any aggregate state left behind by the statement that just finished.
 *
 * Entries normally remove themselves in the final callback, and on the current engine that callback
 * appears to run even for a grouped query abandoned after one row or one that fails mid-step. This
 * does not depend on that. The key is a heap address SQLite frees with the statement and is free to
 * hand out again, so an entry that ever did outlive its statement would look to
 * `getAggregateState` like an accumulator already in progress - and the next `ef_sum` landing on
 * that address would continue someone else's total rather than start at zero, with no error. State
 * is only meaningful within one statement, so dropping it between them costs nothing and removes
 * the possibility.
 */
export function resetAggregateState(): void {
    aggregateState.clear();
}

/**
 * Installs `ef_*`, `EF_DECIMAL`, and `regexp` on an open database. The Emscripten module is needed
 * for the collation and the aggregate context, which wa-sqlite's JavaScript API does not wrap.
 */
export function registerFunctions(module: SQLiteModule, sqlite3: SQLiteAPI, db: number): void {
    const flags = FUNCTION_FLAGS;

    const scalar = (name: string, arity: number, impl: (ctx: Pointer, values: Uint32Array) => void) => {
        sqlite3.create_function(db, name, arity, flags, 0, (ctx, values) => {
            try {
                impl(ctx, values);
            } catch (error) {
                resultError(module, ctx, error);
            }
        });
    };

    const binary = (name: string, apply: (left: DecimalValue, right: DecimalValue) => DecimalValue) => {
        scalar(name, 2, (ctx, values) => {
            const left = readDecimal(sqlite3, values[0]);
            const right = readDecimal(sqlite3, values[1]);
            if (left === null || right === null) {
                sqlite3.result(ctx, null);
                return;
            }

            sqlite3.result(ctx, apply(left, right).toSqlText());
        });
    };

    binary('ef_add', (left, right) => left.add(right));
    binary('ef_multiply', (left, right) => left.multiply(right));

    // Division and modulo return null on a zero divisor - that is EF's contract, not an error - so
    // they write the result themselves rather than going through `binary`.
    sqlite3.create_function(db, 'ef_divide', 2, flags, 0, guarded(module, (ctx, values) => {
        const left = readDecimal(sqlite3, values[0]);
        const right = readDecimal(sqlite3, values[1]);
        if (left === null || right === null || right.coeff === 0n) {
            sqlite3.result(ctx, null);
            return;
        }

        sqlite3.result(ctx, left.divide(right).toSqlText());
    }));

    sqlite3.create_function(db, 'ef_mod', 2, flags, 0, guarded(module, (ctx, values) => {
        const left = readDecimal(sqlite3, values[0]);
        const right = readDecimal(sqlite3, values[1]);
        if (left === null || right === null || right.coeff === 0n) {
            sqlite3.result(ctx, null);
            return;
        }

        sqlite3.result(ctx, left.remainder(right).toSqlText());
    }));

    scalar('ef_negate', 1, (ctx, values) => {
        const value = readDecimal(sqlite3, values[0]);
        sqlite3.result(ctx, value === null ? null : value.negate().toSqlText());
    });

    // A sign, not a bool: EF compiles `a > b` to `ef_compare(a, b) > 0`.
    scalar('ef_compare', 2, (ctx, values) => {
        const left = readDecimal(sqlite3, values[0]);
        const right = readDecimal(sqlite3, values[1]);
        sqlite3.result(ctx, left === null || right === null ? null : left.compareTo(right));
    });

    // SQLite calls regexp(pattern, input) for `input REGEXP pattern` - arguments reversed from
    // Regex.IsMatch. There is no match timeout here: JavaScript's RegExp cannot be interrupted, and
    // the patterns EF emits terminate. Lookaround and backreferences must work; they are why the
    // .NET side does not use RegexOptions.NonBacktracking.
    scalar('regexp', 2, (ctx, values) => {
        const pattern = readText(sqlite3, values[0]);
        const input = readText(sqlite3, values[1]);
        if (pattern === null || input === null) {
            sqlite3.result(ctx, null);
            return;
        }

        sqlite3.result(ctx, compileRegExp(pattern).test(input) ? 1 : 0);
    });

    registerAggregate<{ sum: DecimalValue | null }>(sqlite3, db, module, 'ef_sum', {
        create: () => ({ sum: null }),
        step: (state, value) => {
            if (value === null) {
                return;
            }

            state.sum = state.sum === null ? value : state.sum.add(value);
        },
        final: state => state?.sum ?? null,
    });

    registerAggregate<{ sum: DecimalValue; count: bigint }>(sqlite3, db, module, 'ef_avg', {
        create: () => ({ sum: Decimal.zero(), count: 0n }),
        step: (state, value) => {
            if (value === null) {
                return;
            }

            state.sum = state.sum.add(value);
            state.count += 1n;
        },
        final: state => {
            if (!state || state.count === 0n) {
                return null;
            }

            return state.sum.divide(new Decimal(1, state.count, 0));
        },
    });

    registerAggregate<{ value: DecimalValue | null }>(sqlite3, db, module, 'ef_max', {
        create: () => ({ value: null }),
        step: (state, value) => {
            if (value === null) {
                return;
            }

            if (state.value === null || value.compareTo(state.value) > 0) {
                state.value = value;
            }
        },
        final: state => state?.value ?? null,
    });

    registerAggregate<{ value: DecimalValue | null }>(sqlite3, db, module, 'ef_min', {
        create: () => ({ value: null }),
        step: (state, value) => {
            if (value === null) {
                return;
            }

            if (state.value === null || value.compareTo(state.value) < 0) {
                state.value = value;
            }
        },
        final: state => state?.value ?? null,
    });

    registerCollation(module, db);
}

/**
 * A query applies one pattern to every row it scans, so the compiled RegExp is kept between calls
 * rather than rebuilt per row. Bounded and first-in-first-out: EF sends a handful of distinct
 * patterns, and a query that generates them from data must not grow this without limit.
 */
const REGEXP_CACHE_SIZE = 64;
const regExpCache = new Map<string, RegExp>();

function compileRegExp(pattern: string): RegExp {
    let compiled = regExpCache.get(pattern);
    if (compiled) {
        return compiled;
    }

    compiled = new RegExp(pattern);
    if (regExpCache.size >= REGEXP_CACHE_SIZE) {
        regExpCache.delete(regExpCache.keys().next().value!);
    }

    regExpCache.set(pattern, compiled);
    return compiled;
}

/**
 * `sqlite3_create_collation` is exported from the WASM module but not wrapped by wa-sqlite's JS API,
 * so the compare callback is installed with `addFunction` and the C entry is called directly.
 * That needs a free table slot - `loadEngine` raises the table maximum for this reason.
 */
function registerCollation(module: SQLiteModule, db: number): void {
    let pointer = collationPointers.get(module);
    if (!pointer) {
        pointer = module.addFunction((_pArg: number, n1: number, p1: number, n2: number, p2: number) => {
            const left = module.UTF8ToString(p1, n1);
            const right = module.UTF8ToString(p2, n2);

            try {
                return Decimal.compare(Decimal.parse(left), Decimal.parse(right));
            } catch {
                // A total order is required even for garbage: two undecodable keys must still compare
                // consistently. Byte order of the original text is enough; EF never stores garbage.
                return left < right ? -1 : left > right ? 1 : 0;
            }
        }, 'iiiiii');
        collationPointers.set(module, pointer);
    }

    const rc = module.ccall(
        'sqlite3_create_collation',
        'number',
        ['number', 'string', 'number', 'number', 'number'],
        [db, 'EF_DECIMAL', SQLite.SQLITE_UTF8 | SQLite.SQLITE_DETERMINISTIC, 0, pointer]);

    if (rc !== SQLite.SQLITE_OK) {
        throw new Error(`sqlite3_create_collation(EF_DECIMAL) failed with code ${rc}.`);
    }
}

function registerAggregate<TState>(sqlite3: SQLiteAPI, db: number, module: SQLiteModule, name: string, { create, step, final }: Aggregate<TState>): void {
    sqlite3.create_function(
        db,
        name,
        1,
        FUNCTION_FLAGS,
        0,
        null,
        guarded(module, (ctx: Pointer, values: Uint32Array) => {
            const state = getAggregateState(module, ctx, create);
            step(state, readDecimal(sqlite3, values[0]));
        }),
        guarded(module, (ctx: Pointer) => {
            const state = takeAggregateState<TState>(module, ctx);
            const result = final(state);
            sqlite3.result(ctx, result === null || result === undefined ? null : result.toSqlText());
        }));
}

function getAggregateState<TState>(module: SQLiteModule, ctx: Pointer, create: () => TState): TState {
    const pointer = module._sqlite3_aggregate_context(ctx, 4);
    if (!pointer) {
        throw new Error('sqlite3_aggregate_context could not allocate.');
    }

    let state = aggregateState.get(pointer) as TState | undefined;
    if (!state) {
        state = create();
        aggregateState.set(pointer, state);
    }

    return state;
}

function takeAggregateState<TState>(module: SQLiteModule, ctx: Pointer): TState | null {
    const pointer = module._sqlite3_aggregate_context(ctx, 0);
    if (!pointer) {
        return null;
    }

    const state = (aggregateState.get(pointer) ?? null) as TState | null;
    aggregateState.delete(pointer);
    return state;
}

function readDecimal(sqlite3: SQLiteAPI, pValue: Pointer): DecimalValue | null {
    if (sqlite3.value_type(pValue) === SQLite.SQLITE_NULL) {
        return null;
    }

    return Decimal.parse(sqlite3.value_text(pValue));
}

function readText(sqlite3: SQLiteAPI, pValue: Pointer): string | null {
    if (sqlite3.value_type(pValue) === SQLite.SQLITE_NULL) {
        return null;
    }

    return sqlite3.value_text(pValue);
}

function guarded<TArgs extends [Pointer, ...unknown[]]>(module: SQLiteModule, fn: (...args: TArgs) => void): (...args: TArgs) => void {
    return (...args: TArgs) => {
        try {
            return fn(...args);
        } catch (error) {
            resultError(module, args[0], error);
        }
    };
}

function resultError(module: SQLiteModule, ctx: Pointer, error: unknown): void {
    const message = (error as Error | undefined)?.message ?? String(error);
    module.ccall(
        'sqlite3_result_error',
        null,
        ['number', 'string', 'number'],
        [ctx, message, -1]);
}
