namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The seeded non-admin member of the default store tenant (See UserConfiguration), present in every deployment. The
/// identity to sign in as when a test needs somebody and doesn't care who: no privileges, nothing to disturb.
/// </summary>
public static class StoreUser
{
    public const string Email = "store-user@bitplatform.dev";
    public const string Password = "123456";
}
