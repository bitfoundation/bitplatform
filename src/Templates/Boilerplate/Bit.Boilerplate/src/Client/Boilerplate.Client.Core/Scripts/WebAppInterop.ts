// Read Components/WebInteropApp.razor comments.

declare class BitButil {
    static webAuthn: {
        getCredential: (options: unknown) => Promise<unknown>,
        createCredential: (options: unknown) => Promise<unknown>
    }
};

class WebInteropApp {

    private static autoClose = true;

    public static async run() {
        try {
            const urlParams = new URLSearchParams(location.search);
            const action = urlParams.get('actionName');
            switch (action) {
                case 'SocialSignInCallback':
                    await WebInteropApp.socialSignInCallback();
                    break;
                case 'GetWebAuthnCredential':
                    await WebInteropApp.getWebAuthnCredential();
                    break;
                case 'CreateWebAuthnCredential':
                    await WebInteropApp.createWebAuthnCredential();
                    break;
            }
        }
        catch (err: any) {
            const urlParams = new URLSearchParams(location.search);
            const localHttpPort = urlParams.get('localHttpPort')?.toString();
            if (localHttpPort) {
                // Blazor Hybrid:
                const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
                await fetch('api/LogError', {
                    method: 'POST',
                    credentials: 'omit',
                    body: errMsg
                });
            }
        }
        finally {
            if (WebInteropApp.autoClose) {
                window.close();
                location.href = 'about:blank';
            }
        }
    }

    private static async socialSignInCallback() {
        const urlParams = new URLSearchParams(location.search);
        const urlToOpen = urlParams.get('url')!.toString();
        const localHttpPort = urlParams.get('localHttpPort')?.toString();
        if (!localHttpPort) {
            // Blazor WebAssembly, Auto or Server:
            if (window.opener) {
                window.opener.postMessage({ key: 'PUBLISH_MESSAGE', message: 'SOCIAL_SIGN_IN', payload: urlToOpen });
            }
            else {
                WebInteropApp.autoClose = false;
                location.href = urlToOpen;
            }
            return;
        }
        // Blazor Hybrid:
        await fetch(`http://localhost:${localHttpPort}/api/SocialSignInCallback?urlToOpen=${encodeURIComponent(urlToOpen)}`, {
            method: 'POST',
            credentials: 'omit'
        });
    }

    private static async getWebAuthnCredential() {
        // Blazor Hybrid:
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
        // Blazor Hybrid:
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