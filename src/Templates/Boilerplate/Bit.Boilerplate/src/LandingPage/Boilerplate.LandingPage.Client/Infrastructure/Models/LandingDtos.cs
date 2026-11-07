namespace Boilerplate.LandingPage.Client.Infrastructure.Models;

public sealed record LandingChatTurn(bool FromVisitor, string Text);

public sealed record LandingChatAsk(string Site, string Culture, LandingChatTurn[] Turns);

public sealed record LandingChatFrame(string? Delta = null, bool Done = false, string? Error = null, bool Retryable = false);

public sealed record WaitlistJoinRequest
{
    public string Site { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string[] Platforms { get; init; } = [];

    public bool WantsUpdates { get; init; }

    public string Culture { get; init; } = string.Empty;

    public string? Source { get; init; }

    public string? Medium { get; init; }

    public string? Campaign { get; init; }

    public string? Referrer { get; init; }

    public string? Website { get; init; }

    public long ElapsedMilliseconds { get; init; }
}

public sealed record WaitlistJoinResult(int Position, bool AlreadyJoined, bool ConfirmationSent);

public sealed record WaitlistTokenRequest(string Token);

public sealed record WaitlistConfirmResult(bool Confirmed, int Position);

public sealed record StoreListing
{
    public string? Name { get; init; }

    public double? AverageRating { get; init; }

    public int? RatingCount { get; init; }

    public long? SizeInBytes { get; init; }

    public string? MinimumOsVersion { get; init; }

    public string? Url { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LandingChatAsk))]
[JsonSerializable(typeof(LandingChatFrame))]
[JsonSerializable(typeof(WaitlistJoinRequest))]
[JsonSerializable(typeof(WaitlistJoinResult))]
[JsonSerializable(typeof(WaitlistTokenRequest))]
[JsonSerializable(typeof(WaitlistConfirmResult))]
[JsonSerializable(typeof(StoreListing))]
public partial class LandingJsonContext : JsonSerializerContext
{
}
