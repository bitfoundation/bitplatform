var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function parseUrl(url: string, base?: string | null) {
        try { return base ? new URL(url, base) : new URL(url); } catch { return null; }
    }

    function toParts(parsed: URL) {
        return {
            href: parsed.href,
            origin: parsed.origin,
            protocol: parsed.protocol,
            username: parsed.username,
            password: parsed.password,
            host: parsed.host,
            hostname: parsed.hostname,
            port: parsed.port,
            pathname: parsed.pathname,
            search: parsed.search,
            hash: parsed.hash
        };
    }

    // URLPattern takes either a pattern string or an object of per-component patterns; the string
    // form needs a base to resolve against unless it is absolute, which is why both are accepted.
    function makePattern(pattern: string, base?: string | null) {
        const ctor = (window as any).URLPattern;
        if (typeof ctor !== 'function') return null;
        try { return base ? new ctor(pattern, base) : new ctor(pattern); } catch { return null; }
    }

    // The named groups of every component in one map. A pattern names each group once across the
    // whole URL, so flattening loses nothing and spares the caller a component-shaped object; the
    // component a group came from is still in `components` below.
    function flattenGroups(result: any) {
        const groups: { [key: string]: string | null } = {};
        for (const component of ['protocol', 'username', 'password', 'hostname', 'port', 'pathname', 'search', 'hash']) {
            const matched = result[component];
            if (!matched?.groups) continue;
            for (const key of Object.keys(matched.groups)) {
                // Positional groups are keyed '0', '1', ... in every component, so a later component
                // would overwrite an earlier one's. First writer wins, matching left-to-right order.
                if (key in groups) continue;
                groups[key] = matched.groups[key] ?? null;
            }
        }
        return groups;
    }

    butil.url = {
        isSupported() { return typeof URL === 'function'; },
        canParse(url: string, base?: string | null) {
            // URL.canParse is the cheap path; older runtimes only have the throwing constructor.
            const can = (URL as any).canParse;
            if (typeof can === 'function') { try { return base ? can(url, base) : can(url); } catch { return false; } }
            return parseUrl(url, base) !== null;
        },
        parse(url: string, base?: string | null) {
            const parsed = parseUrl(url, base);
            return parsed ? toParts(parsed) : null;
        },
        // Absolute form of a possibly relative reference, the way the browser's own resolver does it
        // (including the '..' and scheme-relative cases System.Uri handles differently).
        resolve(url: string, base: string) {
            const parsed = parseUrl(url, base);
            return parsed ? parsed.href : null;
        },

        parseQuery(query: string) {
            const params = new URLSearchParams(query ?? '');
            // As a flat list rather than a map: a query string may repeat a key, and a map would
            // silently drop all but one of them.
            const pairs: { key: string, value: string }[] = [];
            params.forEach((value, key) => pairs.push({ key, value }));
            return pairs;
        },
        buildQuery(pairs: any[]) {
            const params = new URLSearchParams();
            for (const pair of pairs ?? []) params.append(pair.key, pair.value ?? '');
            return params.toString();
        },
        getQueryValues(query: string, key: string) {
            return new URLSearchParams(query ?? '').getAll(key);
        },
        // Whole-URL edits, so the caller never has to re-assemble the parts by hand.
        setQuery(url: string, pairs: any[]) {
            const parsed = parseUrl(url);
            if (!parsed) return null;
            parsed.search = butil.url.buildQuery(pairs);
            return parsed.href;
        },
        appendQuery(url: string, pairs: any[]) {
            const parsed = parseUrl(url);
            if (!parsed) return null;
            for (const pair of pairs ?? []) parsed.searchParams.append(pair.key, pair.value ?? '');
            return parsed.href;
        },
        removeQuery(url: string, keys: string[]) {
            const parsed = parseUrl(url);
            if (!parsed) return null;
            for (const key of keys ?? []) parsed.searchParams.delete(key);
            return parsed.href;
        },
        sortQuery(url: string) {
            const parsed = parseUrl(url);
            if (!parsed) return null;
            parsed.searchParams.sort();
            return parsed.href;
        },

        isPatternSupported() { return typeof (window as any).URLPattern === 'function'; },
        patternTest(pattern: string, base: string | null, url: string) {
            const compiled = makePattern(pattern, base);
            if (!compiled) return false;
            try { return compiled.test(url); } catch { return false; }
        },
        patternExec(pattern: string, base: string | null, url: string) {
            const compiled = makePattern(pattern, base);
            if (!compiled) return null;
            let result: any;
            try { result = compiled.exec(url); } catch { return null; }
            if (!result) return null;
            return {
                protocol: result.protocol?.input ?? '',
                username: result.username?.input ?? '',
                password: result.password?.input ?? '',
                hostname: result.hostname?.input ?? '',
                port: result.port?.input ?? '',
                pathname: result.pathname?.input ?? '',
                search: result.search?.input ?? '',
                hash: result.hash?.input ?? '',
                groups: flattenGroups(result)
            };
        },
        // Whether the pattern itself compiles - a bad pattern and a URL that simply doesn't match
        // both come back as "no match" otherwise.
        isPatternValid(pattern: string, base: string | null) {
            return makePattern(pattern, base) !== null;
        }
    };
}(BitButil));
