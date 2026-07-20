using System.Collections.Concurrent;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>The identity e-mail that <see cref="TestIdentityEmailService"/> captured.</summary>
public enum CapturedEmailKind { ResetPassword, Otp, TwoFactor, EmailToken, ElevatedAccess, TenantInvitation }

/// <summary>
/// A single e-mail captured in tests. It holds the values a test actually needs (the token and the link) directly, taken
/// straight from the e-mail method's arguments - so there is no rendered body to parse.
/// </summary>
public sealed record CapturedEmail
{
    public required CapturedEmailKind Kind { get; init; }
    public required string ToEmailAddress { get; init; }
    public string? Token { get; init; }
    public Uri? Link { get; init; }

    /// <summary>True when this e-mail was addressed to <paramref name="email"/> (case-insensitive).</summary>
    public bool IsTo(string email) => string.Equals(ToEmailAddress, email, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A per-<see cref="AppTestServer"/> singleton that holds every e-mail <see cref="TestIdentityEmailService"/> captured,
/// letting a test read back the confirmation link / OTP / elevated-access token straight from the message. Each test owns
/// its own server (and therefore its own store), so captures are already isolated between parallel tests.
/// </summary>
public sealed class EmailCaptureStore
{
    private readonly ConcurrentQueue<CapturedEmail> captured = [];

    /// <summary>Every e-mail captured so far, oldest first.</summary>
    public IReadOnlyCollection<CapturedEmail> Captured => captured;

    public void Add(CapturedEmail email) => captured.Enqueue(email);
}
