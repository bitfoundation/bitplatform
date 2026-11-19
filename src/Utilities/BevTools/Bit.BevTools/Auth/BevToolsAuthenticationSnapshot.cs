namespace Bit.BevTools;

/// <summary>The authentication state the app's <c>AuthenticationStateProvider</c> reports, as the State panel shows it.</summary>
public sealed class BevToolsAuthenticationSnapshot
{
    public bool IsAuthenticated { get; init; }

    public string? AuthenticationType { get; init; }

    public string? Name { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public int Identities { get; init; }

    public IReadOnlyList<BevToolsClaimView> Claims { get; init; } = [];

    /// <summary>Set when the provider failed to report a state, with what it threw.</summary>
    public string? Error { get; init; }
}
