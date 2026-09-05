var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.crypto = {
        // crypto.subtle is undefined outside a secure context, which is the way this API is
        // usually "missing" - the engine shipped it, the page just isn't on https://.
        // getRandomValues and the randomUUID polyfill below work either way.
        isSupported() { return !!(window.crypto && (window.crypto as any).subtle); },
        randomUUID() {
            // Polyfill for older browsers / non-secure contexts.
            if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
            const bytes = new Uint8Array(16);
            crypto.getRandomValues(bytes);
            bytes[6] = (bytes[6] & 0x0f) | 0x40;
            bytes[8] = (bytes[8] & 0x3f) | 0x80;
            const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
            return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
        },
        getRandomValues(length: number) {
            const buf = new Uint8Array(length);
            crypto.getRandomValues(buf);
            return buf;
        },
        async digest(algorithm: string, data: Uint8Array) {
            const buf = await crypto.subtle.digest(algorithm, butil.utils.arrayToBuffer(data));
            return new Uint8Array(buf);
        },
        async signHmac(algorithm: string, key: Uint8Array, data: Uint8Array) {
            const cryptoKey = await crypto.subtle.importKey(
                'raw',
                butil.utils.arrayToBuffer(key),
                { name: 'HMAC', hash: algorithm },
                false,
                ['sign']);
            const sig = await crypto.subtle.sign({ name: 'HMAC' }, cryptoKey, butil.utils.arrayToBuffer(data));
            return new Uint8Array(sig);
        },
        async verifyHmac(algorithm: string, key: Uint8Array, signature: Uint8Array, data: Uint8Array) {
            const cryptoKey = await crypto.subtle.importKey(
                'raw',
                butil.utils.arrayToBuffer(key),
                { name: 'HMAC', hash: algorithm },
                false,
                ['verify']);
            return await crypto.subtle.verify(
                { name: 'HMAC' },
                cryptoKey,
                butil.utils.arrayToBuffer(signature),
                butil.utils.arrayToBuffer(data));
        },
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
        async signRsaPss(privateKey: Uint8Array, data: Uint8Array, saltLength: number, algorithm: string) {
            const key = await crypto.subtle.importKey(
                'pkcs8',
                butil.utils.arrayToBuffer(privateKey),
                { name: 'RSA-PSS', hash: algorithm },
                false,
                ['sign']);
            const sig = await crypto.subtle.sign({ name: 'RSA-PSS', saltLength }, key, butil.utils.arrayToBuffer(data));
            return new Uint8Array(sig);
        },
        async verifyRsaPss(publicKey: Uint8Array, signature: Uint8Array, data: Uint8Array, saltLength: number, algorithm: string) {
            const key = await crypto.subtle.importKey(
                'spki',
                butil.utils.arrayToBuffer(publicKey),
                { name: 'RSA-PSS', hash: algorithm },
                false,
                ['verify']);
            return await crypto.subtle.verify(
                { name: 'RSA-PSS', saltLength },
                key,
                butil.utils.arrayToBuffer(signature),
                butil.utils.arrayToBuffer(data));
        },
        async signEcdsa(privateKey: Uint8Array, data: Uint8Array, curve: string, algorithm: string) {
            const key = await crypto.subtle.importKey(
                'pkcs8',
                butil.utils.arrayToBuffer(privateKey),
                { name: 'ECDSA', namedCurve: curve },
                false,
                ['sign']);
            const sig = await crypto.subtle.sign({ name: 'ECDSA', hash: algorithm }, key, butil.utils.arrayToBuffer(data));
            return new Uint8Array(sig);
        },
        async verifyEcdsa(publicKey: Uint8Array, signature: Uint8Array, data: Uint8Array, curve: string, algorithm: string) {
            const key = await crypto.subtle.importKey(
                'spki',
                butil.utils.arrayToBuffer(publicKey),
                { name: 'ECDSA', namedCurve: curve },
                false,
                ['verify']);
            return await crypto.subtle.verify(
                { name: 'ECDSA', hash: algorithm },
                key,
                butil.utils.arrayToBuffer(signature),
                butil.utils.arrayToBuffer(data));
        },
        async exportKey(sourceFormat: string, key: Uint8Array, targetFormat: string, algorithm: any) {
            const cryptoKey = await importKeyMaterial(sourceFormat, key, algorithm, true);
            const exported = await crypto.subtle.exportKey(targetFormat as any, cryptoKey);
            return new Uint8Array(exported as ArrayBuffer);
        },
        async exportJwk(sourceFormat: string, key: Uint8Array, algorithm: any) {
            const cryptoKey = await importKeyMaterial(sourceFormat, key, algorithm, true);
            return await crypto.subtle.exportKey('jwk', cryptoKey);
        },
        async importJwk(jwk: any, algorithm: any, targetFormat: string) {
            const cryptoKey = await importKeyMaterial('jwk', jwk, algorithm, true);
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
        async wrapKey(format: string, key: Uint8Array, keyAlgorithm: any, wrappingKey: Uint8Array, wrapAlgorithm: any, wrappingKeyHash: string | null) {
            const toWrap = await importKeyMaterial(format, key, keyAlgorithm, true);
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
                keyAlgorithmFor(unwrappedKeyAlgorithm),
                true,
                usagesFor(unwrappedKeyAlgorithm.name, format, null));
            const exported = await crypto.subtle.exportKey(format as any, key);
            return new Uint8Array(exported as ArrayBuffer);
        },

        encryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "encrypt") },
        decryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "decrypt") },

        encryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "encrypt") },
        decryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "decrypt") },

        encryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "encrypt") },
        decryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "decrypt") },

        encryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "encrypt") },
        decryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "decrypt") },
    };

    // The algorithm dictionary an importKey/deriveKey call takes. Absent members are dropped rather
    // than sent as null: WebCrypto validates the dictionary it is given, and an explicit
    // "namedCurve: null" on an AES key is a TypeError where an absent one is simply not read.
    function keyAlgorithmFor(algorithm: any) {
        const params: any = { name: algorithm.name };
        if (algorithm.hash) params.hash = algorithm.hash;
        if (algorithm.namedCurve) params.namedCurve = algorithm.namedCurve;
        if (typeof algorithm.length === 'number') params.length = algorithm.length;
        return params;
    }

    // Which usages a key has to be imported with. importKey rejects a usage the algorithm does not
    // support, so this cannot simply ask for everything - and a key imported with too few usages
    // fails later, at the operation, with an InvalidAccessError. For the asymmetric algorithms the
    // half of the pair decides: pkcs8 is always the private key, and a JWK is private when it
    // carries a "d" member.
    function usagesFor(name: string, format: string, key: any): KeyUsage[] {
        const isPrivate = format === 'pkcs8' || (format === 'jwk' && !!key && !!key.d);

        switch (name) {
            case 'AES-KW': return ['wrapKey', 'unwrapKey'];
            case 'AES-GCM':
            case 'AES-CBC':
            case 'AES-CTR': return ['encrypt', 'decrypt', 'wrapKey', 'unwrapKey'];
            case 'HMAC': return ['sign', 'verify'];
            case 'RSA-OAEP': return isPrivate ? ['decrypt', 'unwrapKey'] : ['encrypt', 'wrapKey'];
            case 'RSA-PSS':
            case 'ECDSA': return isPrivate ? ['sign'] : ['verify'];
            // A public ECDH key is only ever the "other side" of a derivation, and the spec gives
            // it no usages at all - asking for deriveBits on it is an error.
            case 'ECDH': return isPrivate ? ['deriveBits', 'deriveKey'] : [];
            case 'HKDF':
            case 'PBKDF2': return ['deriveBits', 'deriveKey'];
            default: return [];
        }
    }

    // A JWK may narrow what the key is allowed to do with "key_ops", and importKey rejects the whole
    // key with a DataError when the usages asked for are not a subset of it. So where the JWK
    // declares them, they decide - intersected with what the algorithm supports, since key_ops is
    // the key's own list and may name an operation this algorithm has no usage for.
    function requestedUsages(name: string, format: string, key: any): KeyUsage[] {
        const supported = usagesFor(name, format, key);
        const declared = format === 'jwk' && Array.isArray(key?.key_ops) ? key.key_ops : null;
        return declared ? supported.filter(u => declared.includes(u)) : supported;
    }

    function importKeyMaterial(format: string, key: any, algorithm: any, extractable: boolean) {
        // A JWK crosses as an object; every other format crosses as bytes. Its absent members are
        // dropped rather than passed through as null - JWK members are typed by the specification,
        // and an explicit "n": null makes importKey reject the whole key with a DataError.
        const keyData = format === 'jwk' ? withoutNulls(key) : butil.utils.arrayToBuffer(key);
        return crypto.subtle.importKey(format as any, keyData, keyAlgorithmFor(algorithm), extractable, requestedUsages(algorithm.name, format, key));
    }

    function withoutNulls(source: any) {
        if (!source) return source;

        const result: any = {};
        for (const name of Object.keys(source)) {
            if (source[name] !== null && source[name] !== undefined) result[name] = source[name];
        }
        return result;
    }

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
            keyAlgorithmFor(derivedKeyAlgorithm),
            true,
            usagesFor(derivedKeyAlgorithm.name, 'raw', null));
        const raw = await crypto.subtle.exportKey('raw', derived);
        return new Uint8Array(raw);
    }

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

    async function endecryptRsaOaep(algorithm, key, data, keyHash, func: 'encrypt' | 'decrypt') {
        const cryptoAlgorithm: any = { name: algorithm.name };
        if (algorithm.label) {
            cryptoAlgorithm.label = butil.utils.arrayToBuffer(algorithm.label);
        }

        const keyAlgorithm = { name: "RSA-OAEP", hash: keyHash ?? "SHA-256" };

        // RSA keys cannot be imported as "raw"; encrypt uses the public key (spki),
        // decrypt uses the private key (pkcs8).
        const keyFormat = func === 'encrypt' ? 'spki' : 'pkcs8';
        const keyUsages: KeyUsage[] = [func];

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func, keyFormat, keyUsages);
    }

    async function endecryptAesCtr(algorithm, key, data, func: 'encrypt' | 'decrypt') {
        const cryptoAlgorithm = {
            name: algorithm.name,
            counter: butil.utils.arrayToBuffer(algorithm.counter),
            length: algorithm.length
        };

        const keyAlgorithm = { name: "AES-CTR" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func, 'raw', [func]);
    }

    async function endecryptAesCbc(algorithm, key, data, func: 'encrypt' | 'decrypt') {
        const cryptoAlgorithm = {
            name: algorithm.name,
            iv: butil.utils.arrayToBuffer(algorithm.iv),
        };

        const keyAlgorithm = { name: "AES-CBC" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func, 'raw', [func]);
    }

    async function endecryptAesGcm(algorithm, key, data, func: 'encrypt' | 'decrypt') {
        const cryptoAlgorithm: any = {
            name: algorithm.name,
            iv: butil.utils.arrayToBuffer(algorithm.iv),
        };

        // additionalData is optional in the spec; only forward when actually supplied.
        if (algorithm.additionalData) {
            cryptoAlgorithm.additionalData = butil.utils.arrayToBuffer(algorithm.additionalData);
        }
        if (typeof algorithm.tagLength === 'number') {
            cryptoAlgorithm.tagLength = algorithm.tagLength;
        }

        const keyAlgorithm = { name: "AES-GCM" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func, 'raw', [func]);
    }

    async function endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func: 'encrypt' | 'decrypt',
        keyFormat: 'raw' | 'pkcs8' | 'spki' = 'raw', keyUsages: KeyUsage[] = ['encrypt', 'decrypt']) {
        const cryptoKey = await crypto.subtle.importKey(keyFormat, butil.utils.arrayToBuffer(key), keyAlgorithm, false, keyUsages);

        const resultBuffer = await window.crypto.subtle[func](cryptoAlgorithm, cryptoKey, butil.utils.arrayToBuffer(data));

        return new Uint8Array(resultBuffer);
    }
}(BitButil));