namespace Bit.Bcrdt;

/// <summary>Everything currently known about one participant.</summary>
/// <param name="ClientId">Their session identity.</param>
/// <param name="User">Who they are.</param>
/// <param name="Selection">Where their cursor is, if they have published one.</param>
/// <param name="Activity">What they are doing, for example <c>typing</c>.</param>
/// <param name="IsLocal">Whether this is the local participant.</param>
/// <param name="LastSeen">When we last heard from them.</param>
/// <param name="Custom">Any application-specific fields they published.</param>
public sealed record BcrdtCollaborator(
    ulong ClientId,
    BcrdtCollaboratorInfo User,
    BcrdtCollaboratorSelection? Selection,
    string? Activity,
    bool IsLocal,
    DateTimeOffset LastSeen,
    IReadOnlyDictionary<string, object?> Custom);
