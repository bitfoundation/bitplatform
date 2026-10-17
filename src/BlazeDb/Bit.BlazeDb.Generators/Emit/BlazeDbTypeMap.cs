using Microsoft.CodeAnalysis;

namespace Bit.BlazeDb.Generators;

/// <summary>Maps C# types to scalar kinds and their wire encodings.</summary>
internal static class BlazeDbTypeMap
{
    private static readonly SymbolDisplayFormat FullFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static string Display(ITypeSymbol type) => type.ToDisplayString(FullFormat);

    public static BlazeDbScalarKind? GetScalarKind(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            return BlazeDbScalarKind.Enum;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean: return BlazeDbScalarKind.Bool;
            case SpecialType.System_SByte: return BlazeDbScalarKind.SByte;
            case SpecialType.System_Byte: return BlazeDbScalarKind.Byte;
            case SpecialType.System_Int16: return BlazeDbScalarKind.Int16;
            case SpecialType.System_UInt16: return BlazeDbScalarKind.UInt16;
            case SpecialType.System_Int32: return BlazeDbScalarKind.Int32;
            case SpecialType.System_UInt32: return BlazeDbScalarKind.UInt32;
            case SpecialType.System_Int64: return BlazeDbScalarKind.Int64;
            case SpecialType.System_UInt64: return BlazeDbScalarKind.UInt64;
            case SpecialType.System_Char: return BlazeDbScalarKind.Char;
            case SpecialType.System_Single: return BlazeDbScalarKind.Single;
            case SpecialType.System_Double: return BlazeDbScalarKind.Double;
            case SpecialType.System_Decimal: return BlazeDbScalarKind.Decimal;
            case SpecialType.System_String: return BlazeDbScalarKind.String;
            case SpecialType.System_DateTime: return BlazeDbScalarKind.DateTime;
        }

        if (type is IArrayTypeSymbol array && array.ElementType.SpecialType == SpecialType.System_Byte)
        {
            return BlazeDbScalarKind.ByteArray;
        }

