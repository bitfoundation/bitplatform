import { describe, expect, it } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { applyPrivacy, isVersionKey, luhn, redactQuery, redactUrl, scrub, scrubUrl } from '../../Bit.Belemetry/Scripts/privacy.ts';
import { log, span } from './helpers.js';

const privacy = (overrides = {}) => ({ ...resolveConfig().privacy, ...overrides });

describe('redactUrl', () => {
    it('redacts configured query parameters case-insensitively and strips the fragment', () => {
        expect(redactUrl('https://x.test/cb?Token=abc&page=2&access_token=z#frag', privacy()))
            .toBe('https://x.test/cb?Token=REDACTED&page=2&access_token=REDACTED');
    });

    it('keeps the fragment when configured, redacting parameters in it', () => {
        expect(redactUrl('https://x.test/#access_token=abc&state=1', privacy({ stripFragment: false })))
            .toBe('https://x.test/#access_token=REDACTED&state=1');
    });

    it('strips the whole query string when configured', () => {
        expect(redactUrl('https://x.test/a?b=1#c', privacy({ stripQueryString: true }))).toBe('https://x.test/a');
    });

    it('resolves relative URLs and removes credentials', () => {
        expect(redactUrl('/p?q=1', privacy())).toBe(location.origin + '/p?q=1');
        expect(redactUrl('https://user:pw@x.test/', privacy())).toBe('https://x.test/');
    });

    it('matches encoded parameter names', () => {
        expect(redactUrl('https://x.test/?api%5Fkey=1', privacy())).toBe('https://x.test/?api%5Fkey=REDACTED');
    });

    it('redacts the query of a fragment route (as the C# side does)', () => {
        expect(redactUrl('https://x.test/#/cb?access_token=abc&state=1', privacy({ stripFragment: false })))
            .toBe('https://x.test/#/cb?access_token=REDACTED&state=1');
    });
});

describe('redactQuery', () => {
    it('redacts a bare query string, a prefixed one and a fragment', () => {
        const names = { token: true, code: true };
        expect(redactQuery('token=abc&page=2', names)).toBe('token=REDACTED&page=2');
        expect(redactQuery('?code=1&x=2', names)).toBe('?code=REDACTED&x=2');
        expect(redactQuery('#token=a', names)).toBe('#token=REDACTED');
        expect(redactQuery('page=2', names)).toBe('page=2');
    });
});

describe('scrub', () => {
    it('replaces emails, JWTs and bearer tokens', () => {
        expect(scrub('mail john.doe+x@example.co.uk now')).toBe('mail [email] now');
        expect(scrub('t=eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.sig-_x')).toBe('t=[jwt]');
        expect(scrub('Authorization: Bearer abc.def-123=')).toBe('Authorization: Bearer [token]');
    });

    it('replaces Luhn-valid card numbers only', () => {
        expect(luhn('4111111111111111')).toBe(true);
        expect(luhn('4111111111111112')).toBe(false);
        expect(scrub('card 4111 1111 1111 1111 ok')).toBe('card [card] ok');
        expect(scrub('card 4111-1111-1111-1111')).toBe('card [card]');
        expect(scrub('order 4111111111111112')).toBe('order 4111111111111112');
        expect(scrub('short 4242424242')).toBe('short 4242424242');
    });

    it('replaces IPv4 addresses', () => {
        expect(scrub('from 192.168.0.12:443')).toBe('from [ip]:443');
        expect(scrub('not 999.1.1.1')).toBe('not 999.1.1.1');
    });

    it('keeps .NET assembly versions', () => {
        expect(scrub('System.Private.CoreLib, Version=10.0.0.0, Culture=neutral')).toBe('System.Private.CoreLib, Version=10.0.0.0, Culture=neutral');
        expect(scrub('Version=8.0.0.0 at 10.0.0.1')).toBe('Version=8.0.0.0 at [ip]');
    });
});

