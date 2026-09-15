var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Key derivation: PBKDF2, HKDF and ECDH, as raw bits or as a derived key exported back to bytes.
    butil.cryptoDerive = {
        async derivePbkdf2(password: Uint8Array, salt: Uint8Array, iterations: number, outputLengthBits: number, algorithm: string) {
            const baseKey = await crypto.subtle.importKey(
                'raw',
                butil.utils.arrayToBuffer(password),
                { name: 'PBKDF2' },
                false,
                ['deriveBits']);
            const bits = await crypto.subtle.deriveBits(
                { name: 'PBKDF2', salt: butil.utils.arrayToBuffer(salt), iterations, hash: algorithm },
                baseKey,
                outputLengthBits);
            return new Uint8Array(bits);
        },
        async deriveEcdhBits(privateKey: Uint8Array, publicKey: Uint8Array, curve: string, outputLengthBits: number) {
            const { priv, pub } = await importEcdhPair(privateKey, publicKey, curve, 'deriveBits');
            const bits = await crypto.subtle.deriveBits({ name: 'ECDH', public: pub }, priv, outputLengthBits);
            return new Uint8Array(bits);
        },
        async deriveEcdhKey(privateKey: Uint8Array, publicKey: Uint8Array, curve: string, derivedKeyAlgorithm: any) {
            const { priv, pub } = await importEcdhPair(privateKey, publicKey, curve, 'deriveKey');
            return await deriveAndExport({ name: 'ECDH', public: pub }, priv, derivedKeyAlgorithm);
        },
        async deriveHkdfBits(keyMaterial: Uint8Array, salt: Uint8Array, info: Uint8Array, outputLengthBits: number, algorithm: string) {
            const baseKey = await importDerivationKey('HKDF', keyMaterial, 'deriveBits');
            const bits = await crypto.subtle.deriveBits(hkdfParams(salt, info, algorithm), baseKey, outputLengthBits);
            return new Uint8Array(bits);
        },
        async deriveHkdfKey(keyMaterial: Uint8Array, salt: Uint8Array, info: Uint8Array, algorithm: string, derivedKeyAlgorithm: any) {
            const baseKey = await importDerivationKey('HKDF', keyMaterial, 'deriveKey');
            return await deriveAndExport(hkdfParams(salt, info, algorithm), baseKey, derivedKeyAlgorithm);
        },
        async derivePbkdf2Key(password: Uint8Array, salt: Uint8Array, iterations: number, algorithm: string, derivedKeyAlgorithm: any) {
            const baseKey = await importDerivationKey('PBKDF2', password, 'deriveKey');
            const params = { name: 'PBKDF2', salt: butil.utils.arrayToBuffer(salt), iterations, hash: algorithm };
            return await deriveAndExport(params, baseKey, derivedKeyAlgorithm);
        },
    };

    async function importEcdhPair(privateKey: Uint8Array, publicKey: Uint8Array, curve: string, usage: KeyUsage) {
        const params = { name: 'ECDH', namedCurve: curve };
        const priv = await crypto.subtle.importKey('pkcs8', butil.utils.arrayToBuffer(privateKey), params, false, [usage]);
        const pub = await crypto.subtle.importKey('spki', butil.utils.arrayToBuffer(publicKey), params, false, []);
        return { priv, pub };
    }

    function importDerivationKey(name: string, keyMaterial: Uint8Array, usage: KeyUsage) {
        // PBKDF2 and HKDF base keys may never be extractable - the spec requires it, and passing
        // true here is a TypeError rather than a warning.
        return crypto.subtle.importKey('raw', butil.utils.arrayToBuffer(keyMaterial), { name }, false, [usage]);
    }

    // salt and info are required members of the HKDF dictionary even when empty, so a missing one
    // becomes a zero-length buffer instead of undefined.
    function hkdfParams(salt: Uint8Array, info: Uint8Array, algorithm: string) {
        return {
            name: 'HKDF',
            hash: algorithm,
            salt: butil.utils.arrayToBuffer(salt) ?? new ArrayBuffer(0),
            info: butil.utils.arrayToBuffer(info) ?? new ArrayBuffer(0)
        };
    }

    // Derivation on this side of the boundary always ends in raw bytes: .NET holds key material,
    // not CryptoKey handles, so the derived key is exported straight back out.
    async function deriveAndExport(params: any, baseKey: CryptoKey, derivedKeyAlgorithm: any) {
        const derived = await crypto.subtle.deriveKey(
            params,
            baseKey,
            butil.cryptoKeyMaterial.keyAlgorithmFor(derivedKeyAlgorithm),
            true,
            butil.cryptoKeyMaterial.usagesFor(derivedKeyAlgorithm.name, 'raw', null));
        const raw = await crypto.subtle.exportKey('raw', derived);
        return new Uint8Array(raw);
    }
}(BitButil));
