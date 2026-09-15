var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Symmetric and RSA-OAEP encryption. Its key handling is deliberately its own - the import a
    // cipher needs is a single call with a fixed usage, so depending on cryptoKeys for it would
    // pull that whole module in for two lines.
    butil.cryptoCipher = {
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
