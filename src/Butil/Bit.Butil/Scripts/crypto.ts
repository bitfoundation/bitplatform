var BitButil = BitButil || {};

(function (butil: any) {
    butil.crypto = {
        encryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "encrypt") },
        decryptRsaOaep(algorithm, key, data, keyHash) { return endecryptRsaOaep(algorithm, key, data, keyHash, "decrypt") },

        encryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "encrypt") },
        decryptAesCtr(algorithm, key, data) { return endecryptAesCtr(algorithm, key, data, "decrypt") },

        encryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "encrypt") },
        decryptAesCbc(algorithm, key, data) { return endecryptAesCbc(algorithm, key, data, "decrypt") },

        encryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "encrypt") },
        decryptAesGcm(algorithm, key, data) { return endecryptAesGcm(algorithm, key, data, "decrypt") },
    };

    async function endecryptRsaOaep(algorithm, key, data, keyHash, func) {
        const cryptoAlgorithm = {
            name: algorithm.name,
            label: butil.utils.arrayToBuffer(algorithm.label)
        }

        const keyAlgorithm = { name: "RSA-OAEP", hash: keyHash ?? "SHA-256" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesCtr(algorithm, key, data, func) {
        const cryptoAlgorithm = {
            name: algorithm.name,
            counter: butil.utils.arrayToBuffer(algorithm.counter),
            length: algorithm.length
        }

        const keyAlgorithm = { name: "AES-CTR" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesCbc(algorithm, key, data, func) {
        const cryptoAlgorithm = {
            name: algorithm.name,
            iv: butil.utils.arrayToBuffer(algorithm.iv),
        }

        const keyAlgorithm = { name: "AES-CBC" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesGcm(algorithm, key, data, func) {
        const cryptoAlgorithm = {
            name: algorithm.name,
            iv: butil.utils.arrayToBuffer(algorithm.iv),
            additionalData: butil.utils.arrayToBuffer(algorithm.additionalData),
            tagLength: algorithm.tagLength,
        }

        const keyAlgorithm = { name: "AES-GCM" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func) {
        const cryptoKey = await crypto.subtle.importKey("raw", butil.utils.arrayToBuffer(key), keyAlgorithm, false, ["encrypt", "decrypt"]);

        const encryptedBuffer = await window.crypto.subtle[func](cryptoAlgorithm, cryptoKey, butil.utils.arrayToBuffer(data));

        return new Uint8Array(encryptedBuffer);
    }
}(BitButil));