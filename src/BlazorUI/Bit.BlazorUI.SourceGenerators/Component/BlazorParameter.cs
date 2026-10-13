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
    // A cast of null is null for anything but a non-nullable value type, so only such a type needs the branch around it.
    bool NeedsNullGuard,
    bool ResetClassBuilder,
    bool ResetStyleBuilder,
    bool IsTwoWayBound,
    string? CallOnSetMethodName,
    string? CallOnSetAsyncMethodName,
    bool TracksSetByMarkup,
    // Whether the class sees the internal TakeParametersDictionary of BitComponentBase, and so hands its leftovers on
    // through ParametersCache rather than through a view built from them.
    bool TakesParametersDictionary,
    bool IsCascadedParams);
