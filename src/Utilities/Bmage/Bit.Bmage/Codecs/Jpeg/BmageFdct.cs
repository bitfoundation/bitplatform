namespace Bit.Bmage;

/// <summary>Float AAN forward DCT operating in place on an 8×8 block.</summary>
internal static class BmageFdct
{
    public static void Transform(float[] b)
    {
        // Rows
        for (var i = 0; i < 8; i++)
        {
            var o = i * 8;
            var t0 = b[o] + b[o + 7]; var t7 = b[o] - b[o + 7];
            var t1 = b[o + 1] + b[o + 6]; var t6 = b[o + 1] - b[o + 6];
            var t2 = b[o + 2] + b[o + 5]; var t5 = b[o + 2] - b[o + 5];
            var t3 = b[o + 3] + b[o + 4]; var t4 = b[o + 3] - b[o + 4];

            var t10 = t0 + t3; var t13 = t0 - t3;
            var t11 = t1 + t2; var t12 = t1 - t2;
            b[o] = t10 + t11;
            b[o + 4] = t10 - t11;
            var z1 = (t12 + t13) * 0.707106781f;
            b[o + 2] = t13 + z1;
            b[o + 6] = t13 - z1;

            t10 = t4 + t5; t11 = t5 + t6; t12 = t6 + t7;
            var z5 = (t10 - t12) * 0.382683433f;
            var z2 = 0.541196100f * t10 + z5;
            var z4 = 1.306562965f * t12 + z5;
            var z3 = t11 * 0.707106781f;
            var z11 = t7 + z3; var z13 = t7 - z3;
            b[o + 5] = z13 + z2;
            b[o + 3] = z13 - z2;
            b[o + 1] = z11 + z4;
            b[o + 7] = z11 - z4;
        }
        // Columns
        for (var i = 0; i < 8; i++)
        {
            var t0 = b[i] + b[56 + i]; var t7 = b[i] - b[56 + i];
            var t1 = b[8 + i] + b[48 + i]; var t6 = b[8 + i] - b[48 + i];
            var t2 = b[16 + i] + b[40 + i]; var t5 = b[16 + i] - b[40 + i];
            var t3 = b[24 + i] + b[32 + i]; var t4 = b[24 + i] - b[32 + i];

            var t10 = t0 + t3; var t13 = t0 - t3;
            var t11 = t1 + t2; var t12 = t1 - t2;
            b[i] = t10 + t11;
            b[32 + i] = t10 - t11;
            var z1 = (t12 + t13) * 0.707106781f;
            b[16 + i] = t13 + z1;
            b[48 + i] = t13 - z1;

            t10 = t4 + t5; t11 = t5 + t6; t12 = t6 + t7;
            var z5 = (t10 - t12) * 0.382683433f;
            var z2 = 0.541196100f * t10 + z5;
            var z4 = 1.306562965f * t12 + z5;
            var z3 = t11 * 0.707106781f;
            var z11 = t7 + z3; var z13 = t7 - z3;
            b[40 + i] = z13 + z2;
            b[24 + i] = z13 - z2;
            b[8 + i] = z11 + z4;
            b[56 + i] = z11 - z4;
        }
    }
}
