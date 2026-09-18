//+:cnd:noEmit
using System.Collections.Frozen;
using Boilerplate.Shared.Infrastructure.Services;

namespace Boilerplate.Shared.Features.Identity.OAuth;

/// <summary>
/// A scope is a named subset of <see cref="AppFeatures"/> and never more. A token carries its scopes' features
/// intersected with the user's own, so adding a feature here is the only way to widen what an external app can do.
/// </summary>
public static class OAuthScopes
{
    /// <summary>Read-only inspection of a running deployment through <c>/dev-mcp</c>.</summary>
    public const string DevMcp = "dev-mcp";

    //#if (signalR == true)
    /// <summary>
    /// The chatbot's own tools at <c>/mcp</c>. Only the server-side ones are exposed there, so this scope cannot reach
    /// the user's device however it is spent.
    /// </summary>
    public const string Chat = "chat";
    //#endif

    private static readonly FrozenDictionary<string, string[]> featuresByScope = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        //#if (signalR == true)
        // No feature: the chatbot is open to every signed-in user, and NarrowScopes grants a scope whose features are
        // all held - which an empty set vacuously is. Consent is then the only thing standing in front of it.
        [Chat] = [],
        //#endif
        [DevMcp] = [AppFeatures.System.DevMcp]
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Published as <c>scopes_supported</c> in the authorization server metadata.</summary>
    public static string[] All { get; } = [.. featuresByScope.Keys.Order(StringComparer.Ordinal)];

    public static bool IsKnown(string scope) => featuresByScope.ContainsKey(scope);

    /// <summary>
    /// RFC 6749: space delimited, order insignificant. An unknown scope is dropped rather than rejected -
    /// <c>invalid_scope</c> is for a request that asks for nothing valid at all.
    /// </summary>
    public static string[] Parse(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return [];

        return [.. scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(IsKnown)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)];
    }

    /// <summary>What the scopes permit. Callers must still intersect this with what the user actually holds.</summary>
    public static string[] FeaturesFor(IEnumerable<string> scopes)
    {
        return [.. scopes.Where(IsKnown).SelectMany(scope => featuresByScope[scope]).Distinct(StringComparer.Ordinal)];
    }
}
