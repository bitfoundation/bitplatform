namespace Bit.SourceGenerators;

internal readonly record struct BitProperty(
    string ContainingTypeFullName,
    string ClassName,
    string ClassNameForCode,
    string ClassNamespace,
    bool IsBaseTypeComponentBase,
    string PropertyName,
    string PropertyType,
    // a cast of null is null for anything but a struct, so only a struct needs the branch around it
    bool NeedsNullGuard);
