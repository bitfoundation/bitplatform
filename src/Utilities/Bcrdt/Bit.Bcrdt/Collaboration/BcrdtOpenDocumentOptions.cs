namespace Bit.Bcrdt;

/// <summary>Per-document overrides applied when opening.</summary>
public sealed class BcrdtOpenDocumentOptions
{
    /// <summary>Whether to connect immediately. Falls back to the service-wide setting.</summary>
    public bool? Connect { get; init; }

    /// <summary>Whether to load from and write to the store. Falls back to the service-wide setting.</summary>
    public bool? Persist { get; init; }

    /// <summary>Who the local participant is, published as soon as the document opens.</summary>
    public BcrdtCollaboratorInfo? User { get; init; }

    /// <summary>An explicit client identifier, for tests that need reproducibility.</summary>
    public ulong? ClientId { get; init; }
}
