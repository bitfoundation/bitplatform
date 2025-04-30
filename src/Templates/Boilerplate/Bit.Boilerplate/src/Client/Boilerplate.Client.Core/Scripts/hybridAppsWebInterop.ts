// Read Boilerplate.Server.Api/Components/HybridAppWebInteropPage.razor

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
        finally {
            window.close();
        }
    }

    private static async socialSignInCallback() {
        const urlParams = new URLSearchParams(location.search);
        const urlToOpen = urlParams.get('url')!.toString();
        const localHttpPort = Number.parseInt(urlParams.get('localHttpPort')!.toString());
        await fetch(`http://localhost:${localHttpPort}/api/SocialSignInCallback?urlToOpen=${encodeURIComponent(urlToOpen)}`, {
            method: 'POST', // Checkout MauiLocalHttpServer or WindowsLocalHttpServer
            credentials: 'omit'
        });
    }

    private static async getWebAuthnCredential() {
        try {
            const webAuthnCredentialOptions = await (await fetch(`api/GetWebAuthnCredentialOptions`, { credentials: 'omit' })).json();
            const webAuthnCredential = await WebAuthn.getCredential(webAuthnCredentialOptions);
            await fetch(`api/WebAuthnCredential`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(webAuthnCredential),
                credentials: 'omit'
            });
        }
        catch (err: any) {
            const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
            await fetch(`api/WebAuthnCredential?error=${encodeURIComponent(errMsg)}`, { method: 'POST', credentials: 'omit' });
        }
    }

    private static async createWebAuthnCredential() {
        try {
            const webAuthnCredentialOptions = await (await fetch(`api/GetCreateWebAuthnCredentialOptions`, { credentials: 'omit' })).json();
            const webAuthnCredential = await WebAuthn.createCredential(webAuthnCredentialOptions);
            await fetch(`api/CreateWebAuthnCredential`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(webAuthnCredential),
                credentials: 'omit'
            });
        }
        catch (err: any) {
            const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
            await fetch(`api/CreateWebAuthnCredential?error=${encodeURIComponent(errMsg)}`, { method: 'POST', credentials: 'omit' });
        }
    }
}