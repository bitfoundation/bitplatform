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
        localStorage.setItem(WebAuthn.STORE_KEY, JSON.stringify(!!username ? storedCredentials.filter(c => c !== username) : []));
    }


    public static async createCredential(options: PublicKeyCredentialCreationOptions) {
        console.log(options);

        //if (typeof options.challenge === 'string') {
        //    options.challenge = WebAuthn.stringToBinary(options.challenge);
        //}

        options.challenge = WebAuthn.ToArrayBuffer(options.challenge, 'challenge');

        //if (typeof options.user.id === 'string') {
        //    options.user.id = WebAuthn.stringToBinary(options.user.id);
        //}

        options.user.id = WebAuthn.ToArrayBuffer(options.user.id, 'user.id');

        for (let cred of options.excludeCredentials || []) {
            //if (typeof cred.id !== 'string') continue;
            //cred.id = WebAuthn.stringToBinary(cred.id);
            cred.id = WebAuthn.ToArrayBuffer(cred.id, 'cred.id');
        }

        if (options.authenticatorSelection?.authenticatorAttachment === null) {
            options.authenticatorSelection.authenticatorAttachment = undefined;
        }

        if (options.rp.id === null) {
            options.rp.id = undefined;
        }

        console.log('corrected:', options);

        const credential = await navigator.credentials.create({ publicKey: options }) as PublicKeyCredential;

        console.log('credential:', credential);

        const response = credential.response as AuthenticatorAttestationResponse;

        // Move data into Arrays incase it is super long
        const attestationObject = new Uint8Array(response.attestationObject);
        const clientDataJSON = new Uint8Array(response.clientDataJSON);
        const rawId = new Uint8Array(credential.rawId);

        const result = {
            id: credential.id,
            rawId: WebAuthn.ToBase64Url(rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                attestationObject: WebAuthn.ToBase64Url(attestationObject),
                clientDataJSON: WebAuthn.ToBase64Url(clientDataJSON),
                transports: response.getTransports ? response.getTransports() : []
            }
        };

        console.log('result:', result);

        return result;
    }

    public static async verifyCredential(options: PublicKeyCredentialRequestOptions) {
        console.log(options);

        //if (typeof options.challenge === 'string') {
        //    options.challenge = WebAuthn.stringToBinary(options.challenge);
        //}

        options.challenge = WebAuthn.ToArrayBuffer(options.challenge, 'challenge');

        //if (options.allowCredentials) {
        //    for (let i = 0; i < options.allowCredentials.length; i++) {
        //        const id = options.allowCredentials[i].id;
        //        if (typeof id === 'string') {
        //            options.allowCredentials[i].id = WebAuthn.stringToBinary(id);
        //        }
        //    }
        //}

        options.allowCredentials?.forEach(function (cred) {
            cred.id = WebAuthn.ToArrayBuffer(cred.id, 'cred.id');
        });

        console.log('corrected:', options);

        const credential = await navigator.credentials.get({ publicKey: options }) as PublicKeyCredential;

        console.log('credential:', credential);

        const response = credential.response as AuthenticatorAssertionResponse;

        // Move data into Arrays incase it is super long
        let authenticatorData = new Uint8Array(response.authenticatorData);
        let clientDataJSON = new Uint8Array(response.clientDataJSON);
        let rawId = new Uint8Array(credential.rawId);
        let signature = new Uint8Array(response.signature);
        let userHandle = new Uint8Array(response.userHandle || []);

        var result = {
            id: credential.id,
            rawId: WebAuthn.ToBase64Url(rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                authenticatorData: WebAuthn.ToBase64Url(authenticatorData),
                clientDataJSON: WebAuthn.ToBase64Url(clientDataJSON),
                userHandle: userHandle !== null ? WebAuthn.ToBase64Url(userHandle) : null,
                signature: WebAuthn.ToBase64Url(signature)
            }
        }

        console.log('result:', result);

        return result;
    }



    //private static stringToBinary(value: string): Uint8Array {
    //    return Uint8Array.from(atob(value.replace(/-/g, "+").replace(/_/g, "/")), c => c.charCodeAt(0));
    //}

    //private static binaryToString(value: ArrayBuffer): string {
    //    return btoa(String.fromCharCode(...new Uint8Array(value))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    //}

    //private static base64ToString(value: string): string {
    //    return value.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    //}

    private static ToArrayBuffer(value: any, name: string) {
        if (typeof value === "string") {
            // base64url to base64
            value = value.replace(/-/g, "+").replace(/_/g, "/");

            // base64 to Uint8Array
            var str = window.atob(value);
            var bytes = new Uint8Array(str.length);
            for (var i = 0; i < str.length; i++) {
                bytes[i] = str.charCodeAt(i);
            }
            value = bytes;
        }

        // Array to Uint8Array
        if (Array.isArray(value)) {
            value = new Uint8Array(value);
        }

        // Uint8Array to ArrayBuffer
        if (value instanceof Uint8Array) {
            value = value.buffer;
        }

        // error if none of the above worked
        if (!(value instanceof ArrayBuffer)) {
            throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
        }

        return value;
    };

    private static ToBase64Url(value: any) {
        // Array or ArrayBuffer to Uint8Array
        if (Array.isArray(value)) {
            value = Uint8Array.from(value);
        }

        if (value instanceof ArrayBuffer) {
            value = new Uint8Array(value);
        }

        // Uint8Array to base64
        if (value instanceof Uint8Array) {
            var str = "";
            var len = value.byteLength;

            for (var i = 0; i < len; i++) {
                str += String.fromCharCode(value[i]);
            }
            value = window.btoa(str);
        }

        if (typeof value !== "string") {
            throw new Error("could not coerce to string");
        }

        // base64 to base64url
        // NOTE: "=" at the end of challenge is optional, strip it off here
        value = value.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

        return value;
    };
}

(window as any).WebAuthn = WebAuthn;
