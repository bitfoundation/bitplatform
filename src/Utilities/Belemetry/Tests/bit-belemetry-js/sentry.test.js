import { describe, expect, it } from 'vitest';
import {
    createSentryExporter, dotnetFrame, isDotnetStack, level, MAX_FRAMES, parseDotnetStack, parseDsn, parseFrame, parseStack, releaseOf, toEnvelope, toEvents,
} from '../../Bit.Belemetry/Scripts/exporters/sentry.ts';
import { gunzip } from '../../Bit.Belemetry/Scripts/transport.ts';
import { log, span } from './helpers.js';

const DSN = 'https://abc123@o1.ingest.sentry.io/42';
const resource = {
    'service.name': 'shop', 'service.version': '2.0.0', 'service.instance.id': 'inst-1', 'deployment.environment.name': 'Production',
    'telemetry.sdk.language': 'webjs', 'telemetry.sdk.version': '10.6.2', 'user_agent.original': 'UA/1.0', 'browser.language': 'en-US',
};
const context = (overrides = {}) => ({ dsn: parseDsn(DSN), release: null, minimumSeverity: 17, maskIp: true, sdkVersion: '1.0.0', ...overrides });
const error = (overrides = {}) => log({
    severityNumber: 17, severityText: 'ERROR', body: 'x is undefined', eventName: 'exception', scope: 'bit.belemetry.errors', feature: 1,
    attributes: { 'exception.type': 'TypeError', 'exception.message': 'x is undefined', 'exception.stacktrace': 'TypeError: x is undefined\n    at render (https://shop.test/app.js:10:15)' },
    ...overrides,
});
let ids = 0;
const nextId = () => String(++ids).padStart(32, '0');

// The same cases as SentryStackTraceTests (C#), which parses the stacks the browser relays to .NET.
const V8 = [
    'TypeError: x is undefined',
    '    at render (https://shop.test/app.js:10:15)',
    '    at https://shop.test/app.js:20:5',
    '    at async load (https://shop.test/app.js?v=3:30:1)',
    '    at new Cart (https://shop.test/cart.js:40:2)',
    '    at Array.map (<anonymous>)',
    '    at eval (eval at run (https://shop.test/app.js:1:1), <anonymous>:1:1)',
    '    at https://shop.test/_framework/blazor.web.js:1:5000',
].join('\n');
const GECKO = [
    'render@https://shop.test/app.js:10:15',
    'async*load@https://shop.test/app.js:30:1',
    '@https://shop.test/app.js:20:5',
    'run@https://shop.test/app.js line 2 > eval:1:1',
    'lib@https://cdn.test/npm/pkg@1.2.3/x.js:3:4',
    'ext@moz-extension://abc/content.js:1:1',
].join('\n');
const WEBKIT = [
    'render@https://shop.test/app.js:10:15',
    'map@[native code]',
    'global code@app://0.0.0.1/app.js:1:1',
    'Error: mail me@example.com',
].join('\n');

describe('DSN', () => {
    it('names the envelope endpoint, with the key in the query', () => {
        expect(parseDsn(DSN)).toEqual({
            publicKey: 'abc123',
            projectId: '42',
            envelopeEndpoint: 'https://o1.ingest.sentry.io/api/42/envelope/',
            envelopeUrl: 'https://o1.ingest.sentry.io/api/42/envelope/?sentry_key=abc123&sentry_version=7',
            publicDsn: DSN,
        });
    });

    it('keeps a path prefix and a port, and drops a legacy secret key', () => {
        const dsn = parseDsn('http://abc:s3cr3t@sentry.local:9000/sentry/7/');
        expect(dsn.envelopeEndpoint).toBe('http://sentry.local:9000/sentry/api/7/envelope/');
        expect(dsn.publicDsn).toBe('http://abc@sentry.local:9000/sentry/7');
        expect(JSON.stringify(dsn)).not.toContain('s3cr3t');
    });

    it('rejects what is not a DSN', () => {
        for (const value of ['', 'not a url', 'https://o1.ingest.sentry.io/42', 'https://abc@o1.ingest.sentry.io', 'ftp://abc@host/1', null]) {
            expect(parseDsn(value), String(value)).toBeNull();
        }
    });
});

