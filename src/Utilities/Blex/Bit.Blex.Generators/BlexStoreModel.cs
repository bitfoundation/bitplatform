namespace Bit.Blex.Generators;

/// <param name="Namespace">The store's namespace, or null for the global namespace.</param>
/// <param name="ClassName">The store class name.</param>
/// <param name="StoreName">The store name (slice key), before any instance key.</param>
/// <param name="Persist">[BlexStore(Persist = true)].</param>
/// <param name="Version">[BlexStore(Version = n)].</param>
/// <param name="EnforceActions">[BlexStore(EnforceActions = true)]: state setters are private.</param>
/// <param name="RollbackOnError">[BlexStore(RollbackOnError = true)]: a throwing synchronous action restores the fields it changed.</param>
/// <param name="EmitActions">Whether the nested Actions class of typed descriptors is emitted (false when the name is taken; BLEX020).</param>
/// <param name="States">The [BlexState] fields.</param>
/// <param name="Computeds">The [BlexComputed] methods.</param>
/// <param name="Actions">The [BlexAction] methods.</param>
/// <param name="Effects">The [BlexEffect] methods.</param>
internal sealed record BlexStoreModel(
    string? Namespace,
    string ClassName,
    string StoreName,
    bool Persist,
    int Version,
    bool EnforceActions,
    bool RollbackOnError,
    bool EmitActions,
    BlexEquatableArray<BlexStateModel> States,
    BlexEquatableArray<BlexComputedModel> Computeds,
    BlexEquatableArray<BlexActionModel> Actions,
    BlexEquatableArray<BlexEffectModel> Effects) : System.IEquatable<BlexStoreModel>;
