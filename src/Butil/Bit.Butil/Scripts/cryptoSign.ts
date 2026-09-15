var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Signing and verification. These import their key material inline with a single fixed usage
    // and share nothing with cryptoKeys, so they weigh no more than the algorithms they implement.
    butil.cryptoSign = {
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
    };

}(BitButil));