describe('stack parsing', () => {
    it('parses V8 stacks, oldest frame first', () => {
        expect(parseStack(V8)).toEqual([
            { function: '?', filename: 'https://shop.test/_framework/blazor.web.js', abs_path: 'https://shop.test/_framework/blazor.web.js', lineno: 1, colno: 5000, in_app: false },
            { function: 'eval', filename: 'https://shop.test/app.js', abs_path: 'https://shop.test/app.js', lineno: 1, colno: 1, in_app: true },
            { function: 'Array.map', filename: '<anonymous>', abs_path: '<anonymous>', in_app: false },
            { function: 'new Cart', filename: 'https://shop.test/cart.js', abs_path: 'https://shop.test/cart.js', lineno: 40, colno: 2, in_app: true },
            { function: 'load', filename: 'https://shop.test/app.js?v=3', abs_path: 'https://shop.test/app.js?v=3', lineno: 30, colno: 1, in_app: true },
            { function: '?', filename: 'https://shop.test/app.js', abs_path: 'https://shop.test/app.js', lineno: 20, colno: 5, in_app: true },
            { function: 'render', filename: 'https://shop.test/app.js', abs_path: 'https://shop.test/app.js', lineno: 10, colno: 15, in_app: true },
        ]);
    });

    it('parses SpiderMonkey stacks: async frames, eval, and an @ in the URL', () => {
        expect(parseStack(GECKO).reverse().map(f => [f.function, f.filename, f.lineno, f.colno, f.in_app])).toEqual([
            ['render', 'https://shop.test/app.js', 10, 15, true],
            ['load', 'https://shop.test/app.js', 30, 1, true],
            ['?', 'https://shop.test/app.js', 20, 5, true],
            ['run', 'https://shop.test/app.js', 2, undefined, true],
            ['lib', 'https://cdn.test/npm/pkg@1.2.3/x.js', 3, 4, true],
            ['ext', 'moz-extension://abc/content.js', 1, 1, false],
        ]);
    });

    it('parses JavaScriptCore stacks and skips a line that only holds an @', () => {
        expect(parseStack(WEBKIT).reverse().map(f => [f.function, f.filename, f.lineno, f.in_app])).toEqual([
            ['render', 'https://shop.test/app.js', 10, true],
            ['map', '[native code]', undefined, false],
            ['global code', 'app://0.0.0.1/app.js', 1, true],
        ]);
        expect(parseFrame('Error: mail me@example.com')).toBeNull();
        expect(parseFrame('TypeError: x is undefined')).toBeNull();
    });

    it('drops a line number out of range rather than failing (SentryStackTraceTests)', () => {
        expect(parseFrame('at f (https://shop.test/app.js:99999999999:1)')).toEqual({ function: 'f', filename: 'https://shop.test/app.js', abs_path: 'https://shop.test/app.js', colno: 1, in_app: true });
        expect(parseFrame('at f (https://shop.test/app.js:١:٢)').filename).toBe('https://shop.test/app.js:١:٢');
        expect(parseFrame('f@shop:99999999999')).toBeNull();
        expect(dotnetFrame('at X.Y.Z() in /src/a.cs:line ١')).toBeNull();
        expect(dotnetFrame('at X.Y.Z() in /src/a.cs:line 99999999999').lineno).toBeUndefined();
    });

    it('does not count the .NET runtime\'s WebAssembly as the app\'s', () => {
        const frames = parseStack('Error: boom\n    at wasm://wasm/00a8e8e6:wasm-function[8519]:0x1d1d5d\n    at render (https://shop.test/app.js:10:15)');
        expect(frames.map(f => f.in_app)).toEqual([true, false]);
    });

    it('keeps the frames of a JS interop error that carries .NET text', () => {
        const stack = [
            'Error: System.InvalidOperationException: boom',
            '   at Shop.Interop.Save() in /src/Shop/Interop.cs:line 9',
            '   at Shop.Interop.<>c.<Run>b__0_0()',
            '    at Object.invokeMethodAsync (https://shop.test/_framework/blazor.webassembly.js:1:3000)',
            '    at save (https://shop.test/app.js:10:15)',
        ].join('\n');
        expect(isDotnetStack(stack)).toBe(false);
        expect(parseStack(stack).map(f => [f.function, f.lineno, f.in_app])).toEqual([['save', 10, true], ['Object.invokeMethodAsync', 1, false]]);
        expect(isDotnetStack('System.Exception: mail me@example.com:25\n   at X.Y.Z()')).toBe(true);

        const record = error({
            attributes: { 'exception.type': 'Error', 'exception.message': 'System.InvalidOperationException: boom', 'exception.stacktrace': stack },
        });
        const [event] = toEvents({ resource, spans: [], logs: [record] }, context(), nextId);
        expect(event.platform).toBe('javascript');
        expect(event.exception.values[0].stacktrace.frames.map(f => f.function)).toEqual(['save', 'Object.invokeMethodAsync']);
    });

    it('keeps the newest frames of a deep stack', () => {
        const stack = Array.from({ length: MAX_FRAMES + 20 }, (_, i) => `    at f${i} (https://shop.test/app.js:${i + 1}:1)`).join('\n');
        const frames = parseStack(stack);
        expect(frames).toHaveLength(MAX_FRAMES);
        expect(frames[frames.length - 1].function).toBe('f0');
    });
});

