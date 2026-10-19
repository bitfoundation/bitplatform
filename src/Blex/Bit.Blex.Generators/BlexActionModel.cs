namespace Bit.Blex.Generators;

internal sealed record BlexActionModel(
    string ImplName,
    string WrapperName,
    string ActionLabel,
    bool IsAsync,
    bool IsValueTask,
    BlexEquatableArray<BlexParamModel> Parameters) : System.IEquatable<BlexActionModel>;
