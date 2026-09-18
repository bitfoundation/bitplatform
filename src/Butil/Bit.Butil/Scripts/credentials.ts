var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.credentials = {
        isSupported() { return !!navigator.credentials; },
        isPasswordSupported() { return 'PasswordCredential' in window; },
        isFederatedSupported() { return 'FederatedCredential' in window; },
        storePassword,
        storeFederated,
        get,
        preventSilentAccess
    };

    async function storePassword(id: string, password: string, name: string | null, iconUrl: string | null) {
        const W = window as any;
        if (!navigator.credentials || typeof W.PasswordCredential !== 'function') return false;

        try {
            // iconURL, not iconUrl: the credential data dictionary spells it the way the platform
            // does, and a misspelled key is dropped in silence rather than refused.
            const credential = new W.PasswordCredential({
                id,
                password,
                name: name ?? undefined,
                iconURL: iconUrl ?? undefined
            });
            await navigator.credentials.store(credential);
            return true;
        } catch {
            // The browser declines rather than explains: not secure, or the user said no.
            return false;
        }
    }

    async function storeFederated(id: string, provider: string, name: string | null, iconUrl: string | null, protocol: string | null) {
        const W = window as any;
        if (!navigator.credentials || typeof W.FederatedCredential !== 'function') return false;

        try {
            const credential = new W.FederatedCredential({
                id,
                provider,
                name: name ?? undefined,
                iconURL: iconUrl ?? undefined,
                protocol: protocol ?? undefined
            });
            await navigator.credentials.store(credential);
            return true;
        } catch {
            return false;
        }
    }

    async function get(password: boolean, providers: string[] | null, protocols: string[] | null, mediation: string) {
        if (!navigator.credentials) return null;

        const options: any = { mediation: mediation || 'optional' };
        if (password) options.password = true;
        if (providers?.length) {
            options.federated = protocols?.length ? { providers, protocols } : { providers };
        }

        // Asking for neither would hand navigator.credentials.get() an options object with only a
        // mediation in it, which matches every credential type rather than none. The C# side refuses
        // such a request before it gets here; this is the guard for a call that did not come through it.
        if (!options.password && !options.federated) return null;

        try {
            const credential: any = await navigator.credentials.get(options);
            if (!credential) return null;

            return {
                type: credential.type ?? '',
                id: credential.id ?? '',
                name: credential.name || null,
                iconUrl: credential.iconURL || null,
                password: credential.password ?? null,
                provider: credential.provider ?? null,
                protocol: credential.protocol ?? null
            };
        } catch {
            return null;
        }
    }

    async function preventSilentAccess() {
        if (!navigator.credentials?.preventSilentAccess) return;
        try { await navigator.credentials.preventSilentAccess(); }
        catch { /* nothing to forget */ }
    }
}(BitButil));
