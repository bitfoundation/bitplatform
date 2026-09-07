var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.fedCm = {
        isSupported() { return 'IdentityCredential' in window; },
        isLoginStatusSupported() { return !!(navigator as any).login; },
        get,
        disconnect,
        setLoginStatus
    };

    // configURL and params are the platform's spellings; the C# side uses ConfigUrl and Parameters
    // to read as C#. A key the dictionary does not know is ignored without a word, so the mapping
    // is written out rather than spread.
    function toProvider(provider: any) {
        const out: any = { configURL: provider.configUrl, clientId: provider.clientId };
        if (provider.nonce) out.nonce = provider.nonce;
        if (provider.loginHint) out.loginHint = provider.loginHint;
        if (provider.domainHint) out.domainHint = provider.domainHint;
        if (provider.fields) out.fields = provider.fields;
        if (provider.parameters) out.params = provider.parameters;
        return out;
    }

    async function get(providers: any[], context: string | null, mediation: string) {
        if (!('IdentityCredential' in window) || !navigator.credentials || !providers?.length) return null;

        const identity: any = { providers: providers.map(toProvider) };
        if (context) identity.context = context;

        try {
            const credential: any = await navigator.credentials.get({ identity, mediation: mediation || 'optional' } as any);
            if (!credential) return null;

            // configURL is left null when the browser does not report it: guessing it from the request
            // names the wrong provider as soon as more than one was offered.
            return {
                id: credential.id ?? null,
                token: credential.token ?? '',
                isAutoSelected: !!credential.isAutoSelected,
                configUrl: credential.configURL ?? null
            };
        } catch {
            // The user dismissed it, the provider returned no account, or the browser is in its
            // cooldown after a dismissal. None of them is something the page can act on.
            return null;
        }
    }

    async function disconnect(configUrl: string, clientId: string, accountHint: string) {
        const IdentityCredential = (window as any).IdentityCredential;
        if (typeof IdentityCredential?.disconnect !== 'function') return false;

        try {
            await IdentityCredential.disconnect({ configURL: configUrl, clientId, accountHint });
            return true;
        } catch {
            return false;
        }
    }

    async function setLoginStatus(status: string) {
        const login = (navigator as any).login;
        if (typeof login?.setStatus !== 'function') return false;

        try { await login.setStatus(status); return true; }
        catch { return false; }
    }
}(BitButil));
