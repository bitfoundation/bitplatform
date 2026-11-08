/**
 * Page route templates (`url.template` on page views): the app's routes - the Blazor `@page` templates .NET finds and
 * hands over in `pageViews.routes` - matched against a page's path the way the Blazor router matches them, and, for a
 * path none matches, the path with the segments that identify an instance replaced.
 */

/** A path with the segments that identify an instance replaced, as C# BelemetryUrlTemplate.TemplatePath does. */
export function templatePath(path: string): string {
    if (!path) return '/';
    return path.split('/').map(segment => {
        if (!segment || segment[0] === '{') return segment;
        let digits = 0;
        let hex = 0;
        let letters = 0;
        for (let i = 0; i < segment.length; i++) {
            const c = segment.charCodeAt(i);
            const digit = c >= 48 && c <= 57;
            const lower = c >= 97 && c <= 122;
            const upper = c >= 65 && c <= 90;
            if (digit) digits++;
            if (digit || (c >= 97 && c <= 102) || (c >= 65 && c <= 70)) hex++;
            if (lower || upper) letters++;
        }
        if (digits === segment.length) return '{n}';
        if (segment.length === 36 && /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(segment)) return '{guid}';
        if (hex === segment.length && segment.length >= 16 && digits > 0) return '{hex}';
        if (segment.length >= 16 && digits > 0 && letters > 0) return '{id}';
        if (segment.indexOf('@') >= 0 || segment.indexOf('[') >= 0) return '{value}';
        return segment;
    }).join('/');
}

type Check = (value: string) => boolean;

interface Parameter {
    checks: Check[];
    constrained: boolean;
    optional: boolean;
    catchAll: boolean;
}

interface Segment {
    /**
     * ASP.NET Core's inbound precedence digit, which the Blazor router orders its routes by: 1 a literal, 2 a constrained
     * parameter or a segment of several parts, 3 a parameter, 4 a constrained catch-all, 5 a catch-all.
     */
    rank: number;
    /** A literal segment, lower-cased (routes match case-insensitively). */
    literal?: string;
    /** A segment of several parts (`{name}.{extension}`): its literals (lower-cased) and parameters, in order. */
    parts?: (string | Parameter)[];
    parameters: Parameter[];
}

export interface CompiledRoute {
    /** The template as the app declared it, with one leading `/` and no trailing one. */
    template: string;
    segments: Segment[];
    precedence: string;
}

