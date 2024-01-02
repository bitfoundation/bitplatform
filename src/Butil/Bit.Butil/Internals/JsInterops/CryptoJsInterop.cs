using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class CryptoJsInterop
{
    internal static ValueTask<byte[]> CryptoEncrypt<T>(this IJSRuntime js, T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesCbc", algorithm, key, data);
        }


        return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesGcm", algorithm, key, data);
    }
    internal static ValueTask<byte[]> CryptoEncrypt(this IJSRuntime js, CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv, CryptoKeyHash? keyHash)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => CryptoEncrypt(js, new AesCtrCryptoAlgorithmParams { Counter = iv }, key, data, null),
            CryptoAlgorithm.AesCbc => CryptoEncrypt(js, new AesCbcCryptoAlgorithmParams { Iv = iv }, key, data, null),
            CryptoAlgorithm.AesGcm => CryptoEncrypt(js, new AesGcmCryptoAlgorithmParams { Iv = iv }, key, data, null),
            _ => CryptoEncrypt(js, new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };

    internal static ValueTask<byte[]> CryptoDecrypt<T>(this IJSRuntime js, T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesCbc", algorithm, key, data);
        }


        return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesGcm", algorithm, key, data);
    }
    internal static ValueTask<byte[]> CryptoDecrypt(this IJSRuntime js, CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv, CryptoKeyHash? keyHash)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => CryptoDecrypt(js, new AesCtrCryptoAlgorithmParams { Counter = iv }, key, data, null),
            CryptoAlgorithm.AesCbc => CryptoDecrypt(js, new AesCbcCryptoAlgorithmParams { Iv = iv }, key, data, null),
            CryptoAlgorithm.AesGcm => CryptoDecrypt(js, new AesGcmCryptoAlgorithmParams { Iv = iv }, key, data, null),
            _ => CryptoDecrypt(js, new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };
}
