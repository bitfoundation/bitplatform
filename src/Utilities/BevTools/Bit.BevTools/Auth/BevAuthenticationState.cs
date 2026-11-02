using System.Reflection;
using System.Security.Claims;

namespace Bit.BevTools;

/// <summary>One claim of the signed-in user, its type shortened to the name it is known by ("role", not the schema URI).</summary>
public sealed record BevClaimView(string Type, string Value, string? Issuer);

/// <summary>The authentication state the app's <c>AuthenticationStateProvider</c> reports, as the State panel shows it.</summary>
public sealed class BevAuthenticationSnapshot
{
    public bool IsAuthenticated { get; init; }

    public string? AuthenticationType { get; init; }

    public string? Name { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public int Identities { get; init; }

    public IReadOnlyList<BevClaimView> Claims { get; init; } = [];

    /// <summary>Set when the provider failed to report a state, with what it threw.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Exposes the app's authentication state as a built-in state provider: the user, roles and claims, with each change
/// recorded in the State panel and the timeline like any other state change. Found through the scope's
/// <c>AuthenticationStateProvider</c> by name, because Microsoft.AspNetCore.Components.Authorization is the app's
/// dependency and not BevTools'; an app without it simply has no such provider. Claim values go through the same
/// inspection as every other value: credential-shaped values and sensitive claim types are redacted, and
/// <see cref="BitBevToolsOptions.ValueAccess"/> decides whether values are read at all.
/// </summary>
internal sealed class BevAuthenticationStateProvider : IBevStateProvider, IDisposable
{
    private const string ProviderTypeName = "Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, Microsoft.AspNetCore.Components.Authorization";
    private const int MaxClaims = 100;

    private static readonly Type? ProviderType = LoadProviderType();
    private static readonly MethodInfo? GetStateMethod = ProviderType?.GetMethod("GetAuthenticationStateAsync", Type.EmptyTypes);
    private static readonly EventInfo? ChangedEvent = ProviderType?.GetEvent("AuthenticationStateChanged");

    private readonly object _provider;
    private readonly Delegate? _handler;
    private BevAuthenticationSnapshot? _current;
    private int _disposed;

    private BevAuthenticationStateProvider(object provider)
    {
        _provider = provider;
        Description = TypeNames.Short(provider.GetType());
        if (ChangedEvent?.EventHandlerType is { } handlerType)
        {
            try
            {
                // The handler takes Task<AuthenticationState>; a method over Task binds to it by parameter contravariance.
                _handler = Delegate.CreateDelegate(handlerType, this, typeof(BevAuthenticationStateProvider).GetMethod(nameof(OnStateChanged), BindingFlags.Instance | BindingFlags.NonPublic)!);
                ChangedEvent.AddEventHandler(provider, _handler);
            }
            catch
            {
                _handler = null;
            }
        }

        _ = LoadAsync(Invoke(), "Initial state");
    }

    public string Name => "Authentication";

    public string? Description { get; }

    public event Action<BevStateChangeInfo>? Changed;

    public object? GetSnapshot() => _current;

    /// <summary>The provider for the scope, or null when the app has no <c>AuthenticationStateProvider</c>.</summary>
    public static BevAuthenticationStateProvider? TryCreate(IServiceProvider services)
    {
        if (ProviderType is null || GetStateMethod is null)
        {
            return null;
        }

        try
        {
            return services.GetService(ProviderType) is { } provider ? new BevAuthenticationStateProvider(provider) : null;
        }
        catch
        {
            // A provider whose own dependencies cannot be resolved in this scope (a circuit-only service during prerendering).
            return null;
        }
    }

    private static Type? LoadProviderType()
    {
        try
        {
            return Type.GetType(ProviderTypeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    private Task? Invoke()
    {
        try
        {
            // A provider is free to fetch the user from an API when asked; that request is BevTools asking, not the app.
            using var _ = SessionResolver.BeginBevToolsWork();
            return GetStateMethod!.Invoke(_provider, null) as Task;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex is TargetInvocationException { InnerException: { } inner } ? inner : ex);
        }
    }

    private void OnStateChanged(Task task) => _ = LoadAsync(task, "AuthenticationStateChanged");

    private async Task LoadAsync(Task? task, string action)
    {
        BevAuthenticationSnapshot snapshot;
        try
        {
            if (task is null)
            {
                return;
            }

            await task.ConfigureAwait(false);
            var state = task.GetType().GetProperty("Result")?.GetValue(task);
            var user = state?.GetType().GetProperty("User")?.GetValue(state) as ClaimsPrincipal;
            snapshot = Describe(user);
        }
        catch (Exception ex)
        {
            snapshot = new BevAuthenticationSnapshot { Error = ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message) };
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        _current = snapshot;
        // The detail is shown and exported as written, so it names no one: who signed in is a value, and values go
        // through inspection; that someone did is the event.
        var detail = snapshot.Error ?? (snapshot.IsAuthenticated
            ? "authenticated (" + snapshot.AuthenticationType + ", " + snapshot.Claims.Count + " claims)"
            : "anonymous");
        Changed?.Invoke(new BevStateChangeInfo(action, detail));
    }

    internal static BevAuthenticationSnapshot Describe(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return new BevAuthenticationSnapshot();
        }

        var identity = user.Identity;
        var roleTypes = user.Identities.Select(i => i.RoleClaimType).Distinct().ToArray();
        return new BevAuthenticationSnapshot
        {
            IsAuthenticated = identity?.IsAuthenticated == true,
            AuthenticationType = identity?.AuthenticationType,
            Name = identity?.Name,
            Identities = user.Identities.Count(),
            Roles = user.Claims.Where(c => roleTypes.Contains(c.Type)).Select(c => c.Value).Distinct().Take(MaxClaims).ToArray(),
            Claims = user.Claims.Take(MaxClaims).Select(c => new BevClaimView(ShortClaimType(c.Type), c.Value, c.Issuer == ClaimsIdentity.DefaultIssuer ? null : c.Issuer)).ToArray(),
        };
    }

    /// <summary>"http://schemas.microsoft.com/ws/2008/06/identity/claims/role" reads as "role".</summary>
    internal static string ShortClaimType(string type)
    {
        if (!type.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !type.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return type;
        }

        var slash = type.LastIndexOf('/');
        return slash >= 0 && slash < type.Length - 1 ? type[(slash + 1)..] : type;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_handler is not null)
        {
            try
            {
                ChangedEvent!.RemoveEventHandler(_provider, _handler);
            }
            catch
            {
                // the provider may already be disposed with its scope
            }
        }

        Changed = null;
    }
}
