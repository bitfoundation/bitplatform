//+:cnd:noEmit
// Read Client.web/wwwroot/web-interop-app.html comments.

import { App } from './App';

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
        catch (err) {
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
            // A promise can be rejected with null/undefined (or an unserializable value), and a throw here would
            // skip the fetch below and leave the TaskCompletionSource pending, so never let building the message fail.
            let errMsg = String(err);
            if (err != null) {
                try {
                    errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${errMsg}`;
                } catch { }
            }
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
        const localHttpNonce = urlParams.get('localHttpNonce')?.toString() ?? '';

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
            else if (await WebInteropApp.handOver(urlToOpen) === false) {
                // Nobody waits for it: the page that opened this window was closed or reloaded, so it goes on here.
                WebInteropApp.autoClose = false;
                location.href = urlToOpen;
            }
            return;
        }

        // Blazor Hybrid:
        const response = await fetch(`http://localhost:${localHttpPort}/api/ExternalSignInCallback?urlToOpen=${encodeURIComponent(urlToOpen)}&nonce=${encodeURIComponent(localHttpNonce)}`, {
            method: 'POST',
            credentials: 'omit'
        });

        if (!response.ok) {
            throw new Error(`ExternalSignInCallback was refused with status ${response.status}.`);
        }
    }

    /**
     * A window loses its opener when it passes through a page of another origin with a Cross-Origin-Opener-Policy, as
     * the same-origin-allow-popups of the template's own pages (App.razor.cs) and of many sign-in providers' does: the
     * browser moves it to a browsing context group of its own. The page that opened it is still on this origin and
     * waits for the answer (See App.expectExternalCallback), so it goes over a BroadcastChannel instead, and once that
     * page says it has it, this window closes as usual, which a window opened by script may still do.
     */
    private static handOver(urlToOpen: string) {
        return new Promise<boolean>(resolve => {
            if (!('BroadcastChannel' in window)) return resolve(false);

            const channel = new BroadcastChannel(App.externalCallbackChannel);
            const id = crypto.randomUUID();
            const timeout = setTimeout(() => { channel.close(); resolve(false); }, 2000);

            channel.onmessage = e => {
                if (e.data?.answerTo !== id) return;
                clearTimeout(timeout);
                channel.close();
                resolve(true);
            };

            channel.postMessage({ id, payload: urlToOpen });
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