using System;
using System.Reflection.Metadata;

namespace Bit.BlazorUI.Build;

/// <summary>Walks a method body's IL for the metadata tokens and the string literals its instructions name.</summary>
internal static class ILScanner
{
    public static void Scan(ref BlobReader il, Action<int> onToken, Action<int> onString)
    {
        while (il.RemainingBytes > 0)
        {
            int opCode = il.ReadByte();
            if (opCode == 0xFE)
            {
                opCode = 0xFE00 | il.ReadByte();
            }

            switch (OperandOf(opCode))
            {
                case Operand.None:
                    break;
                case Operand.Int8:
                    il.Offset += 1;
                    break;
                case Operand.Int16:
                    il.Offset += 2;
                    break;
                case Operand.Int32:
                    il.Offset += 4;
                    break;
                case Operand.Int64:
                    il.Offset += 8;
                    break;
                case Operand.Token:
                    onToken(il.ReadInt32());
                    break;
                case Operand.String:
                    onString(il.ReadInt32());
                    break;
                case Operand.Switch:
                    var count = il.ReadInt32();
                    il.Offset += count * 4;
                    break;
            }
        }
    }

    private enum Operand { None, Int8, Int16, Int32, Int64, Token, String, Switch }

    // ECMA-335 III, by the size and kind of each instruction's inline operand.
    private static Operand OperandOf(int opCode)
    {
        switch (opCode)
        {
            // ldarg.s, ldarga.s, starg.s, ldloc.s, ldloca.s, stloc.s, ldc.i4.s, and the short branches br.s .. blt.un.s, leave.s
            case 0x0E: case 0x0F: case 0x10: case 0x11: case 0x12: case 0x13: case 0x1F:
            case >= 0x2B and <= 0x37:
            case 0xDE:
            // unaligned., no.
            case 0xFE12: case 0xFE19:
                return Operand.Int8;

            // ldarg, ldarga, starg, ldloc, ldloca, stloc
            case 0xFE09: case 0xFE0A: case 0xFE0B: case 0xFE0C: case 0xFE0D: case 0xFE0E:
                return Operand.Int16;

            // ldc.i4, ldc.r4, the long branches br .. blt.un, leave
            case 0x20: case 0x22:
            case >= 0x38 and <= 0x44:
            case 0xDD:
                return Operand.Int32;

            // ldc.i8, ldc.r8
            case 0x21: case 0x23:
                return Operand.Int64;

            case 0x45:
                return Operand.Switch;

            case 0x72:
                return Operand.String;

            // jmp, call, calli, callvirt, cpobj, ldobj, newobj, castclass, isinst, unbox, ldfld, ldflda, stfld, ldsfld,
            // ldsflda, stsfld, stobj, box, newarr, ldelema, ldelem, stelem, unbox.any, refanyval, mkrefany, ldtoken,
            // ldftn, ldvirtftn, initobj, constrained., sizeof
            case 0x27: case 0x28: case 0x29: case 0x6F: case 0x70: case 0x71: case 0x73: case 0x74: case 0x75:
            case 0x79: case 0x7B: case 0x7C: case 0x7D: case 0x7E: case 0x7F: case 0x80: case 0x81: case 0x8C:
            case 0x8D: case 0x8F: case 0xA3: case 0xA4: case 0xA5: case 0xC2: case 0xC6: case 0xD0:
            case 0xFE06: case 0xFE07: case 0xFE15: case 0xFE16: case 0xFE1C:
                return Operand.Token;

            default:
                return Operand.None;
        }
    }
}
