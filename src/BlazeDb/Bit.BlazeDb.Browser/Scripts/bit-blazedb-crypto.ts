// Bit.BlazeDb encryption-at-rest module.
// .NET's managed AES is unavailable inside the browser sandbox, so AES-GCM is done here with
// WebCrypto (SubtleCrypto). Keys are imported as non-extractable CryptoKey handles, so the raw
// key material cannot be read back out of JS once imported.

export async function importKey(rawKey: Uint8Array): Promise<CryptoKey> {
    return await crypto.subtle.importKey('raw', rawKey as Uint8Array<ArrayBuffer>, { name: 'AES-GCM' }, false, [
        'encrypt',
        'decrypt',
    ]);
}

export async function encrypt(key: CryptoKey, nonce: Uint8Array, plaintext: Uint8Array, associatedData: Uint8Array): Promise<Uint8Array> {
    const result = await crypto.subtle.encrypt(
        { name: 'AES-GCM', iv: nonce as Uint8Array<ArrayBuffer>, additionalData: associatedData as Uint8Array<ArrayBuffer>, tagLength: 128 },
        key,
        plaintext as Uint8Array<ArrayBuffer>,
    );
    return new Uint8Array(result);
}

export async function decrypt(key: CryptoKey, nonce: Uint8Array, ciphertext: Uint8Array, associatedData: Uint8Array): Promise<Uint8Array> {
    // Throws OperationError when authentication fails; .NET surfaces that as an exception.
    const result = await crypto.subtle.decrypt(
        { name: 'AES-GCM', iv: nonce as Uint8Array<ArrayBuffer>, additionalData: associatedData as Uint8Array<ArrayBuffer>, tagLength: 128 },
        key,
        ciphertext as Uint8Array<ArrayBuffer>,
    );
    return new Uint8Array(result);
}

export function copyBytes(source: Uint8Array, destination: DotNetMemoryView): void {
    destination.set(source);
}