// The same text as SentryStackTraceTests.DotnetText (C#).
const DOTNET = [
    'System.InvalidOperationException: outer failed',
    ' ---> System.ArgumentException: inner failed',
    '   at Shop.Orders.Inner() in /src/Shop/Orders.cs:line 12',
    '   at Shop.Orders.Outer() in C:\\src\\Shop\\Orders.cs:line 20',
    '   --- End of inner exception stack trace ---',
    '   at Shop.Orders.Outer() in /src/Shop/Orders.cs:line 24',
    '   at Shop.Orders.<RunAsync>d__3.MoveNext()',
    '--- End of stack trace from previous location ---',
    '   at Shop.Orders.Load[T](String id)',
    '   at Shop.Orders..ctor()',
    '   at Shop.Orders.<>c.<Start>b__4_0()',
    '   at System.Threading.Tasks.Task.Run()',
].join('\r\n');

describe('.NET stacks', () => {
    it('are told from JavaScript stacks by their lines', () => {
        expect(isDotnetStack(DOTNET)).toBe(true);
        for (const stack of [V8, GECKO, WEBKIT, '', null]) expect(isDotnetStack(stack)).toBe(false);
    });

    it('parse into the chain, outermost first, frames oldest first', () => {
        const chain = parseDotnetStack(DOTNET);
        expect(chain).toHaveLength(2);
        expect(chain[0].type).toBe('System.InvalidOperationException');
        expect(chain[0].message).toBe('outer failed');
        expect(chain[0].frames.map(f => f.module + '.' + f.function)).toEqual([
            'System.Threading.Tasks.Task.Run', 'Shop.Orders.<>c.<Start>b__4_0', 'Shop.Orders..ctor', 'Shop.Orders.Load', 'Shop.Orders.RunAsync', 'Shop.Orders.Outer',
        ]);
        expect(chain[0].frames[0].in_app).toBe(false);
        expect(chain[0].frames[5]).toEqual({ function: 'Outer', module: 'Shop.Orders', filename: 'Orders.cs', abs_path: '/src/Shop/Orders.cs', lineno: 24, in_app: true });
        expect(chain[1]).toMatchObject({ type: 'System.ArgumentException', message: 'inner failed' });
        expect(chain[1].frames.map(f => [f.function, f.filename, f.lineno])).toEqual([['Outer', 'Orders.cs', 20], ['Inner', 'Orders.cs', 12]]);
    });

    it('stop at an AggregateException\'s other inner exceptions', () => {
        const chain = parseDotnetStack([
            'System.AggregateException: One or more errors occurred. (a) (b)',
            ' ---> System.Exception: a',
            '   at X.Y.A()',
            '   --- End of inner exception stack trace ---',
            '   at X.Y.Outer()',
            ' ---> (Inner Exception #1) System.Exception: b',
            '   at X.Y.B()<---',
        ].join('\n'));
        expect(chain.map(e => [e.type, e.message, e.frames.map(f => f.function)])).toEqual([
            ['System.AggregateException', 'One or more errors occurred. (a) (b)', ['Outer']],
            ['System.Exception', 'a', ['A']],
        ]);
        expect(dotnetFrame('at App.Program.Main(String[] args) + 0x5a').function).toBe('Main');
    });

    it('leave a message line that starts with "at" in the header', () => {
        const chain = parseDotnetStack([
            'System.InvalidOperationException: Validation failed:',
            'at least one item is required',
            ' ---> System.ArgumentException: empty',
            '   at Shop.Cart.Check() in /src/Shop/Cart.cs:line 5',
            '   --- End of inner exception stack trace ---',
            '   at Shop.Cart.Save() in /src/Shop/Cart.cs:line 9',
        ].join('\n'));
        expect(chain.map(e => [e.type, e.message, e.frames.map(f => f.function)])).toEqual([
            ['System.InvalidOperationException', 'Validation failed:\nat least one item is required', ['Save']],
            ['System.ArgumentException', 'empty', ['Check']],
        ]);
        expect(parseDotnetStack('System.Exception: boom\n   at App!<BaseAddress>+0x1a2b\n   at App!<BaseAddress>+0x3c4d')).toEqual([{ type: 'System.Exception', message: 'boom', frames: [] }]);
    });

    it('name an async lambda\'s or local function\'s state machine as the method it was written as', () => {
        expect(dotnetFrame('at Shop.Cart.<>c__DisplayClass0_0.<<BuildRenderTree>b__0>d.MoveNext() in /src/Cart.razor:line 3'))
            .toMatchObject({ module: 'Shop.Cart.<>c__DisplayClass0_0', function: '<BuildRenderTree>b__0', lineno: 3 });
        expect(dotnetFrame('at Shop.Program.<<Main>g__Local|0_0>d.MoveNext()')).toMatchObject({ module: 'Shop.Program', function: '<Main>g__Local|0_0' });
        expect(dotnetFrame('at Shop.Orders.<Items>d__7.MoveNext()')).toMatchObject({ module: 'Shop.Orders', function: 'Items' });
    });

    it('read a frame as C# does, in one pass', () => {
        // The same lines as SentryStackTraceTests.Dotnet_Frame_IsReadAsThePatternReadsIt_InOnePass, against the pattern
        // the hand-written reader replaced.
        const pattern = /^at ([^\s(]+)\((.*?)\)(?: \+ 0x[0-9a-fA-F]+)?(?: in (.+):line (\d+))?$/;
        const lines = [
            'at A.B.C()', 'at A.B.C(String a, Int32 b)', 'at A.B.C(Func`1 f) + 0x2a', 'at A.B.C() + 0x', 'at A.B.C() + 0x2a in /x.cs:line 4',
            'at A.B.C() in C:\\src\\x.cs:line 12', 'at A.B.C(a) in x:line 1) in y:line 2', 'at A.B.C((x)) in /p (1)/x.cs:line 3', 'at A.B.C() in :line 3',
            'at A.B.C() in x.cs:line ', 'at A.B.C() trailing', 'at A.B.C ()', 'at (x)', 'at A.B.C(', 'at A..ctor()', 'at A.B.C[T](T x) in x.cs:line 9',
            'at A.B.C(x) + 0x1 + 0x2', 'at A.B.C()) in x.cs:line 5', 'at f (https://a/b.js:1:2)', 'at https://a/b.js:1:2',
        ];
        for (const line of lines) {
            const match = pattern.exec(line);
            const frame = dotnetFrame(line);
            expect(frame !== null, line).toBe(match !== null);
            if (!frame) continue;
            expect(frame.abs_path, line).toBe(match[3]);
            expect(frame.lineno, line).toBe(match[4] === undefined ? undefined : Number(match[4]));
        }
        const adversarial = 'at a(' + ') in x:line 1'.repeat(2500) + '!';
        const start = Date.now();
        for (let i = 0; i < 10; i++) expect(dotnetFrame(adversarial)).toBeNull();
        expect(Date.now() - start).toBeLessThan(2000);
    });

    it('make a csharp event when the script recorded a .NET exception', () => {
        const record = error({
            scope: 'Bit.Belemetry.Api', body: 'Order failed',
            attributes: { 'exception.type': 'System.InvalidOperationException', 'exception.message': 'outer failed', 'exception.stacktrace': DOTNET },
        });
        const [event] = toEvents({ resource, spans: [], logs: [record] }, context(), nextId);
        expect(event.platform).toBe('csharp');
        expect(event.sdk.name).toBe('bit.belemetry.javascript');
        const values = event.exception.values;
        expect(values.map(v => [v.type, v.value])).toEqual([['System.ArgumentException', 'inner failed'], ['System.InvalidOperationException', 'outer failed']]);
        expect(values[0].mechanism).toBeUndefined();
        expect(values[1].mechanism).toEqual({ type: 'generic', handled: true });
        expect(values[1].stacktrace.frames.at(-1).function).toBe('Outer');
    });
});

describe('events', () => {
    it('maps severities to levels', () => {
        expect([1, 5, 9, 13, 17, 21].map(level)).toEqual(['debug', 'debug', 'info', 'warning', 'error', 'fatal']);
    });

    it('takes the configured release, else service.name@service.version', () => {
        expect(releaseOf(context(), resource)).toBe('shop@2.0.0');
        expect(releaseOf(context({ release: 'web@9' }), resource)).toBe('web@9');
        expect(releaseOf(context(), { 'service.name': 'shop' })).toBeUndefined();
    });

    it('writes a JS error as an unhandled error event', () => {
        const record = error({
            time: 1700000000123.456, traceId: '0af7651916cd43dd8448eb211c80319c', spanId: 'b7ad6b7169203331',
            attributes: {
                ...error().attributes, 'session.id': 's'.repeat(32), 'enduser.pseudo.id': 'p'.repeat(32), 'user.id': 'alice', tenant: 'contoso', retries: 2,
            },
        });
        const [event] = toEvents({ resource, spans: [], logs: [record] }, context(), nextId);
        expect(event).toEqual({
            event_id: event.event_id,
            timestamp: 1700000000.123,
            platform: 'javascript',
            level: 'error',
            logger: 'bit.belemetry.errors',
            release: 'shop@2.0.0',
            environment: 'Production',
            exception: {
                values: [{
                    type: 'TypeError',
                    value: 'x is undefined',
                    stacktrace: { frames: [{ function: 'render', filename: 'https://shop.test/app.js', abs_path: 'https://shop.test/app.js', lineno: 10, colno: 15, in_app: true }] },
                    mechanism: { type: 'generic', handled: false },
                }],
            },
            contexts: {
                trace: { trace_id: '0af7651916cd43dd8448eb211c80319c', span_id: 'b7ad6b7169203331', type: 'trace' },
                culture: { locale: 'en-US', type: 'culture' },
                otel: { resource, type: 'otel' },
            },
            tags: { 'session.id': 's'.repeat(32) },
            user: { id: 'alice' },
            request: { headers: { 'User-Agent': 'UA/1.0' } },
            extra: { 'enduser.pseudo.id': 'p'.repeat(32), tenant: 'contoso', retries: 2 },
            sdk: { name: 'bit.belemetry.javascript', version: '10.6.2', settings: { infer_ip: 'never' } },
        });
        expect(event.event_id).toMatch(/^[0-9a-f]{32}$/);
    });

    it('marks an exception of another scope handled, and keeps a body that says more', () => {
        const [event] = toEvents({ resource, spans: [], logs: [error({ scope: 'app.checkout', body: 'Checkout failed' })] }, context(), nextId);
        expect(event.exception.values[0].mechanism).toEqual({ type: 'generic', handled: true });
        expect(event.message).toEqual({ formatted: 'Checkout failed' });
        expect(toEvents({ resource, spans: [], logs: [error()] }, context(), nextId)[0].message, 'The body is the exception message.').toBeUndefined();
    });

    it('turns plain logs at the level into message events, and nothing else', () => {
        const logs = [
            log({ severityNumber: 17, body: 'console error', scope: 'bit.belemetry.console' }),
            log({ severityNumber: 13, body: 'a warning' }),
            log({ severityNumber: 17, body: 'a page view', eventName: 'browser.page_view' }),
            log({ severityNumber: 17, body: '' }),
        ];
        const events = toEvents({ resource, spans: [], logs }, context(), nextId);
        expect(events.map(e => e.message.formatted)).toEqual(['console error']);
        expect(events[0].exception).toBeUndefined();
        expect(events[0].level).toBe('error');

        const warnings = toEvents({ resource, spans: [], logs }, context({ minimumSeverity: 13 }), nextId);
        expect(warnings.map(e => e.message.formatted)).toEqual(['console error', 'a warning']);
        expect(toEvents({ resource, spans: [], logs: [error()] }, context({ minimumSeverity: 21 }), nextId)).toHaveLength(0);
    });

    it('turns the exceptions recorded on a span into events, unless the error limits dropped them', () => {
        const recorded = span({
            parentSpanId: '00f067aa0ba902b7',
            attributes: { 'session.id': 's'.repeat(32), 'user.id': 'alice' },
            scope: 'app.orders',
            events: [
                { name: 'exception', time: 1700000000200, attributes: { 'exception.type': 'Error', 'exception.message': 'boom', extra: 1 } },
                { name: 'exception', time: 1700000000201, attributes: { 'exception.type': 'Error', 'exception.message': 'limited' }, limited: true },
                { name: 'exception', time: 1700000000202, attributes: { 'exception.stacktrace': 'at x (https://a/b.js:1:1)' } },
                { name: 'retry', time: 1700000000203, attributes: {} },
            ],
        });
        const events = toEvents({ resource, spans: [recorded], logs: [] }, context(), nextId);
        expect(events).toHaveLength(1);
        const [event] = events;
        expect(event.transaction).toBe('GET');
        expect(event.logger).toBe('app.orders');
        expect(event.level).toBe('error');
        expect(event.timestamp).toBe(1700000000.2);
        expect(event.contexts.trace).toEqual({ trace_id: recorded.traceId, span_id: recorded.spanId, parent_span_id: '00f067aa0ba902b7', type: 'trace' });
        expect(event.tags).toEqual({ 'session.id': 's'.repeat(32) });
        expect(event.user).toEqual({ id: 'alice' });
        expect(event.extra).toEqual({ extra: 1 });
        expect(event.exception.values[0].mechanism.handled).toBe(true);
    });

    it('links no trace for an all-zero id (SentryEnvelopeSerializerTests)', () => {
        const record = error({ traceId: '0'.repeat(32), spanId: '0'.repeat(16) });
        expect(toEvents({ resource, spans: [], logs: [record] }, context(), nextId)[0].contexts.trace).toBeUndefined();
    });

    it('tells Sentry to infer the IP address only when asked', () => {
        expect(toEvents({ resource, spans: [], logs: [error()] }, context({ maskIp: false }), nextId)[0].sdk.settings).toEqual({ infer_ip: 'auto' });
    });
});

describe('envelopes', () => {
    const parse = bytes => {
        const text = new TextDecoder().decode(bytes);
        expect(text.endsWith('\n')).toBe(true);
        const lines = text.split('\n');
        expect(lines).toHaveLength(4);
        return { header: JSON.parse(lines[0]), item: JSON.parse(lines[1]), event: JSON.parse(lines[2]), eventBytes: new TextEncoder().encode(lines[2]).length };
    };

    it('holds one event, with its length in bytes', () => {
        const [event] = toEvents({ resource, spans: [], logs: [error({ attributes: { ...error().attributes, 'exception.message': 'café ☕' } })] }, context(), nextId);
        const { header, item, event: parsed, eventBytes } = parse(toEnvelope(event, context()));
        expect(header).toEqual({ event_id: event.event_id, dsn: DSN, sdk: { name: 'bit.belemetry.javascript', version: '10.6.2' } });
        expect(item).toEqual({ type: 'event', length: eventBytes });
        expect(parsed).toEqual(event);
    });

    const sent = () => {
        const requests = [];
        return { requests, transport: { send: request => { requests.push(request); return Promise.resolve(); } } };
    };

    it('posts one request per event to the envelope endpoint', async () => {
        const { requests, transport } = sent();
        const exporter = createSentryExporter({ type: 'sentry', dsn: DSN }, transport, '1.0.0');
        expect(exporter.urls).toEqual(['https://o1.ingest.sentry.io/api/42/envelope/']);
        await exporter.export({ resource, spans: [], logs: [error(), log(), error({ body: 'second' })] }, false);
        expect(requests).toHaveLength(2);
        expect(requests[0]).toMatchObject({
            exporter: 'sentry', url: 'https://o1.ingest.sentry.io/api/42/envelope/?sentry_key=abc123&sentry_version=7',
            contentType: 'application/x-sentry-envelope', contentEncoding: null,
        });
        expect(parse(requests[1].body).event.message).toEqual({ formatted: 'second' });
    });

    it('defaults to errors, and takes 0 for everything', async () => {
        const plain = sent();
        await createSentryExporter({ type: 'sentry', dsn: DSN }, plain.transport, '1.0.0').export({ resource, spans: [], logs: [log({ severityNumber: 13 })] }, false);
        expect(plain.requests).toHaveLength(0);
        const all = sent();
        await createSentryExporter({ type: 'sentry', dsn: DSN, minimumSeverity: 0 }, all.transport, '1.0.0').export({ resource, spans: [], logs: [log({ severityNumber: 5 })] }, false);
        expect(all.requests).toHaveLength(1);
    });

    it('compresses with gzip, except during unload', async () => {
        const zipped = sent();
        const exporter = createSentryExporter({ type: 'sentry', dsn: DSN, compression: 'gzip' }, zipped.transport, '1.0.0');
        await exporter.export({ resource, spans: [], logs: [error()] }, false);
        expect(zipped.requests[0].contentEncoding).toBe('gzip');
        expect(await gunzip(zipped.requests[0].body)).toEqual(zipped.requests[0].raw);

        const unload = sent();
        await createSentryExporter({ type: 'sentry', dsn: DSN, compression: 'gzip' }, unload.transport, '1.0.0').export({ resource, spans: [], logs: [error()] }, true);
        expect(unload.requests[0].contentEncoding).toBeNull();
    });

    it('sends nothing with an invalid DSN', async () => {
        const { requests, transport } = sent();
        const exporter = createSentryExporter({ type: 'sentry', dsn: 'nope' }, transport, '1.0.0');
        expect(exporter.urls).toEqual([]);
        await exporter.export({ resource, spans: [], logs: [error()] }, false);
        expect(requests).toHaveLength(0);
    });
});
