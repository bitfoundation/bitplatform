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
        console.log(options)
        if (typeof options.challenge === 'string') {
            options.challenge = WebAuthn.stringToBinary(options.challenge);
        }

        if (typeof options.user.id === 'string') {
            options.user.id = WebAuthn.stringToBinary(options.user.id);
        }

        if (options.rp.id === null) {
            options.rp.id = undefined;
        }

        for (let cred of options.excludeCredentials || []) {
            if (typeof cred.id !== 'string') continue;

            cred.id = WebAuthn.stringToBinary(cred.id);
        }

        const credential = await navigator.credentials.create({ publicKey: options }) as PublicKeyCredential;
        const response = credential.response as AuthenticatorAttestationResponse;

        return {
            id: WebAuthn.base64ToString(credential.id),
            rawId: WebAuthn.binaryToString(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                attestationObject: WebAuthn.binaryToString(response.attestationObject),
                clientDataJSON: WebAuthn.binaryToString(response.clientDataJSON),
                transports: response.getTransports ? response.getTransports() : []
            }
        };
    }

    public static async verifyCredential(options: PublicKeyCredentialRequestOptions) {
        if (typeof options.challenge === 'string') {
            options.challenge = WebAuthn.stringToBinary(options.challenge);
        }

        if (options.allowCredentials) {
            for (let i = 0; i < options.allowCredentials.length; i++) {
                const id = options.allowCredentials[i].id;
                if (typeof id === 'string') {
                    options.allowCredentials[i].id = WebAuthn.stringToBinary(id);
                }
            }
        }
        const credential = await navigator.credentials.get({ publicKey: options }) as PublicKeyCredential;
        const response = credential.response as AuthenticatorAssertionResponse;

        return {
            id: credential.id,
            rawId: WebAuthn.binaryToString(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                authenticatorData: WebAuthn.binaryToString(response.authenticatorData),
                clientDataJSON: WebAuthn.binaryToString(response.clientDataJSON),
                userHandle: response.userHandle && response.userHandle.byteLength > 0 ? WebAuthn.binaryToString(response.userHandle) : undefined,
                signature: WebAuthn.binaryToString(response.signature)
            }
        }
    }



    private static binaryToString(value: ArrayBuffer): string {
        return btoa(String.fromCharCode(...new Uint8Array(value))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }

    private static stringToBinary(value: string): Uint8Array {
        return Uint8Array.from(atob(value.replace(/-/g, "+").replace(/_/g, "/")), c => c.charCodeAt(0));
    }

    private static base64ToString(value: string): string {
        return value.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }
}

(window as any).WebAuthn = WebAuthn;
