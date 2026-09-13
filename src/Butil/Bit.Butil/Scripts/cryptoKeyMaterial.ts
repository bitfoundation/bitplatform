var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // How a key crosses into WebCrypto: the algorithm dictionary it is imported with, and the
    // usages that import has to ask for. Its own module rather than a corner of cryptoKeys because
    // cryptoDerive needs exactly this much of it - and a lazy-loaded module file inlines its
    // dependencies, so parking it next to key generation would put all of that in the download of
    // an app that only derives a key.
    butil.cryptoKeyMaterial = {
        keyAlgorithmFor,
        usagesFor,
        importKeyMaterial
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
        if (!declared) return supported;

        const narrowed = supported.filter(u => declared.includes(u));

        // A key_ops naming none of the algorithm's usages narrows to nothing, and importKey rejects
        // an empty list outright for a secret or private key - with a SyntaxError about the empty
        // list rather than about the mismatch that produced it. Ask for the algorithm's own usages
        // instead, so the failure is the DataError that names key_ops. (An algorithm with no usages
        // to begin with - the public half of ECDH - genuinely wants the empty list, and still gets it.)
        return narrowed.length > 0 || supported.length === 0 ? narrowed : supported;
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
}(BitButil));
