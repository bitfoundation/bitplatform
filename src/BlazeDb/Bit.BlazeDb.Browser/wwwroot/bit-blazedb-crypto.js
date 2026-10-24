// Bit.BlazeDb encryption-at-rest module.
// .NET's managed AES is unavailable inside the browser sandbox, so AES-GCM is done here with
// WebCrypto (SubtleCrypto). Keys are imported as non-extractable CryptoKey handles, so the raw
// key material cannot be read back out of JS once imported.
export async function importKey(rawKey) {
    return await crypto.subtle.importKey('raw', rawKey, { name: 'AES-GCM' }, false, [
        'encrypt',
        'decrypt',
    ]);
}
export async function encrypt(key, nonce, plaintext, associatedData) {
    const result = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce, additionalData: associatedData, tagLength: 128 }, key, plaintext);
    return new Uint8Array(result);
}
export async function decrypt(key, nonce, ciphertext, associatedData) {
    // Throws OperationError when authentication fails; .NET surfaces that as an exception.
    const result = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: nonce, additionalData: associatedData, tagLength: 128 }, key, ciphertext);
    return new Uint8Array(result);
}
export function copyBytes(source, destination) {
    destination.set(source);
}
