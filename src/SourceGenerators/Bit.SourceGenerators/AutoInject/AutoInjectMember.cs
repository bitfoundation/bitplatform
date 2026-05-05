namespace Bit.SourceGenerators;

internal readonly record struct AutoInjectMember(string Name, string TypeDisplay, bool IsField, bool IsNullable);
