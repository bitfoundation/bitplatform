// Original C# implementation of the RC4 stream cipher, used by the standard PDF
// security handler (encryption revisions 2–4). RC4 is a symmetric algorithm, so
// the same routine encrypts and decrypts.

namespace Bit.BlazorUI;

/// <summary>The RC4 stream cipher (ARCFOUR).</summary>
internal static class BitPdfRc4
{
    public static byte[] Transform(byte[] key, byte[] data)
    {
        var s = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        int j = 0;
        int keyLen = key.Length == 0 ? 1 : key.Length;
        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + (key.Length == 0 ? 0 : key[i % keyLen])) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var output = new byte[data.Length];
        int a = 0, b = 0;
        for (int k = 0; k < data.Length; k++)
        {
            a = (a + 1) & 0xFF;
            b = (b + s[a]) & 0xFF;
            (s[a], s[b]) = (s[b], s[a]);
            byte ks = s[(s[a] + s[b]) & 0xFF];
            output[k] = (byte)(data[k] ^ ks);
        }
        return output;
    }
}
