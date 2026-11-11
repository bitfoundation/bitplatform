using System.Collections.Concurrent;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>The identity e-mail that <see cref="TestIdentityEmailService"/> captured.</summary>
public enum CapturedEmailKind { ResetPassword, Otp, TwoFactor, EmailToken, ElevatedAccess, TenantInvitation, AccountAlreadyExists }

internal sealed record CapturedEmail
{
    public required CapturedEmailKind Kind { get; init; }
    public required string ToEmailAddress { get; init; }
    public string? Token { get; init; }
    public Uri? Link { get; init; }

    /// <summary>True when this e-mail was addressed to <paramref name="email"/> (case-insensitive).</summary>
    public bool IsTo(string email) => string.Equals(ToEmailAddress, email, StringComparison.OrdinalIgnoreCase);
}

internal sealed class EmailCaptureStore
{
    private readonly ConcurrentQueue<CapturedEmail> captured = [];

    /// <summary>Every e-mail captured so far, oldest first.</summary>
    public IReadOnlyCollection<CapturedEmail> Captured => captured;

    public void Add(CapturedEmail email) => captured.Enqueue(email);
}
