// Read Components/HybridAppWebInterop.razor comments.

declare class BitButil {
    static webAuthn: {
        getCredential: (options: unknown) => Promise<unknown>,
        createCredential: (options: unknown) => Promise<unknown>
    }
};

class HybridAppWebInterop {
    public static async run() {
        try {
            const urlParams = new URLSearchParams(location.search);
            const action = urlParams.get('actionName');
            switch (action) {
                case 'SocialSignInCallback':
                    await HybridAppWebInterop.socialSignInCallback();
                    break;
                case 'GetWebAuthnCredential':
                    await HybridAppWebInterop.getWebAuthnCredential();
                    break;
                case 'CreateWebAuthnCredential':
                    await HybridAppWebInterop.createWebAuthnCredential();
                    break;
            }
        }
        catch (err: any) {
            const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
            await fetch('api/LogError', {
                method: 'POST',
                credentials: 'omit',
                body: errMsg
            });
        }
        finally {
            window.close();
            location.href = 'about:blank';
        }
    }

    private static async socialSignInCallback() {
        const urlParams = new URLSearchParams(location.search);
        const urlToOpen = urlParams.get('url')!.toString();
        const localHttpPort = Number.parseInt(urlParams.get('localHttpPort')!.toString());
        await fetch(`http://localhost:${localHttpPort}/api/SocialSignInCallback?urlToOpen=${encodeURIComponent(urlToOpen)}`, {
            method: 'POST',
            credentials: 'omit'
        });
    }

    private static async getWebAuthnCredential() {
        const webAuthnCredentialOptions = await (await fetch(`api/GetWebAuthnCredentialOptions`, { credentials: 'omit' })).json();
        const webAuthnCredential = await BitButil.webAuthn.getCredential(webAuthnCredentialOptions);
        await fetch(`api/WebAuthnCredential`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(webAuthnCredential),
            credentials: 'omit'
        });

    }

    private static async createWebAuthnCredential() {
        const webAuthnCredentialOptions = await (await fetch(`api/GetCreateWebAuthnCredentialOptions`, { credentials: 'omit' })).json();
        const webAuthnCredential = await BitButil.webAuthn.createCredential(webAuthnCredentialOptions);
        await fetch(`api/CreateWebAuthnCredential`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(webAuthnCredential),
            credentials: 'omit'
        });
    }
}