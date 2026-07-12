// A small managed AES (FIPS-197) with CBC mode, no padding. Used so encrypted
// PDFs (AESV2/AESV3) open in the browser WebAssembly sandbox, where the platform
// Aes implementation throws PlatformNotSupportedException.

namespace Bit.BlazorUI;

internal static class BitPdfManagedAes
{
    private static readonly byte[] SBox = BuildSBox();
    private static readonly byte[] InvSBox = BuildInvSBox();
    private static readonly byte[] Rcon =
    [
        0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1B, 0x36,
        0x6C, 0xD8, 0xAB, 0x4D, 0x9A, 0x2F, 0x5E, 0xBC, 0x63, 0xC6, 0x97,
    ];

    /// <summary>AES-CBC decrypt with no padding. Key length selects AES-128/192/256.</summary>
    public static byte[] CbcDecrypt(byte[] key, byte[] iv, byte[] data)
    {
        uint[] w = ExpandKey(key, out int rounds);
        var output = new byte[data.Length];
        var prev = (byte[])iv.Clone();
        var block = new byte[16];
        for (int off = 0; off + 16 <= data.Length; off += 16)
        {
            Array.Copy(data, off, block, 0, 16);
            byte[] dec = DecryptBlock(block, w, rounds);
            for (int i = 0; i < 16; i++)
            {
                output[off + i] = (byte)(dec[i] ^ prev[i]);
            }
            Array.Copy(data, off, prev, 0, 16);
        }
        return output;
    }

    /// <summary>AES-CBC encrypt with no padding.</summary>
    public static byte[] CbcEncrypt(byte[] key, byte[] iv, byte[] data)
    {
        uint[] w = ExpandKey(key, out int rounds);
        var output = new byte[data.Length];
        var prev = (byte[])iv.Clone();
        var block = new byte[16];
        for (int off = 0; off + 16 <= data.Length; off += 16)
        {
            for (int i = 0; i < 16; i++)
            {
                block[i] = (byte)(data[off + i] ^ prev[i]);
            }
            byte[] enc = EncryptBlock(block, w, rounds);
            Array.Copy(enc, 0, output, off, 16);
            prev = enc;
        }
        return output;
    }

    private static uint[] ExpandKey(byte[] key, out int rounds)
    {
        int nk = key.Length / 4;
        rounds = nk + 6;
        int total = 4 * (rounds + 1);
        var w = new uint[total];
        for (int i = 0; i < nk; i++)
        {
            w[i] = (uint)((key[4 * i] << 24) | (key[4 * i + 1] << 16) | (key[4 * i + 2] << 8) | key[4 * i + 3]);
        }
        for (int i = nk; i < total; i++)
        {
            uint temp = w[i - 1];
            if (i % nk == 0)
            {
                temp = SubWord(RotWord(temp)) ^ ((uint)Rcon[i / nk] << 24);
            }
            else if (nk > 6 && i % nk == 4)
            {
                temp = SubWord(temp);
            }
            w[i] = w[i - nk] ^ temp;
        }
        return w;
    }

    private static byte[] DecryptBlock(byte[] input, uint[] w, int rounds)
    {
        var state = (byte[])input.Clone();
        AddRoundKey(state, w, rounds);
        for (int round = rounds - 1; round >= 1; round--)
        {
            InvShiftRows(state);
            InvSubBytes(state);
            AddRoundKey(state, w, round);
            InvMixColumns(state);
        }
        InvShiftRows(state);
        InvSubBytes(state);
        AddRoundKey(state, w, 0);
        return state;
    }

    private static byte[] EncryptBlock(byte[] input, uint[] w, int rounds)
    {
        var state = (byte[])input.Clone();
        AddRoundKey(state, w, 0);
        for (int round = 1; round < rounds; round++)
        {
            SubBytes(state);
            ShiftRows(state);
            MixColumns(state);
            AddRoundKey(state, w, round);
        }
        SubBytes(state);
        ShiftRows(state);
        AddRoundKey(state, w, rounds);
        return state;
    }

    private static void AddRoundKey(byte[] state, uint[] w, int round)
    {
        for (int c = 0; c < 4; c++)
        {
            uint k = w[round * 4 + c];
            state[c * 4 + 0] ^= (byte)(k >> 24);
            state[c * 4 + 1] ^= (byte)(k >> 16);
            state[c * 4 + 2] ^= (byte)(k >> 8);
            state[c * 4 + 3] ^= (byte)k;
        }
    }

