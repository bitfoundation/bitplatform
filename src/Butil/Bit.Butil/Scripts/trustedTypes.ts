var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _policies: { [name: string]: any } = {};
    const _listeners: { [id: string]: (event: Event) => void } = {};

    function api() { return (window as any).trustedTypes; }

    // A prefix match on the raw string is not a scope check: 'https://cdn.example.com' also matches
    // 'https://cdn.example.com.evil.test/x', and '/assets/' matches '/assets/../../evil.js'. So both
    // sides are parsed, compared by origin, and the path is matched on a segment boundary - which is
    // also what makes the escapes and dot segments in the raw string irrelevant.
    function withinScope(value: string, prefixes: string[]): string | null {
        let url: URL;
        try { url = new URL(value, document.baseURI); } catch { return null; }
        // Only the two schemes that fetch a script over the network. javascript:, data: and blob:
        // have no origin to compare, so nothing could authorize them.
        if (url.protocol !== 'https:' && url.protocol !== 'http:') return null;

        for (const prefix of prefixes) {
            let scope: URL;
            try { scope = new URL(prefix, document.baseURI); } catch { continue; }
            if (scope.protocol !== url.protocol || scope.origin !== url.origin) continue;

            const path = scope.pathname;
            // A prefix ending in '/' is a directory; anything else is a file, or a directory the
            // caller wrote without its slash - both of which only match on a segment boundary.
            if (path.endsWith('/')
                ? url.pathname.startsWith(path)
                : url.pathname === path || url.pathname.startsWith(path + '/')) return url.href;
        }
        return null;
    }

    butil.trustedTypes = {
        isSupported() { return !!api()?.createPolicy; },

        // Whether a CSP is actually enforcing trusted types on this document. There is no property
        // that says so, so it is asked the only way it can be: assign a plain string to a sink on a
        // detached element - which is a no-op when nothing is enforced, and a TypeError when it is.
        isEnforced() {
            if (!api()) return false;
            try {
                document.createElement('div').innerHTML = '<i></i>';
                return false;
            } catch {
                return true;
            }
        },

        // The policy's rules are declared rather than passed as callbacks: a trusted-types transform
        // has to run synchronously, and a .NET callback cannot - every interop call back into .NET is
        // asynchronous. The rules below are what a hand-written policy usually does anyway.
        createPolicy(name: string, options: any, sanitizerId: string | null) {
            const types = api();
            if (!types?.createPolicy) return false;

            const rules = options ?? {};
            try {
                _policies[name] = types.createPolicy(name, {
                    createHTML: (value: string) => {
                        if (rules.sanitizeHtml === false) return value;
                        const sanitized = butil.sanitizer.sanitize(value, sanitizerId ?? null);
                        // A runtime with no sanitizing sink can't honour the promise this policy
                        // makes, and quietly handing back the input would be the one outcome a
                        // trusted-types policy exists to prevent.
                        if (sanitized === null) throw new Error('BitButil: this policy sanitizes HTML, but Element.setHTML is not supported by this browser.');
                        return sanitized;
                    },
                    createScriptURL: (value: string) => {
                        const allowed: string[] = rules.allowedScriptUrlPrefixes ?? [];
                        // The normalized URL is what comes back, so what the sink loads is what was
                        // checked - not a string that only looked like it.
                        const scoped = withinScope(value, allowed);
                        if (scoped) return scoped;
                        throw new Error(`BitButil: policy '${name}' does not allow the script URL '${value}'.`);
                    },
                    createScript: (value: string) => {
                        if (rules.allowScript === true) return value;
                        throw new Error(`BitButil: policy '${name}' does not create scripts.`);
                    }
                });
                return true;
            } catch {
                // The CSP's trusted-types directive doesn't list this name, or the name is already
                // taken - both are configuration facts the caller can act on, not exceptions.
                return false;
            }
        },
        hasPolicy(name: string) { return !!_policies[name]; },
        policyNames() { return Object.keys(_policies); },

        // The trusted object itself cannot cross to .NET - it would arrive as its string, losing
        // exactly the type that makes it trusted. So the value is created and used on this side,
        // and what comes back is only the resulting text (for display or comparison).
        createHtml(name: string, value: string) {
            const policy = _policies[name];
            if (!policy) return null;
            try { return policy.createHTML(value ?? '').toString(); } catch { return null; }
        },
        createScriptUrl(name: string, value: string) {
            const policy = _policies[name];
            if (!policy) return null;
            try { return policy.createScriptURL(value ?? '').toString(); } catch { return null; }
        },

        // Writing through the policy, which is the whole point of having one: under enforcement this
        // succeeds where assigning a string to innerHTML throws.
        setHtml(element: Element, name: string, value: string) {
            const policy = _policies[name];
            if (!policy || !element) return false;
            try {
                element.innerHTML = policy.createHTML(value ?? '');
                return true;
            } catch {
                return false;
            }
        },
        setScriptSrc(element: HTMLScriptElement, name: string, value: string) {
            const policy = _policies[name];
            if (!policy || !element) return false;
            try {
                element.src = policy.createScriptURL(value ?? '');
                return true;
            } catch {
                return false;
            }
        },

        // Violations are how an app finds the sinks it still writes to as plain strings, which is the
        // reason to run the CSP in report-only mode first.
        onViolation(dotNetRef: any, method: string, id: string) {
            const handler = (event: any) => {
                // Every CSP violation lands on this one event; only the trusted-types ones belong here.
                if (typeof event.violatedDirective === 'string'
                    && event.violatedDirective.indexOf('trusted-types') < 0
                    && event.violatedDirective.indexOf('require-trusted-types-for') < 0) return;

                butil.utils.dispatch(dotNetRef, method, id, {
                    directive: event.violatedDirective ?? '',
                    sample: event.sample ?? '',
                    sourceFile: event.sourceFile ?? '',
                    lineNumber: event.lineNumber ?? 0,
                    disposition: event.disposition ?? ''
                });
            };
            _listeners[id] = handler;
            document.addEventListener('securitypolicyviolation', handler);
        },
        offViolation(id: string) {
            const handler = _listeners[id];
            if (!handler) return;
            delete _listeners[id];
            document.removeEventListener('securitypolicyviolation', handler);
        },
        disposeAll() {
            for (const id in _listeners) {
                document.removeEventListener('securitypolicyviolation', _listeners[id]);
                delete _listeners[id];
            }
            // The policies themselves are deliberately kept: the browser refuses to create a policy
            // name twice, so forgetting them here would make a re-created scope unable to get them back.
        }
    };
}(BitButil));
