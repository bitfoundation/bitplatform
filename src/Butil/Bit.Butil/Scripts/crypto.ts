var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The WebCrypto entry points that need nothing else: randomness and hashing. Signing, key
    // material, derivation and the ciphers each live in their own module (cryptoSign, cryptoKeys,
    // cryptoDerive, cryptoCipher), so an app that only hashes a value never downloads the rest.
    butil.crypto = {
        // crypto.subtle is undefined outside a secure context, which is the way this API is
        // usually "missing" - the engine shipped it, the page just isn't on https://.
        // getRandomValues and the randomUUID polyfill in utils work either way.
        isSupported() { return !!(window.crypto && (window.crypto as any).subtle); },
        // Polyfilled for older browsers / non-secure contexts, in utils, because the rest of Butil
        // needs the same ids without depending on this module.
        randomUUID() { return butil.utils.randomUUID(); },
        getRandomValues(length: number) {
            const buf = new Uint8Array(length);
            crypto.getRandomValues(buf);
            return buf;
        },
        async digest(algorithm: string, data: Uint8Array) {
            const buf = await crypto.subtle.digest(algorithm, butil.utils.arrayToBuffer(data));
            return new Uint8Array(buf);
        },
    };

}(BitButil));
