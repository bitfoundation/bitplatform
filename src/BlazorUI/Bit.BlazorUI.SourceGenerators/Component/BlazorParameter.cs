namespace Bit.BlazorUI.SourceGenerators.Component;

internal readonly record struct BlazorParameter(
    string ContainingTypeFullName,
    string ClassName,
    string ClassNameForCode,
    string ClassNamespace,
    bool IsBaseTypeComponentBase,
    bool InheritsFromBitComponentBase,
    string PropertyName,
    string PropertyType,
    bool ResetClassBuilder,
    bool ResetStyleBuilder,
    bool IsTwoWayBound,
    string? CallOnSetMethodName,
    string? CallOnSetAsyncMethodName);
