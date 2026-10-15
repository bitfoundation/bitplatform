import type { PrivacyConfig } from './config';
import { isSpan, type Attributes, type TelemetryRecord } from './model';

// Attributes that hold URLs: their query parameters and fragment are redacted before scrubbing.
const URL_ATTRIBUTES = ['url.full', 'page.referrer', 'url.original', 'http.url'];

// Attributes added by the enrichment step after privacy: ids, never scrubbed.
const ID_ATTRIBUTES = ['session.id', 'enduser.pseudo.id', 'user.id'];

const BEARER = /\bBearer\s+[A-Za-z0-9\-._~+/]+=*/gi;
const JWT = /\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*/g;
const EMAIL = /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g;
const CARD = /\b\d(?:[ -]?\d){12,18}\b/g;
// Built with the constructor because lookbehind needs ES2018 regex support that the ES2019 tsc/esbuild target
// would otherwise reject or warn about in a literal. It keeps .NET assembly versions ("Version=10.0.0.0") in stack
// traces intact, matching the C# scrubber.
const IPV4 = new RegExp('(?<!Version=)\\b(?:(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)\\.){3}(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)\\b', 'g');

/** The Luhn checksum used by payment card numbers. */
export function luhn(digits: string): boolean {
    let sum = 0;
    let double = false;
    for (let i = digits.length - 1; i >= 0; i--) {
        let d = digits.charCodeAt(i) - 48;
        if (d < 0 || d > 9) return false;
        if (double) {
            d *= 2;
            if (d > 9) d -= 9;
        }
        sum += d;
        double = !double;
    }
    return sum % 10 === 0;
}

/** Replaces emails, JWTs, bearer tokens, card numbers (Luhn-valid, 13-19 digits) and IPv4 addresses. */
export function scrub(text: string): string {
    if (!text) return text;
    return text
        .replace(BEARER, 'Bearer [token]')
        .replace(JWT, '[jwt]')
        .replace(EMAIL, '[email]')
        .replace(CARD, match => {
            const digits = match.replace(/[ -]/g, '');
            return digits.length >= 13 && digits.length <= 19 && luhn(digits) ? '[card]' : match;
        })
        .replace(IPV4, '[ip]');
}

/**
 * Redacts the configured query parameters (case-insensitive, value replaced by `REDACTED`), removes credentials,
 * and strips the query string and the fragment when configured. Parameters in a kept fragment are redacted too
 * (OAuth implicit flows put tokens there). Relative URLs are resolved against the page.
 */
export function redactUrl(url: string, privacy: PrivacyConfig): string {
    if (!url) return url;
    let parsed: URL;
    try {
        parsed = new URL(url, typeof location !== 'undefined' ? location.href : undefined);
    } catch {
        return url;
    }
    if (parsed.username || parsed.password) {
        parsed.username = '';
        parsed.password = '';
    }
    const href = parsed.href;
    const search = parsed.search;
    const hash = parsed.hash;
    const base = href.slice(0, href.length - search.length - hash.length);
    const names: Record<string, true> = {};
    for (const name of privacy.redactQueryParameters || []) names[String(name).toLowerCase()] = true;
    const redact = (part: string) => part.replace(/([?&#])([^=&#]*)=([^&#]*)/g, (match, sep: string, key: string) => {
        let name = key;
        try {
            name = decodeURIComponent(key.replace(/\+/g, ' '));
        } catch {
            // Keep the raw name.
        }
        return names[name.toLowerCase()] ? sep + key + '=REDACTED' : match;
    });
    return base + (privacy.stripQueryString ? '' : redact(search)) + (privacy.stripFragment ? '' : redact(hash));
}

// The server that was called, not the user: an IP literal here is kept (as in the host of a URL).
const HOST_ATTRIBUTES = ['server.address'];

/**
 * Scrubs a URL after its origin: the host (an IP literal included) names the server that was called, not the user,
 * and "http://[ip]:5000/" would hide which backend failed. Matches the C# scrubber.
 */
export function scrubUrl(url: string): string {
    const schemeEnd = url.indexOf('://');
    if (schemeEnd < 0) return scrub(url);
    const authorityStart = schemeEnd + 3;
    let pathStart = url.length;
    for (const c of ['/', '?', '#']) {
        const index = url.indexOf(c, authorityStart);
        if (index >= 0 && index < pathStart) pathStart = index;
    }
    let authority = url.slice(authorityStart, pathStart);
    const at = authority.lastIndexOf('@');
    if (at >= 0) authority = authority.slice(at + 1);
    return url.slice(0, authorityStart) + authority + scrub(url.slice(pathStart));
}

function clean(attributes: Attributes, privacy: PrivacyConfig): void {
    const blocked = privacy.blockedAttributes || [];
    for (const key of Object.keys(attributes)) {
        if (ID_ATTRIBUTES.indexOf(key) >= 0) continue;
        if (blocked.indexOf(key) >= 0) {
            delete attributes[key];
            continue;
        }
        const value = attributes[key];
        if (typeof value === 'string') {
            if (HOST_ATTRIBUTES.indexOf(key) >= 0) continue;
            const isUrl = URL_ATTRIBUTES.indexOf(key) >= 0;
            let next = isUrl ? redactUrl(value, privacy) : value;
            if (privacy.scrubPatterns) next = isUrl ? scrubUrl(next) : scrub(next);
            attributes[key] = next;
        } else if (privacy.scrubPatterns && Array.isArray(value) && typeof value[0] === 'string') {
            attributes[key] = (value as string[]).map(scrub);
        }
    }
}

/** Removes the blocked attributes, and applies URL redaction and pattern scrubbing to a record in place. */
export function applyPrivacy(record: TelemetryRecord, privacy: PrivacyConfig): void {
    clean(record.attributes, privacy);
    if (isSpan(record)) {
        if (privacy.scrubPatterns && record.status.message) record.status.message = scrub(record.status.message);
        for (const event of record.events) clean(event.attributes, privacy);
    } else if (privacy.scrubPatterns) {
        record.body = scrub(record.body);
    }
}