const INTEGER = /^\s*([+-]?)0*(\d+)\s*$/;
const DECIMAL = /^\s*[+-]?(?:\d[\d,]*(?:\.\d*)?|\.\d+)\s*$/;
const FLOAT = /^\s*(?:[+-]?(?:\d[\d,]*(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?|[+-]?Infinity|NaN)\s*$/i;
const GUID = /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$/i;

/** Whether `value` is an integer within the range whose ends' digits are `max` and `minAbs` (as .NET's TryParse). */
function integer(value: string, max: string, minAbs: string): boolean {
    const m = INTEGER.exec(value);
    if (!m) return false;
    const digits = m[2];
    const limit = m[1] === '-' ? minAbs : max;
    return digits.length < limit.length || (digits.length === limit.length && digits <= limit);
}

const isLong = (value: string) => integer(value, '9223372036854775807', '9223372036854775808');

function isGuid(value: string): boolean {
    let v = value.trim();
    if ((v[0] === '{' && v[v.length - 1] === '}') || (v[0] === '(' && v[v.length - 1] === ')')) v = v.slice(1, -1);
    return GUID.test(v) && (v.length === 32 || v.length === 36);
}

/** ASP.NET Core's file name test: the last segment has a dot, and something other than dots after its first one. */
function isFileName(value: string): boolean {
    const name = value.slice(value.lastIndexOf('/') + 1);
    const dot = name.indexOf('.');
    return dot >= 0 && /[^.]/.test(name.slice(dot + 1));
}

const TIME = '\\d{1,2}:\\d{2}(?::\\d{2}(?:\\.\\d+)?)?(?:\\s*[ap]\\.?m\\.?)?';
const DATE_TIME = new RegExp('^(?:(\\d{1,4}(?:[-/.,]\\d{1,4}){1,2})(?:(?:t|\\s+)(' + TIME + '))?|(' + TIME + '))(?:\\s*(?:z|[+-]\\d{2}(?::?\\d{2})?|gmt|utc))?$', 'i');
const MONTH_NAME = /(?:^|[^a-z])(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)/i;

function isDate(year: number, month: number, day: number): boolean {
    if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1) return false;
    const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
    return day <= [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1];
}

/** A year as .NET's invariant calendar reads it: one or two digits are 1950 to 2049. */
function fullYear(text: string): number {
    const year = Number(text);
    return text.length > 2 ? year : year < 50 ? 2000 + year : 1900 + year;
}

/** A numeric date as .NET's invariant culture reads it: `y-M-d`, `M/d/y`, `y-M`, `M/y`, or `M/d` of this year. */
function isNumericDate(text: string): boolean {
    const parts = text.split(/[-/.,]/);
    const n = parts.map(Number);
    if (parts.length === 2) {
        if (parts[0].length > 2) return parts[1].length <= 2 && isDate(n[0], n[1], 1);
        if (parts[1].length > 2) return isDate(n[1], n[0], 1);
        return isDate(new Date().getFullYear(), n[0], n[1]);
    }
    if (parts[0].length > 2) return parts[1].length <= 2 && parts[2].length <= 2 && isDate(n[0], n[1], n[2]);
    return parts[1].length <= 2 && isDate(fullYear(parts[2]), n[0], n[1]);
}

function isTime(text: string): boolean {
    const m = /^(\d{1,2}):(\d{2})(?::(\d{2}))?/.exec(text);
    if (!m) return false;
    const hour = Number(m[1]);
    return (/[ap]\.?m\.?$/i.test(text) ? hour >= 1 && hour <= 12 : hour <= 23) && Number(m[2]) <= 59 && Number(m[3] || 0) <= 59;
}

/**
 * .NET's `DateTime.TryParse` with the invariant culture, which the `datetime` constraint runs, for the forms a route
 * value takes: a numeric date, optionally with a time and a zone, or a time alone; a date with a month name is left to
 * `Date.parse`. A bare number is no date (though `Date.parse('42')` is one).
 */
function isDateTime(value: string): boolean {
    const v = value.trim();
    const m = DATE_TIME.exec(v);
    if (m) return m[3] ? isTime(m[3]) : isNumericDate(m[1]) && (!m[2] || isTime(m[2]));
    return MONTH_NAME.test(v) && /\d/.test(v) && !isNaN(Date.parse(v));
}

/** The check of one inline constraint (`int`, `length(2,8)`, `regex(...)`); null for one this side does not know. */
function constraint(spec: string): Check | null {
    const open = spec.indexOf('(');
    const close = spec.lastIndexOf(')');
    const name = (open < 0 ? spec : spec.slice(0, open)).trim().toLowerCase();
    const argument = open < 0 ? '' : spec.slice(open + 1, close > open ? close : spec.length);
    const args = argument.split(',').map(a => Number(a.trim()));
    switch (name) {
        case 'int':
            return v => integer(v, '2147483647', '2147483648');
        case 'long':
            return isLong;
        case 'bool':
            return v => /^\s*(?:true|false)\s*$/i.test(v);
        case 'guid':
            return isGuid;
        case 'decimal':
            return v => DECIMAL.test(v);
        case 'double':
        case 'float':
            return v => FLOAT.test(v);
        case 'datetime':
            return isDateTime;
        case 'alpha':
            return v => /^[a-z]*$/i.test(v);
        case 'nonfile':
            return v => !isFileName(v);
        case 'file':
            return isFileName;
        case 'required':
            return v => v.length > 0;
        case 'length':
            return args.length > 1 ? v => v.length >= args[0] && v.length <= args[1] : v => v.length === args[0];
        case 'minlength':
            return v => v.length >= args[0];
        case 'maxlength':
            return v => v.length <= args[0];
        case 'min':
            return v => isLong(v) && Number(v) >= args[0];
        case 'max':
            return v => isLong(v) && Number(v) <= args[0];
        case 'range':
            return v => isLong(v) && Number(v) >= args[0] && Number(v) <= args[1];
        case 'regex':
            try {
                // As ASP.NET Core's RegexRouteConstraint: case-insensitive, and anchored only if the pattern anchors itself.
                const pattern = new RegExp(argument, 'i');
                return v => pattern.test(v);
            } catch {
                return null;
            }
        default:
            // A constraint of the app's own: it still makes the parameter more specific, but cannot be checked here.
            return null;
    }
}

/** `name`, `*rest`, `id:int`, `id:int?`, `page=1`, `ssn:regex(^\d{3}$)` - the inside of a parameter's braces. */
function parseParameter(inner: string): Parameter {
    let text = inner.trim();
    let catchAll = false;
    if (text.indexOf('**') === 0) {
        catchAll = true;
        text = text.slice(2);
    } else if (text[0] === '*') {
        catchAll = true;
        text = text.slice(1);
    }
    const parameter: Parameter = { checks: [], constrained: false, optional: false, catchAll };
    let pos = 0;
    while (pos < text.length && text[pos] !== ':' && text[pos] !== '=' && text[pos] !== '?') pos++;
    while (pos < text.length) {
        const c = text[pos];
        if (c === ':') {
            const start = ++pos;
            let depth = 0;
            for (; pos < text.length; pos++) {
                const d = text[pos];
                if (d === '(') depth++;
                else if (d === ')') depth = Math.max(0, depth - 1);
                else if (depth === 0 && (d === ':' || d === '=' || (d === '?' && pos === text.length - 1))) break;
            }
            parameter.constrained = true;
            const check = constraint(text.slice(start, pos));
            if (check) parameter.checks.push(check);
        } else {
            // A default value (`=...`) or `?`: the parameter may be left out.
            parameter.optional = !catchAll;
            break;
        }
    }
    return parameter;
}

/** One `/`-separated part of a template; null when it is malformed (an unclosed brace). */
function parseSegment(text: string): Segment | null {
    const parts: (string | Parameter)[] = [];
    let literal = '';
    for (let i = 0; i < text.length;) {
        const c = text[i];
        if (c === '{' && text[i + 1] === '{') {
            literal += '{';
            i += 2;
        } else if (c === '}' && text[i + 1] === '}') {
            literal += '}';
            i += 2;
        } else if (c === '{') {
            // The parameter ends at the first single '}'; '}}' inside it is an escaped brace (in a regex constraint).
            let end = i + 1;
            while (end < text.length && !(text[end] === '}' && text[end + 1] !== '}')) end += text[end] === '}' ? 2 : 1;
            if (end >= text.length) return null;
            if (literal) parts.push(literal);
            literal = '';
            parts.push(parseParameter(text.slice(i + 1, end).replace(/\{\{/g, '{').replace(/\}\}/g, '}')));
            i = end + 1;
        } else {
            literal += c;
            i++;
        }
    }
    if (literal) parts.push(literal);

    if (parts.length === 1 && typeof parts[0] === 'string') return { rank: 1, literal: parts[0].toLowerCase(), parameters: [] };
    if (parts.length === 1) {
        const p = parts[0] as Parameter;
        return { rank: (p.catchAll ? 5 : 3) - (p.constrained ? 1 : 0), parameters: [p] };
    }
    const parameters = parts.filter((p): p is Parameter => typeof p !== 'string');
    return { rank: 2, parts: parts.map(p => (typeof p === 'string' ? p.toLowerCase() : p)), parameters };
}

/** Parses route templates and orders them as the Blazor router tries them: the most specific first. */
export function compileRoutes(templates: readonly string[] | null | undefined): CompiledRoute[] {
    const routes: CompiledRoute[] = [];
    const seen: Record<string, boolean> = {};
    for (const raw of templates || []) {
        if (typeof raw !== 'string') continue;
        const trimmed = raw.trim().replace(/^~/, '').replace(/^\/+|\/+$/g, '');
        const template = '/' + trimmed;
        const key = template.toLowerCase();
        if (seen[key]) continue;
        seen[key] = true;
        const segments: Segment[] = [];
        let valid = true;
        for (const text of trimmed ? trimmed.split('/') : []) {
            const segment = text ? parseSegment(text) : null;
            if (!segment) {
                valid = false;
                break;
            }
            segments.push(segment);
        }
        if (valid) routes.push({ template, segments, precedence: segments.map(s => s.rank).join('') });
    }
    // Digit by digit, as ASP.NET Core compares its decimal precedences: a shorter prefix comes first ("/a" before "/a/{b?}").
    return routes.sort((a, b) => a.precedence < b.precedence ? -1 : a.precedence > b.precedence ? 1
        : a.template.toLowerCase() < b.template.toLowerCase() ? -1 : a.template.toLowerCase() > b.template.toLowerCase() ? 1 : 0);
}

function passes(parameter: Parameter, value: string): boolean {
    for (const check of parameter.checks) {
        if (!check(value)) return false;
    }
    return true;
}

/**
 * ASP.NET Core's RoutePatternMatcher.MatchComplexSegmentCore, from the part at `from` leftwards: each literal at its
 * last occurrence left of what the parts after it took, a parameter the text up to the literal after it. The
 * constraints are checked on that one split only, as the router does.
 */
function matchPartsFrom(parts: (string | Parameter)[], value: string, from: number): boolean {
    const lower = value.toLowerCase();
    const captured: [Parameter, string][] = [];
    let lastIndex = value.length;
    let pending: Parameter | null = null;
    let lastLiteral: string | null = null;
    for (let i = from; i >= 0; i--) {
        const part = parts[i];
        let newLastIndex = lastIndex;
        if (typeof part !== 'string') {
            pending = part;
        } else {
            lastLiteral = part;
            // A parameter waiting for its value needs at least one character.
            const end = pending ? lastIndex - 1 : lastIndex;
            if (end <= 0) return false;
            const index = lower.slice(0, end).lastIndexOf(part);
            if (index < 0) return false;
            // A literal that ends the template ends the value.
            if (i === parts.length - 1 && index + part.length !== value.length) return false;
            newLastIndex = index;
        }
        if (pending && ((lastLiteral !== null && typeof part === 'string') || i === 0)) {
            const start = lastLiteral === null || typeof part !== 'string' ? 0 : newLastIndex + lastLiteral.length;
            const text = value.slice(start, lastIndex);
            if (!text) return false;
            captured.push([pending, text]);
            pending = null;
            lastLiteral = null;
        }
        lastIndex = newLastIndex;
    }
    if (lastIndex !== 0 && typeof parts[0] === 'string') return false;
    return captured.every(([parameter, text]) => passes(parameter, text));
}

/**
 * A segment of several parts, as ASP.NET Core matches it: from the right (`{name}.{ext}` splits `archive.tar.gz` into
 * `archive.tar` and `gz`). An optional last parameter after a `.` may be left out with its dot (`{name}.{ext?}` takes
 * `report`), unless the value ends with the dot.
 */
function matchParts(parts: (string | Parameter)[], value: string): boolean {
    const last = parts.length - 1;
    const tail = parts[last];
    if (typeof tail !== 'string' && tail.optional && parts[last - 1] === '.') {
        if (matchPartsFrom(parts, value, last)) return true;
        return value[value.length - 1] !== '.' && matchPartsFrom(parts, value, last - 2);
    }
    return matchPartsFrom(parts, value, last);
}

/** Whether a route matches a path's (decoded, non-empty) segments. */
export function matchRoute(route: CompiledRoute, segments: string[]): boolean {
    const t = route.segments;
    for (let i = 0; i < t.length; i++) {
        const s = t[i];
        const single = s.literal === undefined && !s.parts ? s.parameters[0] : null;
        if (single && single.catchAll) {
            // The rest of the path, if any; a parameter left out is not checked.
            return i >= segments.length || passes(single, segments.slice(i).join('/'));
        }
        if (i >= segments.length) {
            for (let j = i; j < t.length; j++) {
                const p = t[j].literal === undefined && !t[j].parts ? t[j].parameters[0] : null;
                if (!p || !(p.optional || p.catchAll)) return false;
            }
            return true;
        }
        const value = segments[i];
        if (s.literal !== undefined) {
            if (value.toLowerCase() !== s.literal) return false;
        } else if (s.parts) {
            if (!matchParts(s.parts, value)) return false;
        } else if (!passes(single as Parameter, value)) {
            return false;
        }
    }
    return segments.length === t.length;
}

let compiled: { source: readonly string[]; routes: CompiledRoute[] } | null = null;

function routesOf(templates: readonly string[]): CompiledRoute[] {
    if (!compiled || compiled.source !== templates) compiled = { source: templates, routes: compileRoutes(templates) };
    return compiled.routes;
}

/**
 * The path the app's routes are relative to: the directory of the `<base href>` (Blazor's NavigationManager.BaseUri),
 * or `/` for a page without one.
 */
function basePath(): string {
    try {
        if (typeof document === 'undefined' || !document.querySelector('base[href]')) return '/';
        const path = new URL(document.baseURI).pathname;
        return path.slice(0, path.lastIndexOf('/') + 1) || '/';
    } catch {
        return '/';
    }
}

function decode(segment: string): string {
    try {
        return decodeURIComponent(segment);
    } catch {
        return segment;
    }
}

/**
 * The low-cardinality template of a page's path: the first of the app's routes that matches the path below the base
 * path, after that base path (`/orders/42` -> `/orders/{id:int}`); for a path none matches (or without routes),
 * {@link templatePath} (`/orders/{n}`).
 */
export function pageTemplate(path: string, routes: readonly string[] | null | undefined): string {
    if (routes && routes.length) {
        const base = basePath();
        const lower = path.toLowerCase();
        const relative = lower.indexOf(base.toLowerCase()) === 0 ? path.slice(base.length) : lower + '/' === base.toLowerCase() ? '' : null;
        if (relative !== null) {
            const segments = relative.split('/').filter(s => s.length > 0).map(decode);
            for (const route of routesOf(routes)) {
                if (matchRoute(route, segments)) return base.slice(0, -1) + route.template;
            }
        }
    }
    return templatePath(path);
}
