var BitButil = BitButil || {};

(function (butil: any) {
    crypto.subtle.generateKey
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
        var cryptoAlgorithm = {
            name: algorithm.name,
            label: algorithm.label?.buffer
        }

        var keyAlgorithm = { name: "RSA-OAEP", hash: keyHash ?? "SHA-256" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesCtr(algorithm, key, data, func) {
        var cryptoAlgorithm = {
            name: algorithm.name,
            counter: algorithm.counter?.buffer,
            length: algorithm.length
        }

        var keyAlgorithm = { name: "AES-CTR" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesCbc(algorithm, key, data, func) {
        var cryptoAlgorithm = {
            name: algorithm.name,
            iv: algorithm.iv?.buffer,
        }

        var keyAlgorithm = { name: "AES-CBC" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecryptAesGcm(algorithm, key, data, func) {
        var cryptoAlgorithm = {
            name: algorithm.name,
            iv: algorithm.iv?.buffer,
            additionalData: algorithm.additionalData?.buffer,
            tagLength: algorithm.tagLength,
        }

        var keyAlgorithm = { name: "AES-GCM" };

        return await endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func);
    }

    async function endecrypt(cryptoAlgorithm, key, data, keyAlgorithm, func) {
        var cryptoKey = await crypto.subtle.importKey("raw", key.buffer, keyAlgorithm, false, ["encrypt", "decrypt"]);

        var buffer = await window.crypto.subtle[func](cryptoAlgorithm, cryptoKey, data.buffer);

        return new Uint8Array(buffer);
    }


}(BitButil));