        return type.ToDisplayString() switch
        {
            "System.Guid" => BlazeDbScalarKind.Guid,
            "System.DateTimeOffset" => BlazeDbScalarKind.DateTimeOffset,
            "System.TimeSpan" => BlazeDbScalarKind.TimeSpan,
            _ => null,
        };
    }

    public static bool IsReferenceScalar(BlazeDbScalarKind kind) =>
        kind == BlazeDbScalarKind.String || kind == BlazeDbScalarKind.ByteArray;

    public static string WireType(BlazeDbScalarKind kind)
    {
        switch (kind)
        {
            case BlazeDbScalarKind.Single:
                return "global::Bit.BlazeDb.Serialization.BlazeDbWireType.Fixed32";
            case BlazeDbScalarKind.Double:
            case BlazeDbScalarKind.DateTime:
            case BlazeDbScalarKind.TimeSpan:
                return "global::Bit.BlazeDb.Serialization.BlazeDbWireType.Fixed64";
            case BlazeDbScalarKind.Decimal:
            case BlazeDbScalarKind.String:
            case BlazeDbScalarKind.ByteArray:
            case BlazeDbScalarKind.Guid:
            case BlazeDbScalarKind.DateTimeOffset:
                return "global::Bit.BlazeDb.Serialization.BlazeDbWireType.LengthDelimited";
            default:
                return "global::Bit.BlazeDb.Serialization.BlazeDbWireType.VarInt";
        }
    }

    /// <summary>Expression writing a non-null scalar value.</summary>
    public static string WriteExpr(BlazeDbScalarKind kind, string writer, string value)
    {
        switch (kind)
        {
            case BlazeDbScalarKind.Bool: return $"{writer}.WriteBool({value})";
            case BlazeDbScalarKind.SByte:
            case BlazeDbScalarKind.Int16:
            case BlazeDbScalarKind.Int32:
            case BlazeDbScalarKind.Int64: return $"{writer}.WriteVarInt({value})";
            case BlazeDbScalarKind.Byte:
            case BlazeDbScalarKind.UInt16:
            case BlazeDbScalarKind.UInt32:
            case BlazeDbScalarKind.UInt64:
            case BlazeDbScalarKind.Char: return $"{writer}.WriteVarUInt({value})";
            case BlazeDbScalarKind.Single: return $"{writer}.WriteSingle({value})";
            case BlazeDbScalarKind.Double: return $"{writer}.WriteDouble({value})";
            case BlazeDbScalarKind.Decimal: return $"{writer}.WriteDecimal({value})";
            case BlazeDbScalarKind.String: return $"{writer}.WriteString({value})";
            case BlazeDbScalarKind.ByteArray: return $"{writer}.WriteBytes({value})";
            case BlazeDbScalarKind.Guid: return $"{writer}.WriteGuid({value})";
            case BlazeDbScalarKind.DateTime: return $"{writer}.WriteDateTime({value})";
            case BlazeDbScalarKind.DateTimeOffset: return $"{writer}.WriteDateTimeOffset({value})";
            case BlazeDbScalarKind.TimeSpan: return $"{writer}.WriteTimeSpan({value})";
            case BlazeDbScalarKind.Enum: return $"{writer}.WriteVarInt((long){value})";
            default: return "";
        }
    }

    /// <summary>Expression reading a scalar value. <paramref name="scalarTypeDisplay"/> is used for casts.</summary>
    public static string ReadExpr(BlazeDbScalarKind kind, string reader, string scalarTypeDisplay)
    {
        switch (kind)
        {
            case BlazeDbScalarKind.Bool: return $"{reader}.ReadBool()";
            case BlazeDbScalarKind.SByte: return $"(sbyte){reader}.ReadVarInt()";
            case BlazeDbScalarKind.Int16: return $"(short){reader}.ReadVarInt()";
            case BlazeDbScalarKind.Int32: return $"(int){reader}.ReadVarInt()";
            case BlazeDbScalarKind.Int64: return $"{reader}.ReadVarInt()";
            case BlazeDbScalarKind.Byte: return $"(byte){reader}.ReadVarUInt()";
            case BlazeDbScalarKind.UInt16: return $"(ushort){reader}.ReadVarUInt()";
            case BlazeDbScalarKind.UInt32: return $"(uint){reader}.ReadVarUInt()";
            case BlazeDbScalarKind.UInt64: return $"{reader}.ReadVarUInt()";
            case BlazeDbScalarKind.Char: return $"(char){reader}.ReadVarUInt()";
            case BlazeDbScalarKind.Single: return $"{reader}.ReadSingle()";
            case BlazeDbScalarKind.Double: return $"{reader}.ReadDouble()";
            case BlazeDbScalarKind.Decimal: return $"{reader}.ReadDecimal()";
            case BlazeDbScalarKind.String: return $"{reader}.ReadString()";
            case BlazeDbScalarKind.ByteArray: return $"{reader}.ReadBytes().ToArray()";
            case BlazeDbScalarKind.Guid: return $"{reader}.ReadGuid()";
            case BlazeDbScalarKind.DateTime: return $"{reader}.ReadDateTime()";
            case BlazeDbScalarKind.DateTimeOffset: return $"{reader}.ReadDateTimeOffset()";
            case BlazeDbScalarKind.TimeSpan: return $"{reader}.ReadTimeSpan()";
            case BlazeDbScalarKind.Enum: return $"({scalarTypeDisplay}){reader}.ReadVarInt()";
            default: return "";
        }
    }

    /// <summary>Default value expression for read locals of non-nullable scalars.</summary>
    public static string DefaultExpr(BlazeDbScalarKind kind, string scalarTypeDisplay)
    {
        switch (kind)
        {
            case BlazeDbScalarKind.String: return "\"\"";
            case BlazeDbScalarKind.ByteArray: return "global::System.Array.Empty<byte>()";
            default: return "default";
        }
    }
}