describe('applyPrivacy', () => {
    it('cleans log bodies, URL attributes and messages', () => {
        const record = log({
            body: 'failed for a@b.io',
            attributes: { 'url.full': 'https://x.test/?code=1#x', 'page.referrer': 'https://r.test/?email=a@b.io', 'exception.stacktrace': 'at 10.1.2.3', tags: ['a@b.io'], 'session.id': '4111111111111111' },
        });
        applyPrivacy(record, privacy());
        expect(record.body).toBe('failed for [email]');
        expect(record.attributes).toEqual({
            'url.full': 'https://x.test/?code=REDACTED',
            'page.referrer': 'https://r.test/?email=REDACTED',
            'exception.stacktrace': 'at [ip]',
            tags: ['[email]'],
            'session.id': '4111111111111111',
        });
    });

    it('cleans spans and their events', () => {
        const record = span({ status: { code: 2, message: 'Bearer abc' }, events: [{ name: 'e', time: 1, attributes: { 'url.full': 'https://x.test/?secret=1' } }] });
        applyPrivacy(record, privacy());
        expect(record.status.message).toBe('Bearer [token]');
        expect(record.events[0].attributes['url.full']).toBe('https://x.test/?secret=REDACTED');
    });

    it('does not scrub patterns when disabled', () => {
        const record = log({ body: 'a@b.io' });
        applyPrivacy(record, privacy({ scrubPatterns: false }));
        expect(record.body).toBe('a@b.io');
    });

    it('removes blocked attributes from records and span events (exact, case-sensitive keys)', () => {
        const record = span({
            attributes: { 'url.full': 'https://x.test/', 'tenant.plan': 's', 'Tenant.Plan': 'kept' },
            events: [{ name: 'e', time: 1, attributes: { 'tenant.plan': 's', other: 1 } }],
        });
        applyPrivacy(record, privacy({ blockedAttributes: ['tenant.plan'] }));
        expect(record.attributes).toEqual({ 'url.full': 'https://x.test/', 'Tenant.Plan': 'kept' });
        expect(record.events[0].attributes).toEqual({ other: 1 });

        const entry = log({ attributes: { 'tenant.plan': 's' } });
        applyPrivacy(entry, privacy({ blockedAttributes: ['tenant.plan'] }));
        expect(entry.attributes).toEqual({});
    });

    it('redacts the values of sensitive names, whole key or last segment, case-insensitively', () => {
        const record = log({
            attributes: {
                Password: 'hunter2', 'user.password': 'x', 'http.request.header.authorization': ['Basic abc'], 'http.response.header.set-cookie': 'a=b',
                'auth.token': 42, password_policy: 'strict', 'author.name': 'Jane', 'session.id': 'kept',
            },
        });
        applyPrivacy(record, privacy());
        expect(record.attributes).toEqual({
            Password: 'REDACTED', 'user.password': 'REDACTED', 'http.request.header.authorization': 'REDACTED', 'http.response.header.set-cookie': 'REDACTED',
            'auth.token': 'REDACTED', password_policy: 'strict', 'author.name': 'Jane', 'session.id': 'kept',
        });

        const prefixed = log({ attributes: { 'http.request.header.x-tenant': 't', 'other.header': 'h' } });
        applyPrivacy(prefixed, privacy({ redactedAttributes: ['http.request.header.*'] }));
        expect(prefixed.attributes).toEqual({ 'http.request.header.x-tenant': 'REDACTED', 'other.header': 'h' });
    });

    it('keeps the four-part values of version-named keys, as the C# side does', () => {
        const record = log({
            attributes: {
                AppVersion: '1.2.0.0', 'app.version': '10.0.0.1', build_version: '2.0.0.5', Versions: ['1.0.0.0', '1.0.0.1'],
                conversion: '1.2.3.4', 'api.version': 'jane@example.com 1.2.3.4',
            },
        });
        applyPrivacy(record, privacy());
        expect(record.attributes).toEqual({
            AppVersion: '1.2.0.0', 'app.version': '10.0.0.1', build_version: '2.0.0.5', Versions: ['[ip]', '[ip]'],
            conversion: '[ip]', 'api.version': '[email] 1.2.3.4',
        });
        expect(isVersionKey('os.Version')).toBe(true);
        expect(isVersionKey('aversion')).toBe(false);
    });

    it('lets a redacted name win over the host exemption, and never touches the ids', () => {
        const record = log({ attributes: { 'user_agent.original': 'Mozilla/5.0', 'server.address': '10.0.0.5', 'session.id': 's1', 'user.id': 'u1' } });
        applyPrivacy(record, privacy({ redactedAttributes: [...resolveConfig().privacy.redactedAttributes, 'user_agent.original', 'session.id'], blockedAttributes: ['user.id'] }));
        expect(record.attributes).toEqual({ 'user_agent.original': 'REDACTED', 'server.address': '10.0.0.5', 'session.id': 's1', 'user.id': 'u1' });
    });

    it('redacts the signatures of pre-signed storage URLs by default', () => {
        expect(redactUrl('https://b.s3.test/o?X-Amz-Credential=AKIA&X-Amz-Signature=abc&X-Amz-Expires=60', privacy()))
            .toBe('https://b.s3.test/o?X-Amz-Credential=REDACTED&X-Amz-Signature=REDACTED&X-Amz-Expires=60');
        expect(redactUrl('https://storage.test/o?X-Goog-Signature=abc', privacy())).toBe('https://storage.test/o?X-Goog-Signature=REDACTED');
    });

    it('keeps the server host of URLs and server.address, IP literals included', () => {
        const record = span({ attributes: { 'url.full': 'http://10.0.0.5:5000/orders/10.1.2.3?email=a@b.io', 'server.address': '10.0.0.5', 'client.address': '10.9.9.9' } });
        applyPrivacy(record, privacy());
        expect(record.attributes).toEqual({
            'url.full': 'http://10.0.0.5:5000/orders/[ip]?email=REDACTED',
            'server.address': '10.0.0.5',
            'client.address': '[ip]',
        });
        expect(scrubUrl('https://user:pw@x.test/a/jane@example.com')).toBe('https://x.test/a/[email]');
        expect(scrubUrl('from 10.0.0.1')).toBe('from [ip]');
    });

    it('blocks nothing by default', () => {
        expect(resolveConfig().privacy.blockedAttributes).toEqual([]);
    });

    it('redacts url.query like a URL, and drops it with the query string', () => {
        const record = log({ attributes: { 'url.query': 'token=abc&mail=jane@example.com' } });
        applyPrivacy(record, privacy());
        expect(record.attributes['url.query']).toBe('token=REDACTED&mail=[email]');

        const stripped = log({ attributes: { 'url.query': 'a=1', other: 'x' } });
        applyPrivacy(stripped, privacy({ stripQueryString: true }));
        expect(stripped.attributes).toEqual({ other: 'x' });
    });

    it('keeps user_agent.original verbatim (its four-part versions are not addresses)', () => {
        const ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36';
        const record = log({ attributes: { 'user_agent.original': ua, other: 'Chrome/120.0.0.0' } });
        applyPrivacy(record, privacy());
        expect(record.attributes['user_agent.original']).toBe(ua);
        expect(record.attributes.other).toBe('Chrome/[ip]');
    });

    it('scrubs in the C# order: a bearer JWT is a bearer token, an unsigned JWT is a JWT', () => {
        expect(scrub('Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc')).toBe('Authorization: Bearer [token]');
        expect(scrub('unsigned eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0. end')).toBe('unsigned [jwt] end');
        expect(scrub('GET /api/orders')).toBe('GET /api/orders');
        // The pre-checks' boundaries, as in the C# tests.
        expect(scrub('from 1.2.3.4')).toBe('from [ip]');
        expect(scrub('card 4222222222222')).toBe('card [card]');
        expect(scrub('/orders/42?page=2')).toBe('/orders/42?page=2');
    });
});
