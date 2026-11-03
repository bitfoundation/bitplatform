using System.Collections.Concurrent;

namespace Boilerplate.LandingPage.Server.Features.Waitlist;

public sealed record WaitlistEntry
{
    public required string Site { get; init; }

    public required string Email { get; init; }

    public string? Name { get; init; }

    public string[] Platforms { get; init; } = [];

    public bool WantsUpdates { get; init; }

    public required string Culture { get; init; }

    public string? Source { get; init; }

    public string? Medium { get; init; }

    public string? Campaign { get; init; }

    public string? Referrer { get; init; }

    public required string Token { get; init; }

    public int Position { get; init; }

    public DateTimeOffset JoinedOn { get; init; }

    public DateTimeOffset? ConfirmationSentOn { get; init; }

    public DateTimeOffset? ConfirmedOn { get; init; }

    public DateTimeOffset? UnsubscribedOn { get; init; }
}

public interface IWaitlistStore
{
    Task<(WaitlistEntry Entry, bool AlreadyJoined)> AddAsync(WaitlistEntry entry, CancellationToken cancellationToken);

    Task<WaitlistEntry?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken);
}

// The waitlist is kept in memory, so it is gone on every restart. Keep it wherever suits you instead: a table in
// your database, an endpoint of your API server, or your mailing list service. Implement IWaitlistStore and register it.
public sealed class InMemoryWaitlistStore : IWaitlistStore
{
    private readonly ConcurrentDictionary<string, WaitlistEntry> entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

    public Task<(WaitlistEntry Entry, bool AlreadyJoined)> AddAsync(WaitlistEntry entry, CancellationToken cancellationToken)
    {
        var key = $"{entry.Site}:{entry.Email}";

        if (entries.TryGetValue(key, out var existing))
            return Task.FromResult((existing, true));

        var added = entries.GetOrAdd(key, _ => entry with { Position = counts.AddOrUpdate(entry.Site, 1, (_, count) => count + 1) });

        // When two sign-ups race only one entry is stored, and the other learns from its token that it already joined
        return Task.FromResult((added, added.Token != entry.Token));
    }

    public Task<WaitlistEntry?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(entries.Values.FirstOrDefault(e => e.Token == token));
    }

    public Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken)
    {
        entries[$"{entry.Site}:{entry.Email}"] = entry;
        return Task.CompletedTask;
    }
}
