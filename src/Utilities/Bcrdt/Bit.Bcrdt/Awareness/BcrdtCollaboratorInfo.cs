namespace Bit.Bcrdt;

/// <summary>Who a participant is, for display purposes.</summary>
/// <param name="Id">The application's identifier for the person. Distinct from a client session id.</param>
/// <param name="Name">The name to show.</param>
/// <param name="Color">
/// A CSS colour used for their cursor and selection highlight. For participants read through
/// <see cref="BcrdtPresenceService"/> this is always a hex colour (<c>#rgb</c>, <c>#rgba</c>,
/// <c>#rrggbb</c> or <c>#rrggbbaa</c>): anything else a peer publishes is replaced with a suggested
/// colour, so the value is safe to place in a style attribute.
/// </param>
/// <param name="AvatarUrl">An optional avatar image.</param>
public sealed record BcrdtCollaboratorInfo(string Id, string Name, string Color, string? AvatarUrl = null);