    private static void SubBytes(byte[] s) { for (int i = 0; i < 16; i++) s[i] = SBox[s[i]]; }
    private static void InvSubBytes(byte[] s) { for (int i = 0; i < 16; i++) s[i] = InvSBox[s[i]]; }

    // State is column-major (s[c*4 + r]); rows are the r index across columns.
    private static void ShiftRows(byte[] s)
    {
        for (int r = 1; r < 4; r++)
        {
            byte[] row = [s[r], s[4 + r], s[8 + r], s[12 + r]];
            for (int c = 0; c < 4; c++)
            {
                s[c * 4 + r] = row[(c + r) % 4];
            }
        }
    }

    private static void InvShiftRows(byte[] s)
    {
        for (int r = 1; r < 4; r++)
        {
            byte[] row = [s[r], s[4 + r], s[8 + r], s[12 + r]];
            for (int c = 0; c < 4; c++)
            {
                s[c * 4 + r] = row[(c - r + 4) % 4];
            }
        }
    }

    private static void MixColumns(byte[] s)
    {
        for (int c = 0; c < 4; c++)
        {
            int i = c * 4;
            byte a0 = s[i], a1 = s[i + 1], a2 = s[i + 2], a3 = s[i + 3];
            s[i] = (byte)(Mul(a0, 2) ^ Mul(a1, 3) ^ a2 ^ a3);
            s[i + 1] = (byte)(a0 ^ Mul(a1, 2) ^ Mul(a2, 3) ^ a3);
            s[i + 2] = (byte)(a0 ^ a1 ^ Mul(a2, 2) ^ Mul(a3, 3));
            s[i + 3] = (byte)(Mul(a0, 3) ^ a1 ^ a2 ^ Mul(a3, 2));
        }
    }

    private static void InvMixColumns(byte[] s)
    {
        for (int c = 0; c < 4; c++)
        {
            int i = c * 4;
            byte a0 = s[i], a1 = s[i + 1], a2 = s[i + 2], a3 = s[i + 3];
            s[i] = (byte)(Mul(a0, 14) ^ Mul(a1, 11) ^ Mul(a2, 13) ^ Mul(a3, 9));
            s[i + 1] = (byte)(Mul(a0, 9) ^ Mul(a1, 14) ^ Mul(a2, 11) ^ Mul(a3, 13));
            s[i + 2] = (byte)(Mul(a0, 13) ^ Mul(a1, 9) ^ Mul(a2, 14) ^ Mul(a3, 11));
            s[i + 3] = (byte)(Mul(a0, 11) ^ Mul(a1, 13) ^ Mul(a2, 9) ^ Mul(a3, 14));
        }
    }

    private static byte Mul(byte a, int b)
    {
        int result = 0;
        int aa = a;
        for (int i = 0; i < 8; i++)
        {
            if ((b & 1) != 0)
            {
                result ^= aa;
            }
            bool hi = (aa & 0x80) != 0;
            aa <<= 1;
            if (hi)
            {
                aa ^= 0x11B;
            }
            b >>= 1;
        }
        return (byte)result;
    }

    private static uint SubWord(uint w)
        => ((uint)SBox[(w >> 24) & 0xFF] << 24) | ((uint)SBox[(w >> 16) & 0xFF] << 16)
         | ((uint)SBox[(w >> 8) & 0xFF] << 8) | SBox[w & 0xFF];

    private static uint RotWord(uint w) => (w << 8) | (w >> 24);

    private static byte[] BuildSBox()
    {
        var box = new byte[256];
        byte p = 1, q = 1;
        do
        {
            // p = p * 3 in GF(2^8)
            p = (byte)(p ^ (p << 1) ^ ((p & 0x80) != 0 ? 0x1B : 0));
            // q = q / 3
            q ^= (byte)(q << 1);
            q ^= (byte)(q << 2);
            q ^= (byte)(q << 4);
            q ^= (byte)((q & 0x80) != 0 ? 0x09 : 0);
            byte xformed = (byte)(q ^ Rotl(q, 1) ^ Rotl(q, 2) ^ Rotl(q, 3) ^ Rotl(q, 4) ^ 0x63);
            box[p] = xformed;
        }
        while (p != 1);
        box[0] = 0x63;
        return box;
    }

    private static byte[] BuildInvSBox()
    {
        var box = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            box[SBox[i]] = (byte)i;
        }
        return box;
    }

    private static byte Rotl(byte b, int n) => (byte)((b << n) | (b >> (8 - n)));
}
