var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Key material: generation, export/import (including JWK) and wrapping. Derivation is
    // cryptoDerive; both share only what cryptoKeyMaterial holds.
    butil.cryptoKeys = {
        async generateAesKey(bits: number) {
            const key = await crypto.subtle.generateKey({ name: 'AES-GCM', length: bits }, true, ['encrypt', 'decrypt']) as unknown as CryptoKey;
            const raw = await crypto.subtle.exportKey('raw', key);
            return new Uint8Array(raw);
        },
        async generateHmacKey(algorithm: string, lengthBits: number | null) {
            const params: any = { name: 'HMAC', hash: algorithm };
            if (lengthBits) params.length = lengthBits;
            const key = await crypto.subtle.generateKey(params, true, ['sign', 'verify']) as unknown as CryptoKey;
            const raw = await crypto.subtle.exportKey('raw', key);
            return new Uint8Array(raw);
        },
        async generateRsaKeyPair(modulusLengthBits: number, algorithm: string) {
            const pair = await crypto.subtle.generateKey(
                {
                    name: 'RSA-OAEP',
                    modulusLength: modulusLengthBits,
                    publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
                    hash: algorithm
                },
                true,
                ['encrypt', 'decrypt']) as CryptoKeyPair;
            const spki = await crypto.subtle.exportKey('spki', pair.publicKey);
            const pkcs8 = await crypto.subtle.exportKey('pkcs8', pair.privateKey);
            return { publicKey: new Uint8Array(spki), privateKey: new Uint8Array(pkcs8) };
        },
        async generateEcdsaKeyPair(curve: string) {
            const pair = await crypto.subtle.generateKey(
                { name: 'ECDSA', namedCurve: curve },
                true,
                ['sign', 'verify']) as CryptoKeyPair;
            const spki = await crypto.subtle.exportKey('spki', pair.publicKey);
            const pkcs8 = await crypto.subtle.exportKey('pkcs8', pair.privateKey);
            return { publicKey: new Uint8Array(spki), privateKey: new Uint8Array(pkcs8), curve };
        },
        async exportKey(sourceFormat: string, key: Uint8Array, targetFormat: string, algorithm: any) {
            const cryptoKey = await butil.cryptoKeyMaterial.importKeyMaterial(sourceFormat, key, algorithm, true);
            const exported = await crypto.subtle.exportKey(targetFormat as any, cryptoKey);
            return new Uint8Array(exported as ArrayBuffer);
        },
        async exportJwk(sourceFormat: string, key: Uint8Array, algorithm: any) {
            const cryptoKey = await butil.cryptoKeyMaterial.importKeyMaterial(sourceFormat, key, algorithm, true);
            return await crypto.subtle.exportKey('jwk', cryptoKey);
        },
        async importJwk(jwk: any, algorithm: any, targetFormat: string) {
            const cryptoKey = await butil.cryptoKeyMaterial.importKeyMaterial('jwk', jwk, algorithm, true);
            const exported = await crypto.subtle.exportKey(targetFormat as any, cryptoKey);
            return new Uint8Array(exported as ArrayBuffer);
        },
        async generateAesKwKey(bits: number) {
            const key = await crypto.subtle.generateKey({ name: 'AES-KW', length: bits }, true, ['wrapKey', 'unwrapKey']) as unknown as CryptoKey;
            const raw = await crypto.subtle.exportKey('raw', key);
            return new Uint8Array(raw);
        },
        async generateEcdhKeyPair(curve: string) {
            const pair = await crypto.subtle.generateKey(
                { name: 'ECDH', namedCurve: curve },
                true,
                ['deriveBits', 'deriveKey']) as CryptoKeyPair;
            const spki = await crypto.subtle.exportKey('spki', pair.publicKey);
            const pkcs8 = await crypto.subtle.exportKey('pkcs8', pair.privateKey);
            return { publicKey: new Uint8Array(spki), privateKey: new Uint8Array(pkcs8), curve };
        },
        async wrapKey(format: string, key: Uint8Array, keyAlgorithm: any, wrappingKey: Uint8Array, wrapAlgorithm: any, wrappingKeyHash: string | null) {
            const toWrap = await butil.cryptoKeyMaterial.importKeyMaterial(format, key, keyAlgorithm, true);
            const wrapper = await importWrappingKey(wrappingKey, wrapAlgorithm, wrappingKeyHash, 'wrapKey');
            const wrapped = await crypto.subtle.wrapKey(format as any, toWrap, wrapper, wrapParams(wrapAlgorithm));
            return new Uint8Array(wrapped as ArrayBuffer);
        },
        async unwrapKey(format: string, wrappedKey: Uint8Array, unwrappedKeyAlgorithm: any, unwrappingKey: Uint8Array, unwrapAlgorithm: any, unwrappingKeyHash: string | null) {
            const unwrapper = await importWrappingKey(unwrappingKey, unwrapAlgorithm, unwrappingKeyHash, 'unwrapKey');
            const key = await crypto.subtle.unwrapKey(
                format as any,
                butil.utils.arrayToBuffer(wrappedKey),
                unwrapper,
                wrapParams(unwrapAlgorithm),
                butil.cryptoKeyMaterial.keyAlgorithmFor(unwrappedKeyAlgorithm),
                true,
                butil.cryptoKeyMaterial.usagesFor(unwrappedKeyAlgorithm.name, format, null));
            const exported = await crypto.subtle.exportKey(format as any, key);
            return new Uint8Array(exported as ArrayBuffer);
        },
    };

    // The wrapping key itself. AES-KW/GCM/CBC/CTR wrap with raw bytes; RSA-OAEP wraps with the
    // public key (spki) and unwraps with the private one (pkcs8), the same asymmetry encryption has.
    function importWrappingKey(keyBytes: Uint8Array, wrapAlgorithm: any, hash: string | null, usage: KeyUsage) {
        const name = wrapAlgorithm.name;
        if (name === 'RSA-OAEP') {
            const format = usage === 'wrapKey' ? 'spki' : 'pkcs8';
            return crypto.subtle.importKey(format, butil.utils.arrayToBuffer(keyBytes), { name, hash: hash ?? 'SHA-256' }, false, [usage]);
        }
        return crypto.subtle.importKey('raw', butil.utils.arrayToBuffer(keyBytes), { name }, false, [usage]);
    }

    // The wrap/unwrap algorithm dictionary, built from whichever ICryptoAlgorithmParams came over.
    function wrapParams(algorithm: any) {
        const params: any = { name: algorithm.name };
        if (algorithm.iv) params.iv = butil.utils.arrayToBuffer(algorithm.iv);
        if (algorithm.counter) {
            params.counter = butil.utils.arrayToBuffer(algorithm.counter);
            params.length = algorithm.length;
        }
        if (algorithm.additionalData) params.additionalData = butil.utils.arrayToBuffer(algorithm.additionalData);
        if (typeof algorithm.tagLength === 'number') params.tagLength = algorithm.tagLength;
        if (algorithm.label) params.label = butil.utils.arrayToBuffer(algorithm.label);
        return params;
    }
}(BitButil));
