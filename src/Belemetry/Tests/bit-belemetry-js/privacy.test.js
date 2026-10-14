import { describe, expect, it } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { applyPrivacy, luhn, redactUrl, scrub } from '../../Bit.Belemetry/Scripts/privacy.ts';
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
});
