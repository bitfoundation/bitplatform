class WebAuthn {
    private static STORE_KEY = 'configured-webauthn';

    public static isAvailable() {
        return !!window.PublicKeyCredential;
    }

    public static storeConfigured(username: string) {
        const storedCredentials = JSON.parse(localStorage.getItem(WebAuthn.STORE_KEY) || '[]') as string[];
        storedCredentials.push(username);
        localStorage.setItem(WebAuthn.STORE_KEY, JSON.stringify(storedCredentials));
    }

    public static isConfigured(username: string | undefined) {
        const storedCredentials = JSON.parse(localStorage.getItem(WebAuthn.STORE_KEY) || '[]') as string[];
        return !!username ? storedCredentials.includes(username) : storedCredentials.length > 0;
    }

    public static removeConfigured(username: string) {
        const storedCredentials = JSON.parse(localStorage.getItem(WebAuthn.STORE_KEY) || '[]') as string[];
        localStorage.setItem(WebAuthn.STORE_KEY, JSON.stringify(storedCredentials.filter(c => c !== username)));
    }


    public static async createCredential(options: PublicKeyCredentialCreationOptions) {
        if (typeof options.challenge === 'string') {
            options.challenge = WebAuthn.fromBase64Url(options.challenge);
        }

        if (typeof options.user.id === 'string') {
            options.user.id = WebAuthn.fromBase64Url(options.user.id);
        }

        if (options.rp.id === null) {
            options.rp.id = undefined;
        }

        for (let cred of options.excludeCredentials || []) {
            if (typeof cred.id !== 'string') continue;

            cred.id = WebAuthn.fromBase64Url(cred.id);
        }

        const credential = await navigator.credentials.create({ publicKey: options }) as PublicKeyCredential;
        const response = credential.response as AuthenticatorAttestationResponse;

        return {
            id: WebAuthn.base64StringToUrl(credential.id),
            rawId: WebAuthn.toBase64Url(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                attestationObject: WebAuthn.toBase64Url(response.attestationObject),
                clientDataJSON: WebAuthn.toBase64Url(response.clientDataJSON),
                transports: response.getTransports ? response.getTransports() : []
            }
        };
    }

    public static async verifyCredential(options: PublicKeyCredentialRequestOptions) {
        if (typeof options.challenge === 'string') {
            options.challenge = WebAuthn.fromBase64Url(options.challenge);
        }

        if (options.allowCredentials) {
            for (let i = 0; i < options.allowCredentials.length; i++) {
                const id = options.allowCredentials[i].id;
                if (typeof id === 'string') {
                    options.allowCredentials[i].id = WebAuthn.fromBase64Url(id);
                }
            }
        }
        const credential = await navigator.credentials.get({ publicKey: options }) as PublicKeyCredential;
        const response = credential.response as AuthenticatorAssertionResponse;

        return {
            id: credential.id,
            rawId: WebAuthn.toBase64Url(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                authenticatorData: WebAuthn.toBase64Url(response.authenticatorData),
                clientDataJSON: WebAuthn.toBase64Url(response.clientDataJSON),
                userHandle: response.userHandle && response.userHandle.byteLength > 0 ? WebAuthn.toBase64Url(response.userHandle) : undefined,
                signature: WebAuthn.toBase64Url(response.signature)
            }
        }
    }



    private static toBase64Url(arrayBuffer: ArrayBuffer): string {
        return btoa(String.fromCharCode(...new Uint8Array(arrayBuffer))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }

    private static fromBase64Url(value: string): Uint8Array {
        return Uint8Array.from(atob(value.replace(/-/g, "+").replace(/_/g, "/")), c => c.charCodeAt(0));
    }

    private static base64StringToUrl(base64String: string): string {
        return base64String.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }
}

(window as any).WebAuthn = WebAuthn;
