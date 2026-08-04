//+:cnd:noEmit
// Read Client.web/wwwroot/web-interop-app.html comments.

export class WebInteropApp {
    private static autoClose = true;

    public static async run() {
        try {
            const urlParams = new URLSearchParams(location.search);
            const action = urlParams.get('actionName');

            switch (action) {
                case 'ExternalSignInCallback':
                    await WebInteropApp.externalSignInCallback();
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
            // Report the failure back to the app. This is the ONLY path that faults the TaskCompletionSource the
            // Hybrid app is awaiting, so it must run for the WebAuthn actions too - it used to bail out whenever
            // `localHttpPort` was absent, and the WebAuthn interop URLs never carried it, so cancelling a biometric
            // prompt left the sign-in UI spinning until the app was restarted.
            const urlParams = new URLSearchParams(location.search);
            const action = urlParams.get('actionName');
            const localHttpPort = urlParams.get('localHttpPort')?.toString();

            // The WebAuthn pages are served BY the local server, so a relative URL is correct for them. The
            // external-sign-in page is served from the web-app origin, so it needs the absolute loopback URL.
            const isServedByLocalHttpServer = action === 'GetWebAuthnCredential' || action === 'CreateWebAuthnCredential';
            if (!isServedByLocalHttpServer && !localHttpPort) return; // Blazor WebAssembly, Auto or Server.

            const logErrorUrl = isServedByLocalHttpServer
                ? `api/LogError`
                : `http://localhost:${localHttpPort}/api/LogError`;

            // Blazor Hybrid:
            const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
            await fetch(logErrorUrl, {
                method: 'POST',
                credentials: 'omit',
                body: errMsg
            });

        }
        finally {
            if (!WebInteropApp.autoClose) return;

            window.close();
            location.href = 'about:blank';
        }
    }

    private static async externalSignInCallback() {
        const urlParams = new URLSearchParams(location.search);
        const urlToOpen = urlParams.get('url')!.toString();
        const localHttpPort = urlParams.get('localHttpPort')?.toString();

        try {
            const redirectUrl = new URL(urlToOpen, window.location.origin);
            if (redirectUrl.protocol !== "http:" && redirectUrl.protocol !== "https:") {
                throw new Error("Invalid protocol for redirection.");
            }
            if (redirectUrl.origin !== window.location.origin) {
                throw new Error("Redirect to a different origin is not allowed.");
            }
        } catch (e) {
            console.error("Invalid or malicious redirect URL detected:", e);
            return;
        }

        if (!localHttpPort) {
            // Blazor WebAssembly, Auto or Server:
            if (window.opener) {
                window.opener.postMessage({ key: 'PUBLISH_MESSAGE', message: 'EXTERNAL_SIGN_IN_CALLBACK', payload: urlToOpen });
            }
            else {
                WebInteropApp.autoClose = false;
                location.href = urlToOpen;
            }
            return;
        }

        // Blazor Hybrid:
        await fetch(`http://localhost:${localHttpPort}/api/ExternalSignInCallback?urlToOpen=${encodeURIComponent(urlToOpen)}`, {
            method: 'POST',
            credentials: 'omit'
        });
    }

    /// The per-process token the local HTTP server requires on every request. It is placed in the interop URL by
    /// MauiWebAuthnService / WindowsWebAuthnService, so only the app itself can drive those endpoints.
    private static sessionToken() {
        return new URLSearchParams(location.search).get('token') ?? '';
    }

    private static async getWebAuthnCredential() {
        // Blazor Hybrid:
        const token = encodeURIComponent(WebInteropApp.sessionToken());
        const webAuthnCredentialOptions = await (await fetch(`api/GetWebAuthnCredentialOptions?token=${token}`, { credentials: 'omit' })).json();
        const webAuthnCredential = await BitButil.webAuthn.getCredential(webAuthnCredentialOptions);

        await fetch(`api/WebAuthnCredential?token=${token}`, {
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
        const token = encodeURIComponent(WebInteropApp.sessionToken());
        const webAuthnCredentialOptions = await (await fetch(`api/GetCreateWebAuthnCredentialOptions?token=${token}`, { credentials: 'omit' })).json();
        const webAuthnCredential = await BitButil.webAuthn.createCredential(webAuthnCredentialOptions);

        await fetch(`api/CreateWebAuthnCredential?token=${token}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(webAuthnCredential),
            credentials: 'omit'
        });
    }
}