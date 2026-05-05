namespace Bit.SourceGenerators;

internal readonly record struct BitProperty(
    string ContainingTypeFullName,
    string ClassName,
    string ClassNameForCode,
    string ClassNamespace,
    bool IsBaseTypeComponentBase,
    string PropertyName,
    string PropertyType);
