namespace Bit.Blex.Generators;

internal sealed record BlexStoreModel(
    string? Namespace,
    string ClassName,
    string StoreName,
    bool Persist,
    BlexEquatableArray<BlexStateModel> States,
    BlexEquatableArray<BlexComputedModel> Computeds,
    BlexEquatableArray<BlexActionModel> Actions,
    BlexEquatableArray<BlexEffectModel> Effects) : System.IEquatable<BlexStoreModel>;
