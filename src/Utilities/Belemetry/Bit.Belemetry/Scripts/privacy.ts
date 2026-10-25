import type { PrivacyConfig } from './config';
import { isSpan, type Attributes, type TelemetryRecord } from './model';

// Attributes that hold URLs: their query parameters and fragment are redacted before scrubbing.
const URL_ATTRIBUTES = ['url.full', 'page.referrer', 'url.original', 'http.url'];

// Attributes added by the enrichment step after privacy: ids, never scrubbed - only blocked, by the operator's choice.
const ID_ATTRIBUTES = ['session.id', 'enduser.pseudo.id', 'user.id'];

/** Whether a key is one of `blockedAttributes` (exact, case-sensitive): removed from every record, and an id is not added. */
export function isBlocked(key: string, privacy: PrivacyConfig): boolean {
    const blocked = privacy.blockedAttributes;
    return !!blocked && blocked.length > 0 && blocked.indexOf(key) >= 0;
}

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

const BEARER_WORD = /bearer/i;
const ABSOLUTE = /^https?:\/\//i;
const QUERY_FRAGMENT_OR_CREDENTIALS = /[?#@]/;

/**
 * Replaces bearer tokens, JWTs, emails, card numbers (Luhn-valid, 13-19 digits) and IPv4 addresses, in that order
 * (the C# scrubber's order too, so both sides give the same text).
 */
export function scrub(text: string, addresses = true): string {
    if (!text) return text;
    // Every string attribute goes through here and most match no pattern at all: a scan per pattern is far cheaper
    // than running its regex.
    if (BEARER_WORD.test(text)) text = text.replace(BEARER, 'Bearer [token]');
    if (text.indexOf('eyJ') >= 0) text = text.replace(JWT, '[jwt]');
    if (text.indexOf('@') >= 0) text = text.replace(EMAIL, '[email]');
    // A card number needs 13 digits and an IPv4 address 3 dots: most values with a number in them have neither, and
    // one pass over them is cheaper than the two regexes (as in the C# scrubber).
    let digits = 0;
    let dots = 0;
    for (let i = 0; i < text.length; i++) {
        const c = text.charCodeAt(i);
        if (c >= 48 && c <= 57) digits++;
        else if (c === 46) dots++;
    }
    if (digits >= 13) {
        text = text.replace(CARD, match => {
            const cardDigits = match.replace(/[ -]/g, '');
            return cardDigits.length >= 13 && cardDigits.length <= 19 && luhn(cardDigits) ? '[card]' : match;
        });
    }
    if (addresses && digits >= 4 && dots >= 3) text = text.replace(IPV4, '[ip]');
    return text;
}

/**
 * Whether a key names a version (`version` as its last segment, or ending in `_version` or a PascalCase `Version`):
 * its four-part values are versions, not addresses. Matches the C# side.
 */
export function isVersionKey(key: string): boolean {
    const last = key.slice(key.lastIndexOf('.') + 1);
    return last.toLowerCase() === 'version' || /_version$/i.test(key) || /Version$/.test(key);
}

let names: { source: string[] | undefined; map: Record<string, true> } | null = null;

// Built once per configured list, not once per URL. Without a prototype, so a parameter named like one of its members
// ("constructor", "__proto__") is not found in every map.
function redactionNames(privacy: PrivacyConfig): Record<string, true> {
    if (names && names.source === privacy.redactQueryParameters) return names.map;
    const map: Record<string, true> = Object.create(null);
    for (const name of privacy.redactQueryParameters || []) map[String(name).toLowerCase()] = true;
    names = { source: privacy.redactQueryParameters, map };
    return map;
}

/** Replaces the value of every configured parameter in a query string or fragment (`?a=1&b=2`, `#a=1`, `a=1`). */
export function redactQuery(part: string, names: Record<string, true>): string {
    return part.replace(/(^|[?&#])([^=&#?]*)=([^&#]*)/g, (match, sep: string, key: string) => {
        let name = key;
        try {
            name = decodeURIComponent(key.replace(/\+/g, ' '));
        } catch {
            // Keep the raw name.
        }
        return names[name.toLowerCase()] === true ? sep + key + '=REDACTED' : match;
    });
}

/**
 * Redacts the configured query parameters (case-insensitive, value replaced by `REDACTED`), removes credentials,
 * and strips the query string and the fragment when configured. Parameters in a kept fragment are redacted too
 * (OAuth implicit flows put tokens there). Relative URLs are resolved against the page.
 */
export function redactUrl(url: string, privacy: PrivacyConfig): string {
    if (!url) return url;
    // An absolute URL without a query, a fragment or credentials has nothing to redact, and parsing it costs more than
    // the rest of the record's privacy rules together.
    if (ABSOLUTE.test(url) && !QUERY_FRAGMENT_OR_CREDENTIALS.test(url)) return url;
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
    const names = redactionNames(privacy);
    return base + (privacy.stripQueryString ? '' : redactQuery(search, names)) + (privacy.stripFragment ? '' : redactQuery(hash, names));
}

// Kept verbatim, as the C# scrubber keeps them: the server that was called, not the user (an IP literal here is kept,
// as in the host of a URL); and the user agent, whose four-part versions ("Chrome/120.0.0.0") read as IPv4 addresses.
const HOST_ATTRIBUTES = ['server.address', 'user_agent.original'];

// A bare query string (without the `?`, per the conventions): its parameters are redacted like a URL's.
const URL_QUERY = 'url.query';

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

interface RedactionRules {
    source: string[] | undefined;
    names: Record<string, true>;
    prefixes: string[];
    verdicts: Map<string, boolean>;
}

// Attribute names are few and repeat in every record: the verdict per key is cached (bounded), and rebuilt when the
// configured list changes. A Map and a prototype-less object: a key named like an Object member ("constructor",
// "toString") must not find that member.
const MAX_CACHED_KEYS = 1024;
let rules: RedactionRules | null = null;

function redactionRules(privacy: PrivacyConfig): RedactionRules {
    if (rules && rules.source === privacy.redactedAttributes) return rules;
    const names: Record<string, true> = Object.create(null);
    const prefixes: string[] = [];
    for (const entry of privacy.redactedAttributes || []) {
        const name = String(entry).toLowerCase();
        if (name.charAt(name.length - 1) === '*') prefixes.push(name.slice(0, -1));
        else names[name] = true;
    }
    return (rules = { source: privacy.redactedAttributes, names, prefixes, verdicts: new Map() });
}

/**
 * Whether an attribute key is one of `redactedAttributes`: the key whole or its last dot-separated segment equals a
 * name (case-insensitively), or the key starts with a prefix pattern (`http.request.header.*`). Matches the C# side.
 */
export function isRedacted(key: string, privacy: PrivacyConfig): boolean {
    const r = redactionRules(privacy);
    const cached = r.verdicts.get(key);
    if (cached !== undefined) return cached;
    const lower = key.toLowerCase();
    const dot = lower.lastIndexOf('.');
    let redacted = r.names[lower] === true || (dot >= 0 && dot < lower.length - 1 && r.names[lower.slice(dot + 1)] === true);
    for (const prefix of r.prefixes) redacted = redacted || lower.indexOf(prefix) === 0;
    if (r.verdicts.size < MAX_CACHED_KEYS) r.verdicts.set(key, redacted);
    return redacted;
}

function clean(attributes: Attributes, privacy: PrivacyConfig): void {
    for (const key of Object.keys(attributes)) {
        if (isBlocked(key, privacy)) {
            delete attributes[key];
            continue;
        }
        if (ID_ATTRIBUTES.indexOf(key) >= 0) continue;
        if (key === URL_QUERY && privacy.stripQueryString) {
            delete attributes[key];
            continue;
        }
        if (isRedacted(key, privacy)) {
            attributes[key] = 'REDACTED';
            continue;
        }
        const value = attributes[key];
        if (typeof value === 'string') {
            if (HOST_ATTRIBUTES.indexOf(key) >= 0) continue;
            const isUrl = URL_ATTRIBUTES.indexOf(key) >= 0;
            let next = isUrl ? redactUrl(value, privacy) : key === URL_QUERY ? redactQuery(value, redactionNames(privacy)) : value;
            if (privacy.scrubPatterns) next = isUrl ? scrubUrl(next) : scrub(next, !isVersionKey(key));
            attributes[key] = next;
        } else if (privacy.scrubPatterns && Array.isArray(value) && typeof value[0] === 'string') {
            // Not .map(scrub): map passes the index as the second argument.
            const addresses = !isVersionKey(key);
            attributes[key] = (value as string[]).map(item => scrub(item, addresses));
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
