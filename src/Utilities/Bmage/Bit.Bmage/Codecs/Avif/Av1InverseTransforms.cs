using System.Runtime.CompilerServices;

namespace Bit.Bmage.Codecs.Avif;

// Generated from the butterfly sequences of the AV1 specification (7.13.2): the inverse DCT for 4 to 64 points and
// the inverse ADST for 8 and 16, unrolled into straight-line code over locals. B is a rotation by cos128/sin128
// rounded by 12 bits (swapping its outputs when flipped) and H a Hadamard step clamped to the stage range.
internal static class Av1InverseTransforms
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

    public static void Dct4(Span<int> t, int lo, int hi)
    {
        _ = t[3];
        int v0 = t[0];
        int v1 = t[2];
        int v2 = t[1];
        int v3 = t[3];
        { long x = v0, y = v1; v1 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v0 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v2, y = v3; v2 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v3 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { int x = v0, y = v3; v0 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v2; v1 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        t[0] = v0;
        t[1] = v1;
        t[2] = v2;
        t[3] = v3;
    }

    public static void Dct8(Span<int> t, int lo, int hi)
    {
        _ = t[7];
        int v0 = t[0];
        int v1 = t[4];
        int v2 = t[2];
        int v3 = t[6];
        int v4 = t[1];
        int v5 = t[5];
        int v6 = t[3];
        int v7 = t[7];
        { long x = v4, y = v7; v4 = (int)((x * 799 - y * 4017 + 2048) >> 12); v7 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v5, y = v6; v5 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v6 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v0, y = v1; v1 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v0 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v2, y = v3; v2 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v3 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { int x = v4, y = v5; v4 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v6; v7 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v3; v0 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v2; v1 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { long x = v6, y = v5; v5 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v0, y = v7; v0 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v6; v1 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v5; v2 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v4; v3 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        t[0] = v0;
        t[1] = v1;
        t[2] = v2;
        t[3] = v3;
        t[4] = v4;
        t[5] = v5;
        t[6] = v6;
        t[7] = v7;
    }

    public static void Dct16(Span<int> t, int lo, int hi)
    {
        _ = t[15];
        int v0 = t[0];
        int v1 = t[8];
        int v2 = t[4];
        int v3 = t[12];
        int v4 = t[2];
        int v5 = t[10];
        int v6 = t[6];
        int v7 = t[14];
        int v8 = t[1];
        int v9 = t[9];
        int v10 = t[5];
        int v11 = t[13];
        int v12 = t[3];
        int v13 = t[11];
        int v14 = t[7];
        int v15 = t[15];
        { long x = v8, y = v15; v8 = (int)((x * 401 - y * 4076 + 2048) >> 12); v15 = (int)((x * 4076 + y * 401 + 2048) >> 12); }
        { long x = v9, y = v14; v9 = (int)((x * 3166 - y * 2598 + 2048) >> 12); v14 = (int)((x * 2598 + y * 3166 + 2048) >> 12); }
        { long x = v10, y = v13; v10 = (int)((x * 1931 - y * 3612 + 2048) >> 12); v13 = (int)((x * 3612 + y * 1931 + 2048) >> 12); }
        { long x = v11, y = v12; v11 = (int)((x * 3920 - y * 1189 + 2048) >> 12); v12 = (int)((x * 1189 + y * 3920 + 2048) >> 12); }
        { long x = v4, y = v7; v4 = (int)((x * 799 - y * 4017 + 2048) >> 12); v7 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v5, y = v6; v5 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v6 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { int x = v8, y = v9; v8 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v10; v11 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v13; v12 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v14; v15 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { long x = v0, y = v1; v1 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v0 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v2, y = v3; v2 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v3 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { int x = v4, y = v5; v4 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v6; v7 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { long x = v14, y = v9; v9 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v14 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v13, y = v10; v10 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v13 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v0, y = v3; v0 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v2; v1 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { long x = v6, y = v5; v5 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v8, y = v11; v8 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v10; v9 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v12; v15 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v13; v14 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v7; v0 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v6; v1 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v5; v2 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v4; v3 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        { long x = v13, y = v10; v10 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v13 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v12, y = v11; v11 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v12 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v0, y = v15; v0 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v14; v1 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v13; v2 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v12; v3 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v11; v4 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v10; v5 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v9; v6 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v8; v7 = Clamp(x + y, lo, hi); v8 = Clamp(x - y, lo, hi); }
        t[0] = v0;
        t[1] = v1;
        t[2] = v2;
        t[3] = v3;
        t[4] = v4;
        t[5] = v5;
        t[6] = v6;
        t[7] = v7;
        t[8] = v8;
        t[9] = v9;
        t[10] = v10;
        t[11] = v11;
        t[12] = v12;
        t[13] = v13;
        t[14] = v14;
        t[15] = v15;
    }

    public static void Dct32(Span<int> t, int lo, int hi)
    {
        _ = t[31];
        int v0 = t[0];
        int v1 = t[16];
        int v2 = t[8];
        int v3 = t[24];
        int v4 = t[4];
        int v5 = t[20];
        int v6 = t[12];
        int v7 = t[28];
        int v8 = t[2];
        int v9 = t[18];
        int v10 = t[10];
        int v11 = t[26];
        int v12 = t[6];
        int v13 = t[22];
        int v14 = t[14];
        int v15 = t[30];
        int v16 = t[1];
        int v17 = t[17];
        int v18 = t[9];
        int v19 = t[25];
        int v20 = t[5];
        int v21 = t[21];
        int v22 = t[13];
        int v23 = t[29];
        int v24 = t[3];
        int v25 = t[19];
        int v26 = t[11];
        int v27 = t[27];
        int v28 = t[7];
        int v29 = t[23];
        int v30 = t[15];
        int v31 = t[31];
        { long x = v16, y = v31; v16 = (int)((x * 201 - y * 4091 + 2048) >> 12); v31 = (int)((x * 4091 + y * 201 + 2048) >> 12); }
        { long x = v17, y = v30; v17 = (int)((x * 3035 - y * 2751 + 2048) >> 12); v30 = (int)((x * 2751 + y * 3035 + 2048) >> 12); }
        { long x = v18, y = v29; v18 = (int)((x * 1751 - y * 3703 + 2048) >> 12); v29 = (int)((x * 3703 + y * 1751 + 2048) >> 12); }
        { long x = v19, y = v28; v19 = (int)((x * 3857 - y * 1380 + 2048) >> 12); v28 = (int)((x * 1380 + y * 3857 + 2048) >> 12); }
        { long x = v20, y = v27; v20 = (int)((x * 995 - y * 3973 + 2048) >> 12); v27 = (int)((x * 3973 + y * 995 + 2048) >> 12); }
        { long x = v21, y = v26; v21 = (int)((x * 3513 - y * 2106 + 2048) >> 12); v26 = (int)((x * 2106 + y * 3513 + 2048) >> 12); }
        { long x = v22, y = v25; v22 = (int)((x * 2440 - y * 3290 + 2048) >> 12); v25 = (int)((x * 3290 + y * 2440 + 2048) >> 12); }
        { long x = v23, y = v24; v23 = (int)((x * 4052 - y * 601 + 2048) >> 12); v24 = (int)((x * 601 + y * 4052 + 2048) >> 12); }
        { long x = v8, y = v15; v8 = (int)((x * 401 - y * 4076 + 2048) >> 12); v15 = (int)((x * 4076 + y * 401 + 2048) >> 12); }
        { long x = v9, y = v14; v9 = (int)((x * 3166 - y * 2598 + 2048) >> 12); v14 = (int)((x * 2598 + y * 3166 + 2048) >> 12); }
        { long x = v10, y = v13; v10 = (int)((x * 1931 - y * 3612 + 2048) >> 12); v13 = (int)((x * 3612 + y * 1931 + 2048) >> 12); }
        { long x = v11, y = v12; v11 = (int)((x * 3920 - y * 1189 + 2048) >> 12); v12 = (int)((x * 1189 + y * 3920 + 2048) >> 12); }
        { int x = v16, y = v17; v16 = Clamp(x + y, lo, hi); v17 = Clamp(x - y, lo, hi); }
        { int x = v19, y = v18; v19 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v20, y = v21; v20 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v23, y = v22; v23 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v24, y = v25; v24 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v27, y = v26; v27 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v28, y = v29; v28 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v30; v31 = Clamp(x + y, lo, hi); v30 = Clamp(x - y, lo, hi); }
        { long x = v4, y = v7; v4 = (int)((x * 799 - y * 4017 + 2048) >> 12); v7 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v5, y = v6; v5 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v6 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { int x = v8, y = v9; v8 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v10; v11 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v13; v12 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v14; v15 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { long x = v30, y = v17; v17 = (int)((x * 799 - y * 4017 + 2048) >> 12); v30 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v29, y = v18; v18 = (int)((x * -4017 - y * 799 + 2048) >> 12); v29 = (int)((x * 799 + y * -4017 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v26 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v25, y = v22; v22 = (int)((x * -2276 - y * 3406 + 2048) >> 12); v25 = (int)((x * 3406 + y * -2276 + 2048) >> 12); }
        { long x = v0, y = v1; v1 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v0 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v2, y = v3; v2 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v3 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { int x = v4, y = v5; v4 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v6; v7 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { long x = v14, y = v9; v9 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v14 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v13, y = v10; v10 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v13 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v16, y = v19; v16 = Clamp(x + y, lo, hi); v19 = Clamp(x - y, lo, hi); }
        { int x = v17, y = v18; v17 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v23, y = v20; v23 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v22, y = v21; v22 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v24, y = v27; v24 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { int x = v25, y = v26; v25 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v28; v31 = Clamp(x + y, lo, hi); v28 = Clamp(x - y, lo, hi); }
        { int x = v30, y = v29; v30 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v3; v0 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v2; v1 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { long x = v6, y = v5; v5 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v8, y = v11; v8 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v10; v9 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v12; v15 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v13; v14 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { long x = v29, y = v18; v18 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v29 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v28, y = v19; v19 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v28 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v27, y = v20; v20 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v27 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v26 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v0, y = v7; v0 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v6; v1 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v5; v2 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v4; v3 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        { long x = v13, y = v10; v10 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v13 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v12, y = v11; v11 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v12 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v16, y = v23; v16 = Clamp(x + y, lo, hi); v23 = Clamp(x - y, lo, hi); }
        { int x = v17, y = v22; v17 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v18, y = v21; v18 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v19, y = v20; v19 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v24; v31 = Clamp(x + y, lo, hi); v24 = Clamp(x - y, lo, hi); }
        { int x = v30, y = v25; v30 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v29, y = v26; v29 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v28, y = v27; v28 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v15; v0 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v14; v1 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v13; v2 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v12; v3 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v11; v4 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v10; v5 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v9; v6 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v8; v7 = Clamp(x + y, lo, hi); v8 = Clamp(x - y, lo, hi); }
        { long x = v27, y = v20; v20 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v27 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v26 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v25, y = v22; v22 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v25 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v24, y = v23; v23 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v24 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v0, y = v31; v0 = Clamp(x + y, lo, hi); v31 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v30; v1 = Clamp(x + y, lo, hi); v30 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v29; v2 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v28; v3 = Clamp(x + y, lo, hi); v28 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v27; v4 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v26; v5 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v25; v6 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v24; v7 = Clamp(x + y, lo, hi); v24 = Clamp(x - y, lo, hi); }
        { int x = v8, y = v23; v8 = Clamp(x + y, lo, hi); v23 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v22; v9 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v10, y = v21; v10 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v20; v11 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v19; v12 = Clamp(x + y, lo, hi); v19 = Clamp(x - y, lo, hi); }
        { int x = v13, y = v18; v13 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v17; v14 = Clamp(x + y, lo, hi); v17 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v16; v15 = Clamp(x + y, lo, hi); v16 = Clamp(x - y, lo, hi); }
        t[0] = v0;
        t[1] = v1;
        t[2] = v2;
        t[3] = v3;
        t[4] = v4;
        t[5] = v5;
        t[6] = v6;
        t[7] = v7;
        t[8] = v8;
        t[9] = v9;
        t[10] = v10;
        t[11] = v11;
        t[12] = v12;
        t[13] = v13;
        t[14] = v14;
        t[15] = v15;
        t[16] = v16;
        t[17] = v17;
        t[18] = v18;
        t[19] = v19;
        t[20] = v20;
        t[21] = v21;
        t[22] = v22;
        t[23] = v23;
        t[24] = v24;
        t[25] = v25;
        t[26] = v26;
        t[27] = v27;
        t[28] = v28;
        t[29] = v29;
        t[30] = v30;
        t[31] = v31;
    }

    public static void Dct64(Span<int> t, int lo, int hi)
    {
        _ = t[63];
        int v0 = t[0];
        int v1 = t[32];
        int v2 = t[16];
        int v3 = t[48];
        int v4 = t[8];
        int v5 = t[40];
        int v6 = t[24];
        int v7 = t[56];
        int v8 = t[4];
        int v9 = t[36];
        int v10 = t[20];
        int v11 = t[52];
        int v12 = t[12];
        int v13 = t[44];
        int v14 = t[28];
        int v15 = t[60];
        int v16 = t[2];
        int v17 = t[34];
        int v18 = t[18];
        int v19 = t[50];
        int v20 = t[10];
        int v21 = t[42];
        int v22 = t[26];
        int v23 = t[58];
        int v24 = t[6];
        int v25 = t[38];
        int v26 = t[22];
        int v27 = t[54];
        int v28 = t[14];
        int v29 = t[46];
        int v30 = t[30];
        int v31 = t[62];
        int v32 = t[1];
        int v33 = t[33];
        int v34 = t[17];
        int v35 = t[49];
        int v36 = t[9];
        int v37 = t[41];
        int v38 = t[25];
        int v39 = t[57];
        int v40 = t[5];
        int v41 = t[37];
        int v42 = t[21];
        int v43 = t[53];
        int v44 = t[13];
        int v45 = t[45];
        int v46 = t[29];
        int v47 = t[61];
        int v48 = t[3];
        int v49 = t[35];
        int v50 = t[19];
        int v51 = t[51];
        int v52 = t[11];
        int v53 = t[43];
        int v54 = t[27];
        int v55 = t[59];
        int v56 = t[7];
        int v57 = t[39];
        int v58 = t[23];
        int v59 = t[55];
        int v60 = t[15];
        int v61 = t[47];
        int v62 = t[31];
        int v63 = t[63];
        { long x = v32, y = v63; v32 = (int)((x * 101 - y * 4095 + 2048) >> 12); v63 = (int)((x * 4095 + y * 101 + 2048) >> 12); }
        { long x = v33, y = v62; v33 = (int)((x * 2967 - y * 2824 + 2048) >> 12); v62 = (int)((x * 2824 + y * 2967 + 2048) >> 12); }
        { long x = v34, y = v61; v34 = (int)((x * 1660 - y * 3745 + 2048) >> 12); v61 = (int)((x * 3745 + y * 1660 + 2048) >> 12); }
        { long x = v35, y = v60; v35 = (int)((x * 3822 - y * 1474 + 2048) >> 12); v60 = (int)((x * 1474 + y * 3822 + 2048) >> 12); }
        { long x = v36, y = v59; v36 = (int)((x * 897 - y * 3996 + 2048) >> 12); v59 = (int)((x * 3996 + y * 897 + 2048) >> 12); }
        { long x = v37, y = v58; v37 = (int)((x * 3461 - y * 2191 + 2048) >> 12); v58 = (int)((x * 2191 + y * 3461 + 2048) >> 12); }
        { long x = v38, y = v57; v38 = (int)((x * 2359 - y * 3349 + 2048) >> 12); v57 = (int)((x * 3349 + y * 2359 + 2048) >> 12); }
        { long x = v39, y = v56; v39 = (int)((x * 4036 - y * 700 + 2048) >> 12); v56 = (int)((x * 700 + y * 4036 + 2048) >> 12); }
        { long x = v40, y = v55; v40 = (int)((x * 501 - y * 4065 + 2048) >> 12); v55 = (int)((x * 4065 + y * 501 + 2048) >> 12); }
        { long x = v41, y = v54; v41 = (int)((x * 3229 - y * 2520 + 2048) >> 12); v54 = (int)((x * 2520 + y * 3229 + 2048) >> 12); }
        { long x = v42, y = v53; v42 = (int)((x * 2019 - y * 3564 + 2048) >> 12); v53 = (int)((x * 3564 + y * 2019 + 2048) >> 12); }
        { long x = v43, y = v52; v43 = (int)((x * 3948 - y * 1092 + 2048) >> 12); v52 = (int)((x * 1092 + y * 3948 + 2048) >> 12); }
        { long x = v44, y = v51; v44 = (int)((x * 1285 - y * 3889 + 2048) >> 12); v51 = (int)((x * 3889 + y * 1285 + 2048) >> 12); }
        { long x = v45, y = v50; v45 = (int)((x * 3659 - y * 1842 + 2048) >> 12); v50 = (int)((x * 1842 + y * 3659 + 2048) >> 12); }
        { long x = v46, y = v49; v46 = (int)((x * 2675 - y * 3102 + 2048) >> 12); v49 = (int)((x * 3102 + y * 2675 + 2048) >> 12); }
        { long x = v47, y = v48; v47 = (int)((x * 4085 - y * 301 + 2048) >> 12); v48 = (int)((x * 301 + y * 4085 + 2048) >> 12); }
        { long x = v16, y = v31; v16 = (int)((x * 201 - y * 4091 + 2048) >> 12); v31 = (int)((x * 4091 + y * 201 + 2048) >> 12); }
        { long x = v17, y = v30; v17 = (int)((x * 3035 - y * 2751 + 2048) >> 12); v30 = (int)((x * 2751 + y * 3035 + 2048) >> 12); }
        { long x = v18, y = v29; v18 = (int)((x * 1751 - y * 3703 + 2048) >> 12); v29 = (int)((x * 3703 + y * 1751 + 2048) >> 12); }
        { long x = v19, y = v28; v19 = (int)((x * 3857 - y * 1380 + 2048) >> 12); v28 = (int)((x * 1380 + y * 3857 + 2048) >> 12); }
        { long x = v20, y = v27; v20 = (int)((x * 995 - y * 3973 + 2048) >> 12); v27 = (int)((x * 3973 + y * 995 + 2048) >> 12); }
        { long x = v21, y = v26; v21 = (int)((x * 3513 - y * 2106 + 2048) >> 12); v26 = (int)((x * 2106 + y * 3513 + 2048) >> 12); }
        { long x = v22, y = v25; v22 = (int)((x * 2440 - y * 3290 + 2048) >> 12); v25 = (int)((x * 3290 + y * 2440 + 2048) >> 12); }
        { long x = v23, y = v24; v23 = (int)((x * 4052 - y * 601 + 2048) >> 12); v24 = (int)((x * 601 + y * 4052 + 2048) >> 12); }
        { int x = v32, y = v33; v32 = Clamp(x + y, lo, hi); v33 = Clamp(x - y, lo, hi); }
        { int x = v35, y = v34; v35 = Clamp(x + y, lo, hi); v34 = Clamp(x - y, lo, hi); }
        { int x = v36, y = v37; v36 = Clamp(x + y, lo, hi); v37 = Clamp(x - y, lo, hi); }
        { int x = v39, y = v38; v39 = Clamp(x + y, lo, hi); v38 = Clamp(x - y, lo, hi); }
        { int x = v40, y = v41; v40 = Clamp(x + y, lo, hi); v41 = Clamp(x - y, lo, hi); }
        { int x = v43, y = v42; v43 = Clamp(x + y, lo, hi); v42 = Clamp(x - y, lo, hi); }
        { int x = v44, y = v45; v44 = Clamp(x + y, lo, hi); v45 = Clamp(x - y, lo, hi); }
        { int x = v47, y = v46; v47 = Clamp(x + y, lo, hi); v46 = Clamp(x - y, lo, hi); }
        { int x = v48, y = v49; v48 = Clamp(x + y, lo, hi); v49 = Clamp(x - y, lo, hi); }
        { int x = v51, y = v50; v51 = Clamp(x + y, lo, hi); v50 = Clamp(x - y, lo, hi); }
        { int x = v52, y = v53; v52 = Clamp(x + y, lo, hi); v53 = Clamp(x - y, lo, hi); }
        { int x = v55, y = v54; v55 = Clamp(x + y, lo, hi); v54 = Clamp(x - y, lo, hi); }
        { int x = v56, y = v57; v56 = Clamp(x + y, lo, hi); v57 = Clamp(x - y, lo, hi); }
        { int x = v59, y = v58; v59 = Clamp(x + y, lo, hi); v58 = Clamp(x - y, lo, hi); }
        { int x = v60, y = v61; v60 = Clamp(x + y, lo, hi); v61 = Clamp(x - y, lo, hi); }
        { int x = v63, y = v62; v63 = Clamp(x + y, lo, hi); v62 = Clamp(x - y, lo, hi); }
        { long x = v8, y = v15; v8 = (int)((x * 401 - y * 4076 + 2048) >> 12); v15 = (int)((x * 4076 + y * 401 + 2048) >> 12); }
        { long x = v9, y = v14; v9 = (int)((x * 3166 - y * 2598 + 2048) >> 12); v14 = (int)((x * 2598 + y * 3166 + 2048) >> 12); }
        { long x = v10, y = v13; v10 = (int)((x * 1931 - y * 3612 + 2048) >> 12); v13 = (int)((x * 3612 + y * 1931 + 2048) >> 12); }
        { long x = v11, y = v12; v11 = (int)((x * 3920 - y * 1189 + 2048) >> 12); v12 = (int)((x * 1189 + y * 3920 + 2048) >> 12); }
        { int x = v16, y = v17; v16 = Clamp(x + y, lo, hi); v17 = Clamp(x - y, lo, hi); }
        { int x = v19, y = v18; v19 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v20, y = v21; v20 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v23, y = v22; v23 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v24, y = v25; v24 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v27, y = v26; v27 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v28, y = v29; v28 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v30; v31 = Clamp(x + y, lo, hi); v30 = Clamp(x - y, lo, hi); }
        { long x = v62, y = v33; v33 = (int)((x * 401 - y * 4076 + 2048) >> 12); v62 = (int)((x * 4076 + y * 401 + 2048) >> 12); }
        { long x = v61, y = v34; v34 = (int)((x * -4076 - y * 401 + 2048) >> 12); v61 = (int)((x * 401 + y * -4076 + 2048) >> 12); }
        { long x = v58, y = v37; v37 = (int)((x * 3166 - y * 2598 + 2048) >> 12); v58 = (int)((x * 2598 + y * 3166 + 2048) >> 12); }
        { long x = v57, y = v38; v38 = (int)((x * -2598 - y * 3166 + 2048) >> 12); v57 = (int)((x * 3166 + y * -2598 + 2048) >> 12); }
        { long x = v54, y = v41; v41 = (int)((x * 1931 - y * 3612 + 2048) >> 12); v54 = (int)((x * 3612 + y * 1931 + 2048) >> 12); }
        { long x = v53, y = v42; v42 = (int)((x * -3612 - y * 1931 + 2048) >> 12); v53 = (int)((x * 1931 + y * -3612 + 2048) >> 12); }
        { long x = v50, y = v45; v45 = (int)((x * 3920 - y * 1189 + 2048) >> 12); v50 = (int)((x * 1189 + y * 3920 + 2048) >> 12); }
        { long x = v49, y = v46; v46 = (int)((x * -1189 - y * 3920 + 2048) >> 12); v49 = (int)((x * 3920 + y * -1189 + 2048) >> 12); }
        { long x = v4, y = v7; v4 = (int)((x * 799 - y * 4017 + 2048) >> 12); v7 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v5, y = v6; v5 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v6 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { int x = v8, y = v9; v8 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v10; v11 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v13; v12 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v14; v15 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { long x = v30, y = v17; v17 = (int)((x * 799 - y * 4017 + 2048) >> 12); v30 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v29, y = v18; v18 = (int)((x * -4017 - y * 799 + 2048) >> 12); v29 = (int)((x * 799 + y * -4017 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v26 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v25, y = v22; v22 = (int)((x * -2276 - y * 3406 + 2048) >> 12); v25 = (int)((x * 3406 + y * -2276 + 2048) >> 12); }
        { int x = v32, y = v35; v32 = Clamp(x + y, lo, hi); v35 = Clamp(x - y, lo, hi); }
        { int x = v33, y = v34; v33 = Clamp(x + y, lo, hi); v34 = Clamp(x - y, lo, hi); }
        { int x = v39, y = v36; v39 = Clamp(x + y, lo, hi); v36 = Clamp(x - y, lo, hi); }
        { int x = v38, y = v37; v38 = Clamp(x + y, lo, hi); v37 = Clamp(x - y, lo, hi); }
        { int x = v40, y = v43; v40 = Clamp(x + y, lo, hi); v43 = Clamp(x - y, lo, hi); }
        { int x = v41, y = v42; v41 = Clamp(x + y, lo, hi); v42 = Clamp(x - y, lo, hi); }
        { int x = v47, y = v44; v47 = Clamp(x + y, lo, hi); v44 = Clamp(x - y, lo, hi); }
        { int x = v46, y = v45; v46 = Clamp(x + y, lo, hi); v45 = Clamp(x - y, lo, hi); }
        { int x = v48, y = v51; v48 = Clamp(x + y, lo, hi); v51 = Clamp(x - y, lo, hi); }
        { int x = v49, y = v50; v49 = Clamp(x + y, lo, hi); v50 = Clamp(x - y, lo, hi); }
        { int x = v55, y = v52; v55 = Clamp(x + y, lo, hi); v52 = Clamp(x - y, lo, hi); }
        { int x = v54, y = v53; v54 = Clamp(x + y, lo, hi); v53 = Clamp(x - y, lo, hi); }
        { int x = v56, y = v59; v56 = Clamp(x + y, lo, hi); v59 = Clamp(x - y, lo, hi); }
        { int x = v57, y = v58; v57 = Clamp(x + y, lo, hi); v58 = Clamp(x - y, lo, hi); }
        { int x = v63, y = v60; v63 = Clamp(x + y, lo, hi); v60 = Clamp(x - y, lo, hi); }
        { int x = v62, y = v61; v62 = Clamp(x + y, lo, hi); v61 = Clamp(x - y, lo, hi); }
        { long x = v0, y = v1; v1 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v0 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v2, y = v3; v2 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v3 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { int x = v4, y = v5; v4 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v6; v7 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { long x = v14, y = v9; v9 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v14 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v13, y = v10; v10 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v13 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v16, y = v19; v16 = Clamp(x + y, lo, hi); v19 = Clamp(x - y, lo, hi); }
        { int x = v17, y = v18; v17 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v23, y = v20; v23 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v22, y = v21; v22 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v24, y = v27; v24 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { int x = v25, y = v26; v25 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v28; v31 = Clamp(x + y, lo, hi); v28 = Clamp(x - y, lo, hi); }
        { int x = v30, y = v29; v30 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { long x = v61, y = v34; v34 = (int)((x * 799 - y * 4017 + 2048) >> 12); v61 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v60, y = v35; v35 = (int)((x * 799 - y * 4017 + 2048) >> 12); v60 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v59, y = v36; v36 = (int)((x * -4017 - y * 799 + 2048) >> 12); v59 = (int)((x * 799 + y * -4017 + 2048) >> 12); }
        { long x = v58, y = v37; v37 = (int)((x * -4017 - y * 799 + 2048) >> 12); v58 = (int)((x * 799 + y * -4017 + 2048) >> 12); }
        { long x = v53, y = v42; v42 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v53 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v52, y = v43; v43 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v52 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v51, y = v44; v44 = (int)((x * -2276 - y * 3406 + 2048) >> 12); v51 = (int)((x * 3406 + y * -2276 + 2048) >> 12); }
        { long x = v50, y = v45; v45 = (int)((x * -2276 - y * 3406 + 2048) >> 12); v50 = (int)((x * 3406 + y * -2276 + 2048) >> 12); }
        { int x = v0, y = v3; v0 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v2; v1 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { long x = v6, y = v5; v5 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v8, y = v11; v8 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v10; v9 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v12; v15 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v13; v14 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { long x = v29, y = v18; v18 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v29 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v28, y = v19; v19 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v28 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v27, y = v20; v20 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v27 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v26 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v32, y = v39; v32 = Clamp(x + y, lo, hi); v39 = Clamp(x - y, lo, hi); }
        { int x = v33, y = v38; v33 = Clamp(x + y, lo, hi); v38 = Clamp(x - y, lo, hi); }
        { int x = v34, y = v37; v34 = Clamp(x + y, lo, hi); v37 = Clamp(x - y, lo, hi); }
        { int x = v35, y = v36; v35 = Clamp(x + y, lo, hi); v36 = Clamp(x - y, lo, hi); }
        { int x = v47, y = v40; v47 = Clamp(x + y, lo, hi); v40 = Clamp(x - y, lo, hi); }
        { int x = v46, y = v41; v46 = Clamp(x + y, lo, hi); v41 = Clamp(x - y, lo, hi); }
        { int x = v45, y = v42; v45 = Clamp(x + y, lo, hi); v42 = Clamp(x - y, lo, hi); }
        { int x = v44, y = v43; v44 = Clamp(x + y, lo, hi); v43 = Clamp(x - y, lo, hi); }
        { int x = v48, y = v55; v48 = Clamp(x + y, lo, hi); v55 = Clamp(x - y, lo, hi); }
        { int x = v49, y = v54; v49 = Clamp(x + y, lo, hi); v54 = Clamp(x - y, lo, hi); }
        { int x = v50, y = v53; v50 = Clamp(x + y, lo, hi); v53 = Clamp(x - y, lo, hi); }
        { int x = v51, y = v52; v51 = Clamp(x + y, lo, hi); v52 = Clamp(x - y, lo, hi); }
        { int x = v63, y = v56; v63 = Clamp(x + y, lo, hi); v56 = Clamp(x - y, lo, hi); }
        { int x = v62, y = v57; v62 = Clamp(x + y, lo, hi); v57 = Clamp(x - y, lo, hi); }
        { int x = v61, y = v58; v61 = Clamp(x + y, lo, hi); v58 = Clamp(x - y, lo, hi); }
        { int x = v60, y = v59; v60 = Clamp(x + y, lo, hi); v59 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v7; v0 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v6; v1 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v5; v2 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v4; v3 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        { long x = v13, y = v10; v10 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v13 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v12, y = v11; v11 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v12 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v16, y = v23; v16 = Clamp(x + y, lo, hi); v23 = Clamp(x - y, lo, hi); }
        { int x = v17, y = v22; v17 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v18, y = v21; v18 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v19, y = v20; v19 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v24; v31 = Clamp(x + y, lo, hi); v24 = Clamp(x - y, lo, hi); }
        { int x = v30, y = v25; v30 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v29, y = v26; v29 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v28, y = v27; v28 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { long x = v59, y = v36; v36 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v59 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v58, y = v37; v37 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v58 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v57, y = v38; v38 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v57 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v56, y = v39; v39 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v56 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v55, y = v40; v40 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v55 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { long x = v54, y = v41; v41 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v54 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { long x = v53, y = v42; v42 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v53 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { long x = v52, y = v43; v43 = (int)((x * -3784 - y * 1567 + 2048) >> 12); v52 = (int)((x * 1567 + y * -3784 + 2048) >> 12); }
        { int x = v0, y = v15; v0 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v14; v1 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v13; v2 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v12; v3 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v11; v4 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v10; v5 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v9; v6 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v8; v7 = Clamp(x + y, lo, hi); v8 = Clamp(x - y, lo, hi); }
        { long x = v27, y = v20; v20 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v27 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v26, y = v21; v21 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v26 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v25, y = v22; v22 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v25 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v24, y = v23; v23 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v24 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v32, y = v47; v32 = Clamp(x + y, lo, hi); v47 = Clamp(x - y, lo, hi); }
        { int x = v63, y = v48; v63 = Clamp(x + y, lo, hi); v48 = Clamp(x - y, lo, hi); }
        { int x = v33, y = v46; v33 = Clamp(x + y, lo, hi); v46 = Clamp(x - y, lo, hi); }
        { int x = v62, y = v49; v62 = Clamp(x + y, lo, hi); v49 = Clamp(x - y, lo, hi); }
        { int x = v34, y = v45; v34 = Clamp(x + y, lo, hi); v45 = Clamp(x - y, lo, hi); }
        { int x = v61, y = v50; v61 = Clamp(x + y, lo, hi); v50 = Clamp(x - y, lo, hi); }
        { int x = v35, y = v44; v35 = Clamp(x + y, lo, hi); v44 = Clamp(x - y, lo, hi); }
        { int x = v60, y = v51; v60 = Clamp(x + y, lo, hi); v51 = Clamp(x - y, lo, hi); }
        { int x = v36, y = v43; v36 = Clamp(x + y, lo, hi); v43 = Clamp(x - y, lo, hi); }
        { int x = v59, y = v52; v59 = Clamp(x + y, lo, hi); v52 = Clamp(x - y, lo, hi); }
        { int x = v37, y = v42; v37 = Clamp(x + y, lo, hi); v42 = Clamp(x - y, lo, hi); }
        { int x = v58, y = v53; v58 = Clamp(x + y, lo, hi); v53 = Clamp(x - y, lo, hi); }
        { int x = v38, y = v41; v38 = Clamp(x + y, lo, hi); v41 = Clamp(x - y, lo, hi); }
        { int x = v57, y = v54; v57 = Clamp(x + y, lo, hi); v54 = Clamp(x - y, lo, hi); }
        { int x = v39, y = v40; v39 = Clamp(x + y, lo, hi); v40 = Clamp(x - y, lo, hi); }
        { int x = v56, y = v55; v56 = Clamp(x + y, lo, hi); v55 = Clamp(x - y, lo, hi); }
        { int x = v0, y = v31; v0 = Clamp(x + y, lo, hi); v31 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v30; v1 = Clamp(x + y, lo, hi); v30 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v29; v2 = Clamp(x + y, lo, hi); v29 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v28; v3 = Clamp(x + y, lo, hi); v28 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v27; v4 = Clamp(x + y, lo, hi); v27 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v26; v5 = Clamp(x + y, lo, hi); v26 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v25; v6 = Clamp(x + y, lo, hi); v25 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v24; v7 = Clamp(x + y, lo, hi); v24 = Clamp(x - y, lo, hi); }
        { int x = v8, y = v23; v8 = Clamp(x + y, lo, hi); v23 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v22; v9 = Clamp(x + y, lo, hi); v22 = Clamp(x - y, lo, hi); }
        { int x = v10, y = v21; v10 = Clamp(x + y, lo, hi); v21 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v20; v11 = Clamp(x + y, lo, hi); v20 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v19; v12 = Clamp(x + y, lo, hi); v19 = Clamp(x - y, lo, hi); }
        { int x = v13, y = v18; v13 = Clamp(x + y, lo, hi); v18 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v17; v14 = Clamp(x + y, lo, hi); v17 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v16; v15 = Clamp(x + y, lo, hi); v16 = Clamp(x - y, lo, hi); }
        { long x = v55, y = v40; v40 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v55 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v54, y = v41; v41 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v54 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v53, y = v42; v42 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v53 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v52, y = v43; v43 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v52 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v51, y = v44; v44 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v51 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v50, y = v45; v45 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v50 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v49, y = v46; v46 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v49 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v48, y = v47; v47 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v48 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { int x = v0, y = v63; v0 = Clamp(x + y, lo, hi); v63 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v62; v1 = Clamp(x + y, lo, hi); v62 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v61; v2 = Clamp(x + y, lo, hi); v61 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v60; v3 = Clamp(x + y, lo, hi); v60 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v59; v4 = Clamp(x + y, lo, hi); v59 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v58; v5 = Clamp(x + y, lo, hi); v58 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v57; v6 = Clamp(x + y, lo, hi); v57 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v56; v7 = Clamp(x + y, lo, hi); v56 = Clamp(x - y, lo, hi); }
        { int x = v8, y = v55; v8 = Clamp(x + y, lo, hi); v55 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v54; v9 = Clamp(x + y, lo, hi); v54 = Clamp(x - y, lo, hi); }
        { int x = v10, y = v53; v10 = Clamp(x + y, lo, hi); v53 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v52; v11 = Clamp(x + y, lo, hi); v52 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v51; v12 = Clamp(x + y, lo, hi); v51 = Clamp(x - y, lo, hi); }
        { int x = v13, y = v50; v13 = Clamp(x + y, lo, hi); v50 = Clamp(x - y, lo, hi); }
        { int x = v14, y = v49; v14 = Clamp(x + y, lo, hi); v49 = Clamp(x - y, lo, hi); }
        { int x = v15, y = v48; v15 = Clamp(x + y, lo, hi); v48 = Clamp(x - y, lo, hi); }
        { int x = v16, y = v47; v16 = Clamp(x + y, lo, hi); v47 = Clamp(x - y, lo, hi); }
        { int x = v17, y = v46; v17 = Clamp(x + y, lo, hi); v46 = Clamp(x - y, lo, hi); }
        { int x = v18, y = v45; v18 = Clamp(x + y, lo, hi); v45 = Clamp(x - y, lo, hi); }
        { int x = v19, y = v44; v19 = Clamp(x + y, lo, hi); v44 = Clamp(x - y, lo, hi); }
        { int x = v20, y = v43; v20 = Clamp(x + y, lo, hi); v43 = Clamp(x - y, lo, hi); }
        { int x = v21, y = v42; v21 = Clamp(x + y, lo, hi); v42 = Clamp(x - y, lo, hi); }
        { int x = v22, y = v41; v22 = Clamp(x + y, lo, hi); v41 = Clamp(x - y, lo, hi); }
        { int x = v23, y = v40; v23 = Clamp(x + y, lo, hi); v40 = Clamp(x - y, lo, hi); }
        { int x = v24, y = v39; v24 = Clamp(x + y, lo, hi); v39 = Clamp(x - y, lo, hi); }
        { int x = v25, y = v38; v25 = Clamp(x + y, lo, hi); v38 = Clamp(x - y, lo, hi); }
        { int x = v26, y = v37; v26 = Clamp(x + y, lo, hi); v37 = Clamp(x - y, lo, hi); }
        { int x = v27, y = v36; v27 = Clamp(x + y, lo, hi); v36 = Clamp(x - y, lo, hi); }
        { int x = v28, y = v35; v28 = Clamp(x + y, lo, hi); v35 = Clamp(x - y, lo, hi); }
        { int x = v29, y = v34; v29 = Clamp(x + y, lo, hi); v34 = Clamp(x - y, lo, hi); }
        { int x = v30, y = v33; v30 = Clamp(x + y, lo, hi); v33 = Clamp(x - y, lo, hi); }
        { int x = v31, y = v32; v31 = Clamp(x + y, lo, hi); v32 = Clamp(x - y, lo, hi); }
        t[0] = v0;
        t[1] = v1;
        t[2] = v2;
        t[3] = v3;
        t[4] = v4;
        t[5] = v5;
        t[6] = v6;
        t[7] = v7;
        t[8] = v8;
        t[9] = v9;
        t[10] = v10;
        t[11] = v11;
        t[12] = v12;
        t[13] = v13;
        t[14] = v14;
        t[15] = v15;
        t[16] = v16;
        t[17] = v17;
        t[18] = v18;
        t[19] = v19;
        t[20] = v20;
        t[21] = v21;
        t[22] = v22;
        t[23] = v23;
        t[24] = v24;
        t[25] = v25;
        t[26] = v26;
        t[27] = v27;
        t[28] = v28;
        t[29] = v29;
        t[30] = v30;
        t[31] = v31;
        t[32] = v32;
        t[33] = v33;
        t[34] = v34;
        t[35] = v35;
        t[36] = v36;
        t[37] = v37;
        t[38] = v38;
        t[39] = v39;
        t[40] = v40;
        t[41] = v41;
        t[42] = v42;
        t[43] = v43;
        t[44] = v44;
        t[45] = v45;
        t[46] = v46;
        t[47] = v47;
        t[48] = v48;
        t[49] = v49;
        t[50] = v50;
        t[51] = v51;
        t[52] = v52;
        t[53] = v53;
        t[54] = v54;
        t[55] = v55;
        t[56] = v56;
        t[57] = v57;
        t[58] = v58;
        t[59] = v59;
        t[60] = v60;
        t[61] = v61;
        t[62] = v62;
        t[63] = v63;
    }

    public static void Adst8(Span<int> t, int lo, int hi)
    {
        _ = t[7];
        int v0 = t[7];
        int v1 = t[0];
        int v2 = t[5];
        int v3 = t[2];
        int v4 = t[3];
        int v5 = t[4];
        int v6 = t[1];
        int v7 = t[6];
        { long x = v0, y = v1; v1 = (int)((x * 401 - y * 4076 + 2048) >> 12); v0 = (int)((x * 4076 + y * 401 + 2048) >> 12); }
        { long x = v2, y = v3; v3 = (int)((x * 1931 - y * 3612 + 2048) >> 12); v2 = (int)((x * 3612 + y * 1931 + 2048) >> 12); }
        { long x = v4, y = v5; v5 = (int)((x * 3166 - y * 2598 + 2048) >> 12); v4 = (int)((x * 2598 + y * 3166 + 2048) >> 12); }
        { long x = v6, y = v7; v7 = (int)((x * 3920 - y * 1189 + 2048) >> 12); v6 = (int)((x * 1189 + y * 3920 + 2048) >> 12); }
        { int x = v0, y = v4; v0 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v5; v1 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v6; v2 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v7; v3 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { long x = v4, y = v5; v5 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v4 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v7, y = v6; v6 = (int)((x * 3784 - y * 1567 + 2048) >> 12); v7 = (int)((x * 1567 + y * 3784 + 2048) >> 12); }
        { int x = v0, y = v2; v0 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v6; v4 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v3; v1 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v7; v5 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { long x = v2, y = v3; v3 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v2 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v6, y = v7; v7 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        t[0] = v0;
        t[1] = -v4;
        t[2] = v6;
        t[3] = -v2;
        t[4] = v3;
        t[5] = -v7;
        t[6] = v5;
        t[7] = -v1;
    }

    public static void Adst16(Span<int> t, int lo, int hi)
    {
        _ = t[15];
        int v0 = t[15];
        int v1 = t[0];
        int v2 = t[13];
        int v3 = t[2];
        int v4 = t[11];
        int v5 = t[4];
        int v6 = t[9];
        int v7 = t[6];
        int v8 = t[7];
        int v9 = t[8];
        int v10 = t[5];
        int v11 = t[10];
        int v12 = t[3];
        int v13 = t[12];
        int v14 = t[1];
        int v15 = t[14];
        { long x = v0, y = v1; v1 = (int)((x * 201 - y * 4091 + 2048) >> 12); v0 = (int)((x * 4091 + y * 201 + 2048) >> 12); }
        { long x = v2, y = v3; v3 = (int)((x * 995 - y * 3973 + 2048) >> 12); v2 = (int)((x * 3973 + y * 995 + 2048) >> 12); }
        { long x = v4, y = v5; v5 = (int)((x * 1751 - y * 3703 + 2048) >> 12); v4 = (int)((x * 3703 + y * 1751 + 2048) >> 12); }
        { long x = v6, y = v7; v7 = (int)((x * 2440 - y * 3290 + 2048) >> 12); v6 = (int)((x * 3290 + y * 2440 + 2048) >> 12); }
        { long x = v8, y = v9; v9 = (int)((x * 3035 - y * 2751 + 2048) >> 12); v8 = (int)((x * 2751 + y * 3035 + 2048) >> 12); }
        { long x = v10, y = v11; v11 = (int)((x * 3513 - y * 2106 + 2048) >> 12); v10 = (int)((x * 2106 + y * 3513 + 2048) >> 12); }
        { long x = v12, y = v13; v13 = (int)((x * 3857 - y * 1380 + 2048) >> 12); v12 = (int)((x * 1380 + y * 3857 + 2048) >> 12); }
        { long x = v14, y = v15; v15 = (int)((x * 4052 - y * 601 + 2048) >> 12); v14 = (int)((x * 601 + y * 4052 + 2048) >> 12); }
        { int x = v0, y = v8; v0 = Clamp(x + y, lo, hi); v8 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v9; v1 = Clamp(x + y, lo, hi); v9 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v10; v2 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v11; v3 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v12; v4 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v13; v5 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v6, y = v14; v6 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v7, y = v15; v7 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { long x = v8, y = v9; v9 = (int)((x * 799 - y * 4017 + 2048) >> 12); v8 = (int)((x * 4017 + y * 799 + 2048) >> 12); }
        { long x = v13, y = v12; v12 = (int)((x * 4017 - y * 799 + 2048) >> 12); v13 = (int)((x * 799 + y * 4017 + 2048) >> 12); }
        { long x = v10, y = v11; v11 = (int)((x * 3406 - y * 2276 + 2048) >> 12); v10 = (int)((x * 2276 + y * 3406 + 2048) >> 12); }
        { long x = v15, y = v14; v14 = (int)((x * 2276 - y * 3406 + 2048) >> 12); v15 = (int)((x * 3406 + y * 2276 + 2048) >> 12); }
        { int x = v0, y = v4; v0 = Clamp(x + y, lo, hi); v4 = Clamp(x - y, lo, hi); }
        { int x = v8, y = v12; v8 = Clamp(x + y, lo, hi); v12 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v5; v1 = Clamp(x + y, lo, hi); v5 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v13; v9 = Clamp(x + y, lo, hi); v13 = Clamp(x - y, lo, hi); }
        { int x = v2, y = v6; v2 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v10, y = v14; v10 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v3, y = v7; v3 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v11, y = v15; v11 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { long x = v4, y = v5; v5 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v4 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v12, y = v13; v13 = (int)((x * 1567 - y * 3784 + 2048) >> 12); v12 = (int)((x * 3784 + y * 1567 + 2048) >> 12); }
        { long x = v7, y = v6; v6 = (int)((x * 3784 - y * 1567 + 2048) >> 12); v7 = (int)((x * 1567 + y * 3784 + 2048) >> 12); }
        { long x = v15, y = v14; v14 = (int)((x * 3784 - y * 1567 + 2048) >> 12); v15 = (int)((x * 1567 + y * 3784 + 2048) >> 12); }
        { int x = v0, y = v2; v0 = Clamp(x + y, lo, hi); v2 = Clamp(x - y, lo, hi); }
        { int x = v4, y = v6; v4 = Clamp(x + y, lo, hi); v6 = Clamp(x - y, lo, hi); }
        { int x = v8, y = v10; v8 = Clamp(x + y, lo, hi); v10 = Clamp(x - y, lo, hi); }
        { int x = v12, y = v14; v12 = Clamp(x + y, lo, hi); v14 = Clamp(x - y, lo, hi); }
        { int x = v1, y = v3; v1 = Clamp(x + y, lo, hi); v3 = Clamp(x - y, lo, hi); }
        { int x = v5, y = v7; v5 = Clamp(x + y, lo, hi); v7 = Clamp(x - y, lo, hi); }
        { int x = v9, y = v11; v9 = Clamp(x + y, lo, hi); v11 = Clamp(x - y, lo, hi); }
        { int x = v13, y = v15; v13 = Clamp(x + y, lo, hi); v15 = Clamp(x - y, lo, hi); }
        { long x = v2, y = v3; v3 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v2 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v6, y = v7; v7 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v6 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v10, y = v11; v11 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v10 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        { long x = v14, y = v15; v15 = (int)((x * 2896 - y * 2896 + 2048) >> 12); v14 = (int)((x * 2896 + y * 2896 + 2048) >> 12); }
        t[0] = v0;
        t[1] = -v8;
        t[2] = v12;
        t[3] = -v4;
        t[4] = v6;
        t[5] = -v14;
        t[6] = v10;
        t[7] = -v2;
        t[8] = v3;
        t[9] = -v11;
        t[10] = v15;
        t[11] = -v7;
        t[12] = v5;
        t[13] = -v13;
        t[14] = v9;
        t[15] = -v1;
    }
}
