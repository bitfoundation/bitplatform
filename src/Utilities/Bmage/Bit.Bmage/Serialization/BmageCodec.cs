namespace Bit.Bmage;

/// <summary>How one type is written and read.</summary>
internal sealed record BmageCodec(Type Kind, Type Type, string TypeName, Action<BmageJsonWriter, object> Write, Func<BmageJsonReader, object> Read);
