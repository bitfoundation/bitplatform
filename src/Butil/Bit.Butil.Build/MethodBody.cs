using System;

namespace Bit.Butil.Build;

/// <summary>
/// Walks a method's IL and hands back the metadata tokens its instructions carry - the string literals it
/// loads and the types, fields and methods it names.
/// </summary>
/// <remarks>
/// A real instruction walk rather than a scan for byte patterns, because the byte that means <c>ldstr</c>
/// also occurs inside other instructions' operands: a pattern match would invent calls into modules the
/// method never mentions, and every one of those is JavaScript a consumer ships for nothing. Walking means
/// knowing how long each instruction is, which is the table below (ECMA-335 III, Partition VI Appendix C).
/// </remarks>
public static class MethodBody
{
    /// <summary>Operand length in bytes; <see cref="Switch"/> for the one instruction of variable length.</summary>
    private const int Switch = -1;

    private static readonly int[] OneByteOperand = new int[256];

    private static readonly bool[] OneByteToken = new bool[256];

    private static readonly int[] TwoByteOperand = new int[32];

    private static readonly bool[] TwoByteToken = new bool[32];

    static MethodBody()
    {
        // Everything not named below has no operand at all, which is the array's default.
        foreach (var opcode in new[]
        {
            0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,                                     // ldarg.s .. stloc.s
            0x1F,                                                                   // ldc.i4.s
            0x2B, 0x2C, 0x2D,                                                       // br.s, brfalse.s, brtrue.s
            0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,             // beq.s .. blt.un.s
            0xDE,                                                                   // leave.s
        }) OneByteOperand[opcode] = 1;

        foreach (var opcode in new[]
        {
            0x20, 0x22,                                                             // ldc.i4, ldc.r4
            0x38, 0x39, 0x3A,                                                       // br, brfalse, brtrue
            0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44,             // beq .. blt.un
            0xDD,                                                                   // leave
        }) OneByteOperand[opcode] = 4;

        OneByteOperand[0x21] = 8; // ldc.i8
        OneByteOperand[0x23] = 8; // ldc.r8
        OneByteOperand[0x45] = Switch;

        foreach (var opcode in new[]
        {
            0x27, 0x28, 0x29,                                                       // jmp, call, calli
            0x6F,                                                                   // callvirt
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75,                                     // cpobj, ldobj, ldstr, newobj, castclass, isinst
            0x79,                                                                   // unbox
            0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81,                               // ldfld .. stobj
            0x8C, 0x8D, 0x8F,                                                       // box, newarr, ldelema
            0xA3, 0xA4, 0xA5,                                                       // ldelem, stelem, unbox.any
            0xC2, 0xC6,                                                             // refanyval, mkrefany
            0xD0,                                                                   // ldtoken
        })
        {
            OneByteOperand[opcode] = 4;
            OneByteToken[opcode] = true;
        }

        foreach (var opcode in new[] { 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }) TwoByteOperand[opcode] = 2; // ldarg .. stloc
        foreach (var opcode in new[] { 0x12, 0x19 }) TwoByteOperand[opcode] = 1;                          // unaligned., no.

        foreach (var opcode in new[] { 0x06, 0x07, 0x15, 0x16, 0x1C })                                    // ldftn, ldvirtftn, initobj, constrained., sizeof
        {
            TwoByteOperand[opcode] = 4;
            TwoByteToken[opcode] = true;
        }
    }

    /// <summary>
    /// Calls back once per metadata token the body carries. A <c>ldstr</c> comes through with table
    /// <c>0x70</c>, whose "row" is a byte offset into the <c>#US</c> heap rather than a row number - that is
    /// how string tokens are encoded, and the caller reads it back through
    /// <see cref="UserStringHeap.ReadAt(PeImage, int)"/>.
    /// </summary>
    /// <param name="image">The assembly the body lives in.</param>
    /// <param name="rva">The method's RVA, from <see cref="MetadataTables.MethodRva"/>. Zero means no body.</param>
    /// <param name="onToken">Receives the token's table byte and its row (or heap offset).</param>
    public static void ReadTokens(PeImage image, uint rva, Action<int, int> onToken)
    {
        if (rva == 0) return;

        var body = image.ResolveRva(rva);
        var header = image.ReadByte(body);

        int code;
        int size;
        switch (header & 0x03)
        {
            case 0x02: // tiny: the length lives in the top six bits of the one header byte
                code = body + 1;
                size = header >> 2;
                break;

            case 0x03: // fat: a header whose own length is in the top four bits of its first word
                var flags = image.ReadUInt16(body);
                var headerSize = (flags >> 12) * 4;
                if (headerSize < 12) throw image.Invalid("a fat method header shorter than the format allows");
                code = body + headerSize;
                size = image.ReadInt32(body + 4);
                break;

            default:
                throw image.Invalid("a method body with neither a tiny nor a fat header");
        }

        if (size < 0 || size > image.Image.Length - code) throw image.Invalid("a method body that runs past the end of the file");

        var end = code + size;
        var position = code;

        while (position < end)
        {
            var opcode = image.ReadByte(position);
            position++;

            int operand;
            bool carriesToken;

            if (opcode == 0xFE)
            {
                var second = image.ReadByte(position);
                position++;

                // Anything past the two-byte opcodes the table covers is not a real instruction; stopping is
                // safer than guessing a length and walking the rest of the body out of step.
                if (second >= TwoByteOperand.Length) return;

                operand = TwoByteOperand[second];
                carriesToken = TwoByteToken[second];
            }
            else
            {
                operand = OneByteOperand[opcode];
                carriesToken = OneByteToken[opcode];
            }

            if (operand == Switch)
            {
                var cases = image.ReadInt32(position);
                position += 4;
                if (cases < 0 || cases > (end - position) / 4) return;

                position += cases * 4;
                continue;
            }

            if (position + operand > end) return;

            if (carriesToken)
            {
                var token = image.ReadUInt32(position);
                onToken((int)(token >> 24), (int)(token & 0x00FFFFFF));
            }

            position += operand;
        }
    }
}
