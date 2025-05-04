var BitButil = BitButil || {};

(function (butil: any) {
    butil.webAuthn = {
        isAvailable() { return !!window.PublicKeyCredential; },
        createCredential,
        getCredential,
        verify
    };

    async function createCredential(options: PublicKeyCredentialCreationOptions) {
        options.challenge = toArrayBuffer(options.challenge, 'challenge');

        options.user.id = toArrayBuffer(options.user.id, 'user.id');

        options.excludeCredentials?.forEach(function (cred) {
            cred.id = toArrayBuffer(cred.id, 'cred.id');
        });

        if (options.authenticatorSelection?.authenticatorAttachment === null) {
            options.authenticatorSelection.authenticatorAttachment = undefined;
        }

        if (options.rp.id === null) {
            options.rp.id = undefined;
        }

        const credential = await navigator.credentials.create({ publicKey: options }) as PublicKeyCredential;

        const response = credential.response as AuthenticatorAttestationResponse;

        // Move data into Arrays incase it is super long
        const attestationObject = new Uint8Array(response.attestationObject);
        const clientDataJSON = new Uint8Array(response.clientDataJSON);
        const rawId = new Uint8Array(credential.rawId);

        const result = {
            id: credential.id,
            rawId: toBase64Url(rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                attestationObject: toBase64Url(attestationObject),
                clientDataJSON: toBase64Url(clientDataJSON),
                transports: response.getTransports ? response.getTransports() : []
            }
        };

        return result;
    }

    async function getCredential(options: PublicKeyCredentialRequestOptions) {
        options.challenge = toArrayBuffer(options.challenge, 'challenge');

        options.allowCredentials?.forEach(function (cred) {
            cred.id = toArrayBuffer(cred.id, 'cred.id');
        });

        const credential = await navigator.credentials.get({ publicKey: options }) as PublicKeyCredential;

        const response = credential.response as AuthenticatorAssertionResponse;

        // Move data into Arrays incase it is super long
        let authenticatorData = new Uint8Array(response.authenticatorData);
        let clientDataJSON = new Uint8Array(response.clientDataJSON);
        let rawId = new Uint8Array(credential.rawId);
        let signature = new Uint8Array(response.signature);
        let userHandle = new Uint8Array(response.userHandle || []);

        var result = {
            id: credential.id,
            rawId: toBase64Url(rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {
                authenticatorData: toBase64Url(authenticatorData),
                clientDataJSON: toBase64Url(clientDataJSON),
                userHandle: userHandle ? (toBase64Url(userHandle) || null) : null,
                signature: toBase64Url(signature)
            }
        }
        return result;
    }


    const STORAGE_KEY = "Butil.WebAuthn.Verify";
    async function verify(forceCreate: boolean) {
        try {
            const isRegistered = localStorage.getItem(STORAGE_KEY);
            if (!isRegistered || forceCreate) {
                const options = {
                    challenge: "Butil Verify Challenge",
                    rp: { name: "Butil Verify" },
                    user: { id: "ButilVerifyUserId", name: "ButilVerifyUser", displayName: "ButilVerifyUser" },
                    pubKeyCredParams: [{ alg: -7, type: "public-key" }]
                };
                await createCredential(options as any);
                localStorage.setItem(STORAGE_KEY, "ButilVerified");
            }
            else {
                await getCredential({ challenge: "Butil Verify Challenge" } as any);
            }

            return true;
        }
        catch (err) {
            return false;
        }
    }


    function toBase64Url(value: any): string {
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

    function toArrayBuffer(value: any, name: string) {
        if (typeof value === "string") {
            // base64url to base64
            value = value.replace(/-/g, "+").replace(/_/g, "/");

            // base64 to Uint8Array
            var str = value;
            try { str = window.atob(value); } catch { } // we can ignore the error here.
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
}(BitButil));