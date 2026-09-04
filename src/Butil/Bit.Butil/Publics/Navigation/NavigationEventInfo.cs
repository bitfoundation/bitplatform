namespace Bit.Butil;

/// <summary>What happened, delivered to a <see cref="Navigation"/> subscription.</summary>
/// <param name="From">
/// For <see cref="Navigation.SubscribeCurrentEntryChange"/>, the entry that was current before the
/// change; null for the other events and for a change with no previous entry.
/// </param>
/// <param name="NavigationType">
/// <c>push</c>, <c>replace</c>, <c>reload</c> or <c>traverse</c> - null when the change was an
/// in-place state update rather than a navigation.
/// </param>
/// <param name="Message">The failure message, on <see cref="Navigation.SubscribeNavigateError"/> only.</param>
public record NavigationEventInfo(
    NavigationEntry? From,
    string? NavigationType,
    string? Message);
