namespace Bit.Belemetry;

/// <summary>What a bundle holds of one section, for the preview and the manifest.</summary>
/// <param name="Section">The section.</param>
/// <param name="Status"><c>included</c>, <c>excluded</c> (by the user or the app), <c>empty</c>, <c>unavailable</c> or <c>truncated</c>.</param>
/// <param name="Items">How many items it holds.</param>
/// <param name="Omitted">How many items were left out of it (limits, consent, exclusions).</param>
/// <param name="Note">Why, when it is not simply included.</param>
public sealed record BelemetryBundleSection(BelemetryBundleSections Section, string Status, int Items, int Omitted, string? Note);
