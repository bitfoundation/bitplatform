// C# implementation of the PDF standard security handler, following the
// algorithms in the PDF specification (ISO 32000, §7.6). Supports
// empty-user-password decryption for revisions 2–6 (RC4, AESV2/AES-128,
// AESV3/AES-256).

using System.Security.Cryptography;

// MD5/AES are used only for optional encrypted-PDF support and are available on
// server/desktop hosting. They are not present in the browser (WASM) sandbox,
// where encrypted documents are simply unsupported; suppress the platform
// analyzer rather than fail the build for all consumers.
#pragma warning disable CA1416

namespace Bit.BlazorUI;

/// <summary>
/// Derives the document encryption key from an empty user password and decrypts
/// strings and streams for the standard security handler.
/// </summary>
public sealed class BitPdfStandardSecurityHandler
{
    private static readonly byte[] Padding =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ];

    private readonly byte[] _fileKey;
    private readonly int _revision;
    private readonly BitPdfCipherKind _streamCipher;
    private readonly BitPdfCipherKind _stringCipher;

    /// <summary>The raw user-permission bits from the <c>/P</c> entry (Table 22).</summary>
    public int Permissions { get; }

    private BitPdfStandardSecurityHandler(byte[] fileKey, int revision, BitPdfCipherKind stream, BitPdfCipherKind str, int permissions)
    {
        _fileKey = fileKey;
        _revision = revision;
        _streamCipher = stream;
        _stringCipher = str;
        Permissions = permissions;
    }

    /// <summary>Builds a handler from the <c>/Encrypt</c> dictionary, or <c>null</c> if unsupported.</summary>
    public static BitPdfStandardSecurityHandler? TryCreate(BitPdfDict encrypt, byte[]? id0, string? password = null)
    {
        if ((encrypt.Get("Filter") as BitPdfName)?.Value is not "Standard")
        {
            return null;
        }

        int v = GetInt(encrypt.Get("V"), 0);
        int r = GetInt(encrypt.Get("R"), 0);
        int p = GetInt(encrypt.Get("P"), 0);
        byte[] o = GetStringBytes(encrypt.Get("O"));
        byte[] u = GetStringBytes(encrypt.Get("U"));
        int length = GetInt(encrypt.Get("Length"), 40);
        bool encryptMetadata = encrypt.Get("EncryptMetadata") is not bool em || em;
        byte[] pwd = PasswordBytes(password);

        (BitPdfCipherKind stream, BitPdfCipherKind str) = ResolveCiphers(encrypt, v);

        byte[] fileKey;
        try
        {
            if (r is >= 2 and <= 4)
            {
                fileKey = ComputeAndValidateLegacy(pwd, o, u, p, id0 ?? [], r, length, encryptMetadata, password is not null);
            }
            else if (r is 5 or 6)
            {
                byte[] ue = GetStringBytes(encrypt.Get("UE"));
                byte[] oe = GetStringBytes(encrypt.Get("OE"));
                fileKey = ComputeAndValidateR56(pwd, u, o, ue, oe, r, password is not null);
            }
            else
            {
                return null;
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            // MD5/AES from System.Security.Cryptography are absent in the browser
            // WebAssembly sandbox. Convert to a clear, typed failure.
            throw new BitPdfUnsupportedEncryptionException(
                "Encrypted PDFs are not supported on WebAssembly (MD5/AES are unavailable in the browser sandbox).", ex);
        }

        return new BitPdfStandardSecurityHandler(fileKey, r, stream, str, p);
    }

    // ----- Legacy (R2-4) key derivation with password validation -----

    private static byte[] ComputeAndValidateLegacy(byte[] pwd, byte[] o, byte[] u, int p,
        byte[] id0, int r, int length, bool encryptMetadata, bool passwordProvided)
    {
        // Try the supplied (or empty) password as the user password first.
        byte[] key = ComputeKeyLegacy(pwd, o, p, id0, r, length, encryptMetadata);
        if (ValidateUserLegacy(key, u, id0, r))
        {
            return key;
        }

        // Fall back to treating it as the owner password (Algorithm 7): recover the
        // padded user password, then derive and validate the user key from it.
        byte[]? userPwd = UserPasswordFromOwner(pwd, o, r, length);
        if (userPwd is not null)
        {
            byte[] ownerKey = ComputeKeyLegacy(userPwd, o, p, id0, r, length, encryptMetadata);
            if (ValidateUserLegacy(ownerKey, u, id0, r))
            {
                return ownerKey;
            }
        }

        throw new BitPdfPasswordException(
            passwordProvided ? "The supplied password is incorrect." : "This document is password-protected.",
            passwordProvided);
    }

    private static bool ValidateUserLegacy(byte[] key, byte[] u, byte[] id0, int r)
    {
        // Algorithm 6: derive /U from the key and compare.
        if (u.Length == 0)
        {
            return true; // nothing to check against; accept
        }
        if (r == 2)
        {
            byte[] computed = BitPdfRc4.Transform(key, Padding);
            return FirstBytesEqual(computed, u, 32);
        }
        byte[] hash = Md5(Concat(Padding, id0));
        byte[] enc = BitPdfRc4.Transform(key, hash);
        for (int i = 1; i <= 19; i++)
        {
            enc = BitPdfRc4.Transform(XorKey(key, i), enc);
        }
        return FirstBytesEqual(enc, u, 16);
    }

    private static byte[]? UserPasswordFromOwner(byte[] ownerPwd, byte[] o, int r, int length)
    {
        if (o.Length < 32)
        {
            return null;
        }
        byte[] hash = Md5(Pad(ownerPwd));
        int n = r == 2 ? 5 : Math.Clamp(length / 8, 5, 16);
        if (r >= 3)
        {
            for (int i = 0; i < 50; i++)
            {
                hash = Md5(hash[..n]);
            }
        }
        byte[] rc4Key = hash[..n];
        byte[] userPwd = o[..32];
        if (r == 2)
        {
            return BitPdfRc4.Transform(rc4Key, userPwd);
        }
        for (int i = 19; i >= 0; i--)
        {
            userPwd = BitPdfRc4.Transform(XorKey(rc4Key, i), userPwd);
        }
        return userPwd;
    }

    private static byte[] XorKey(byte[] key, int i)
    {
        var x = new byte[key.Length];
        for (int j = 0; j < key.Length; j++)
        {
            x[j] = (byte)(key[j] ^ i);
        }
        return x;
    }

    // ----- R5/R6 (AES-256) key derivation with password validation -----

    private static byte[] ComputeAndValidateR56(byte[] pwd, byte[] u, byte[] o, byte[] ue, byte[] oe,
        int r, bool passwordProvided)
    {
        if (u.Length < 48)
        {
            throw new BitPdfPasswordException("Malformed encryption dictionary.", passwordProvided);
        }
        byte[] uValidationSalt = u[32..40];
        byte[] uKeySalt = u[40..48];

        // User password?
        byte[] userHash = HashR56(pwd, uValidationSalt, [], r);
        if (FirstBytesEqual(userHash, u, 32) && ue.Length >= 32)
        {
            byte[] ik = HashR56(pwd, uKeySalt, [], r);
            return AesCbcNoPadding(ik, new byte[16], ue);
        }

        // Owner password? (owner salts are hashed together with the full /U.)
        if (o.Length >= 48 && oe.Length >= 32)
        {
            byte[] u48 = u[..48];
            byte[] oValidationSalt = o[32..40];
            byte[] oKeySalt = o[40..48];
            byte[] ownerHash = HashR56(pwd, oValidationSalt, u48, r);
            if (FirstBytesEqual(ownerHash, o, 32))
            {
                byte[] ik = HashR56(pwd, oKeySalt, u48, r);
                return AesCbcNoPadding(ik, new byte[16], oe);
            }
        }

        throw new BitPdfPasswordException(
            passwordProvided ? "The supplied password is incorrect." : "This document is password-protected.",
            passwordProvided);
    }

    private static byte[] HashR56(byte[] pwd, byte[] salt, byte[] userData, int r)
        // R5 uses a single SHA-256; R6 uses the hardened Algorithm 2.B loop.
        => r == 5 ? SHA256.HashData(Concat(pwd, salt, userData)) : Hash2B(pwd, salt, userData);

    private static bool FirstBytesEqual(byte[] a, byte[] b, int n)
    {
        if (a.Length < n || b.Length < n)
        {
            return false;
        }
        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }
        return true;
    }

    private static byte[] PasswordBytes(string? password)
        => string.IsNullOrEmpty(password) ? [] : System.Text.Encoding.UTF8.GetBytes(password);

    /// <summary>Decrypts the raw bytes of a stream belonging to object <paramref name="num"/> <paramref name="gen"/>.</summary>
    public byte[] DecryptStream(byte[] data, int num, int gen) => Decrypt(data, num, gen, _streamCipher);

    /// <summary>Decrypts a string belonging to object <paramref name="num"/> <paramref name="gen"/>.</summary>
    public byte[] DecryptString(byte[] data, int num, int gen) => Decrypt(data, num, gen, _stringCipher);

    private byte[] Decrypt(byte[] data, int num, int gen, BitPdfCipherKind cipher)
    {
        switch (cipher)
        {
            case BitPdfCipherKind.None:
                return data;
            case BitPdfCipherKind.Rc4:
                return BitPdfRc4.Transform(ObjectKey(num, gen, aes: false), data);
            case BitPdfCipherKind.Aes128:
                return AesCbcDecrypt(ObjectKey(num, gen, aes: true), data);
            case BitPdfCipherKind.Aes256:
                return AesCbcDecrypt(_fileKey, data); // V5 uses the file key directly
            default:
                return data;
        }
    }

    private byte[] ObjectKey(int num, int gen, bool aes)
    {
        // Algorithm 1: per-object key from the file key, object and generation.
        int n = _fileKey.Length;
        var input = new byte[n + 5 + (aes ? 4 : 0)];
        Array.Copy(_fileKey, input, n);
        input[n] = (byte)num;
        input[n + 1] = (byte)(num >> 8);
        input[n + 2] = (byte)(num >> 16);
        input[n + 3] = (byte)gen;
        input[n + 4] = (byte)(gen >> 8);
        if (aes)
        {
            input[n + 5] = 0x73; // 's'
            input[n + 6] = 0x41; // 'A'
            input[n + 7] = 0x6C; // 'l'
            input[n + 8] = 0x54; // 'T'
        }

        byte[] hash = Md5(input);
        int keyLen = Math.Min(n + 5, 16);
        return hash[..keyLen];
    }

    private static byte[] ComputeKeyLegacy(byte[] pwd, byte[] o, int p, byte[] id0, int r, int length, bool encryptMetadata)
    {
        // Algorithm 2 with the given (possibly empty) user password.
        byte[] pbytes = [(byte)p, (byte)(p >> 8), (byte)(p >> 16), (byte)(p >> 24)];
        byte[] input = r >= 4 && !encryptMetadata
            ? Concat(Pad(pwd), o.Length >= 32 ? o[..32] : Pad(o), pbytes, id0, [0xFF, 0xFF, 0xFF, 0xFF])
            : Concat(Pad(pwd), o.Length >= 32 ? o[..32] : Pad(o), pbytes, id0);
        byte[] hash = Md5(input);

        // A malformed /Length (e.g. 256 → n=32) would slice past the 16-byte MD5
        // digest and crash. Clamp to the valid RC4/AES-128 key range.
        int n = r == 2 ? 5 : Math.Clamp(length / 8, 5, 16);
        if (r >= 3)
        {
            for (int i = 0; i < 50; i++)
            {
                hash = Md5(hash[..n]);
            }
        }
        return hash[..n];
    }

    private static byte[] Hash2B(byte[] password, byte[] salt, byte[] userData)
    {
        // Algorithm 2.B (revision 6 hardened hash).
        byte[] input = Concat(password, salt, userData);
        byte[] k = SHA256.HashData(input);

        int round = 0;
        while (true)
        {
            byte[] block = Concat(password, k, userData);
            var k1 = new byte[block.Length * 64];
            for (int i = 0; i < 64; i++)
            {
                Array.Copy(block, 0, k1, i * block.Length, block.Length);
            }

            byte[] key = k[..16];
            byte[] iv = k[16..32];
            byte[] e = AesCbcEncryptNoPadding(key, iv, k1);

            int mod = 0;
            for (int i = 0; i < 16; i++)
            {
                mod += e[i];
            }
            mod %= 3;

            k = mod switch
            {
                0 => SHA256.HashData(e),
                1 => SHA384.HashData(e),
                _ => SHA512.HashData(e),
            };

            round++;
            if (round >= 64 && e[^1] <= round - 32)
            {
                break;
            }
        }
        return k[..32];
    }

    private static (BitPdfCipherKind Stream, BitPdfCipherKind Str) ResolveCiphers(BitPdfDict encrypt, int v)
    {
        if (v >= 5)
        {
            return (BitPdfCipherKind.Aes256, BitPdfCipherKind.Aes256);
        }
        if (v == 4 && encrypt.Get("CF") is BitPdfDict cf)
        {
            BitPdfCipherKind Lookup(string filterKey)
            {
                string fname = (encrypt.Get(filterKey) as BitPdfName)?.Value ?? "Identity";
                if (fname == "Identity")
                {
                    return BitPdfCipherKind.None;
                }
                if (cf.Get(fname) is BitPdfDict cfDict)
                {
                    string cfm = (cfDict.Get("CFM") as BitPdfName)?.Value ?? "V2";
                    return cfm switch
                    {
                        "AESV2" => BitPdfCipherKind.Aes128,
                        "AESV3" => BitPdfCipherKind.Aes256,
                        "Identity" => BitPdfCipherKind.None,
                        _ => BitPdfCipherKind.Rc4,
                    };
                }
                return BitPdfCipherKind.Rc4;
            }
            return (Lookup("StmF"), Lookup("StrF"));
        }
        return (BitPdfCipherKind.Rc4, BitPdfCipherKind.Rc4);
    }

    private static byte[] AesCbcDecrypt(byte[] key, byte[] data)
    {
        if (data.Length < 16)
        {
            return [];
        }
        byte[] iv = data[..16];
        byte[] cipher = data[16..];
        if (cipher.Length == 0 || cipher.Length % 16 != 0)
        {
            return [];
        }
        byte[] plain = AesCbcNoPadding(key, iv, cipher);
        return StripPkcs7(plain);
    }

    // Managed AES/MD5 so encrypted PDFs open in the browser WebAssembly sandbox,
    // where System.Security.Cryptography's Aes/MD5 throw PlatformNotSupported.
    private static byte[] AesCbcNoPadding(byte[] key, byte[] iv, byte[] data)
        => BitPdfManagedAes.CbcDecrypt(key, iv, data);

    private static byte[] AesCbcEncryptNoPadding(byte[] key, byte[] iv, byte[] data)
        => BitPdfManagedAes.CbcEncrypt(key, iv, data);

    private static byte[] Md5(byte[] data) => BitPdfManagedMd5.Hash(data);

    private static byte[] StripPkcs7(byte[] data)
    {
        if (data.Length == 0)
        {
            return data;
        }
        int pad = data[^1];
        if (pad is >= 1 and <= 16 && pad <= data.Length)
        {
            return data[..^pad];
        }
        return data;
    }

    private static byte[] Pad(byte[] value)
    {
        var result = new byte[32];
        int len = Math.Min(value.Length, 32);
        Array.Copy(value, result, len);
        Array.Copy(Padding, 0, result, len, 32 - len);
        return result;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        int total = parts.Sum(static p => p.Length);
        var result = new byte[total];
        int offset = 0;
        foreach (var p in parts)
        {
            Array.Copy(p, 0, result, offset, p.Length);
            offset += p.Length;
        }
        return result;
    }

    private static int GetInt(object? value, int fallback) => value is double d ? (int)d : fallback;

    private static byte[] GetStringBytes(object? value) => value is BitPdfString s ? s.Bytes : [];
}
