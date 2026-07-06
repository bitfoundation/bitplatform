var BitButil = BitButil || {};

(function (butil: any) {
    butil.crypto = {
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
        encryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "encrypt") },
        decryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "decrypt") },

        encryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "encrypt") },
        decryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "decrypt") },

        encryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "encrypt") },
        decryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "decrypt") },

        encryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "encrypt") },
        decryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "decrypt") },
    };

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